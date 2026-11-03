using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rcinel.Level;

/// <summary>
/// The terrain ops the level hoe places: square level ops in a few fixed sizes, side by side.
/// </summary>
/// <remarks>
/// <para>
/// A terrain op reaches the terrain's owner as nothing but its prefab's name hash; the owner looks the
/// settings up in its own <c>ObjectDB.m_terrainOps</c> (resources/terrain-and-multiplayer.md). So the
/// size can't be chosen per use, and the frame is covered with tiles instead (decision 0006). Every
/// client and the server register the same tiles, which is one reason everyone needs the mod.
/// </para>
/// <para>
/// A level op with <c>m_square</c> moves every vertex within <c>ceil(radius)</c> of its centre vertex
/// to its own height, with no falloff: a tile of radius k covers (2k + 1)² vertices.
/// </para>
/// <para>
/// <b>Tiles must never overlap.</b> A level op adds "target minus the vertex's current height" to the
/// vertex's stored change, and it reads that height from the heightmap, which the game only rebuilds a
/// frame or more later (<c>Heightmap.Poke(1)</c>). Two ops on one vertex before the rebuild both see the
/// old height and move it twice: the first version overlapped tiles at the edges and left spikes and
/// pits exactly there (owner's test, 2026-11-03). So the tiles partition the area.
/// </para>
/// </remarks>
internal static class TerrainTiles
{
  /// <summary>Tile radii, largest first: 9×9, 5×5, 3×3 and single vertices for the edges.</summary>
  public static readonly int[] Radii = [4, 2, 1, 0];

  public static readonly Dictionary<int, TerrainOp> Prefabs = new();

  public static string PrefabName(int radius) => $"RcinelLevel_LevelTile{radius}";

  /// <summary>
  /// Covers the vertices for which <paramref name="inside"/> is true, within the given bounds, with
  /// tiles that neither overlap nor reach outside: big ones first, single vertices for what is left.
  /// </summary>
  public static List<(int X, int Z, int Radius)> Cover((int MinX, int MinZ, int MaxX, int MaxZ) bounds, Func<int, int, bool> inside)
  {
    var (minX, minZ, maxX, maxZ) = bounds;
    int width = maxX - minX + 1, depth = maxZ - minZ + 1;
    if (width <= 0 || depth <= 0)
    {
      return [];
    }

    // free[gx, gz]: inside the area and not yet taken by a tile.
    var free = new bool[width, depth];
    for (var gz = 0; gz < depth; gz++)
    {
      for (var gx = 0; gx < width; gx++)
      {
        free[gx, gz] = inside(gx + minX, gz + minZ);
      }
    }

    var tiles = new List<(int, int, int)>();
    foreach (var k in Radii)
    {
      for (var gz = k; gz < depth - k; gz++)
      {
        for (var gx = k; gx < width - k; gx++)
        {
          if (!AllFree(gx, gz, k))
          {
            continue;
          }

          tiles.Add((gx + minX, gz + minZ, k));
          for (var dz = -k; dz <= k; dz++)
          {
            for (var dx = -k; dx <= k; dx++)
            {
              free[gx + dx, gz + dz] = false;
            }
          }
        }
      }
    }

    return tiles;

    bool AllFree(int gx, int gz, int k)
    {
      for (var dz = -k; dz <= k; dz++)
      {
        for (var dx = -k; dx <= k; dx++)
        {
          if (!free[gx + dx, gz + dz])
          {
            return false;
          }
        }
      }

      return true;
    }
  }

  /// <summary>
  /// Places the tiles, each at its own height, a few per rendered frame, so a large area doesn't
  /// freeze the game. Each one is a terrain op the game applies and sends on like the hoe's own.
  /// </summary>
  public static IEnumerator Level(List<(int X, int Z, int Radius, float Height)> tiles, int perFrame)
  {
    var placed = 0;
    foreach (var (x, z, radius, height) in tiles)
    {
      if (!Prefabs.TryGetValue(radius, out var prefab))
      {
        continue;
      }

      // The op's Awake applies it to every heightmap it touches and destroys itself.
      UnityEngine.Object.Instantiate(prefab.gameObject, new Vector3(x, height, z), Quaternion.identity);
      if (++placed % perFrame == 0)
      {
        yield return null;
      }
    }
  }

  public static bool IsTile(TerrainOp op) => op.name.StartsWith("RcinelLevel_LevelTile", StringComparison.Ordinal);
}

/// <summary>
/// One stroke of the level hoe: it only <b>raises</b> ground that lies below the target, inside the
/// brush and the frame, and pays for it in stone (decision 0015). Digging is the pickaxe's job.
/// </summary>
internal static class Stroke
{
  /// <summary>Stone per cubic metre the game's own Raise ground costs; measured from it in <see cref="Items"/>.</summary>
  public static float StonePerCubicMetre { get; set; } = 1f;

  /// <summary>What a stroke would raise: the tiles, the cubic metres and the stone it costs.</summary>
  public static (List<(int X, int Z, int Radius, float Height)> Tiles, float Fill, int Stone) Plan(
    Frame frame, (int MinX, int MinZ, int MaxX, int MaxZ) brush, float target, float costFactor)
  {
    // The game clamps every vertex to 8 m from the generated ground, so nothing beyond is raised or paid.
    var raise = new Dictionary<(int, int), float>();
    for (var z = brush.MinZ; z <= brush.MaxZ; z++)
    {
      for (var x = brush.MinX; x <= brush.MaxX; x++)
      {
        if (!frame.Contains(x, z) || Probe.At(x, z) is not { } ground)
        {
          continue;
        }

        var top = Math.Min(target, ground.Generated + Probe.MaxChange);
        if (top - ground.Height > 0.02f)
        {
          raise[(x, z)] = top - ground.Height;
        }
      }
    }

    var fill = 0f;
    foreach (var amount in raise.Values)
    {
      fill += amount;
    }

    // Where the limit lowers a vertex's top, a tile at the target still ends at the limit: the game clamps.
    var tiles = new List<(int, int, int, float)>();
    foreach (var (x, z, radius) in TerrainTiles.Cover(brush, (x, z) => raise.ContainsKey((x, z))))
    {
      tiles.Add((x, z, radius, target));
    }

    return (tiles, fill, Mathf.CeilToInt(fill * StonePerCubicMetre * costFactor - 0.001f));
  }
}

/// <summary>
/// With the depth lock on, a pickaxe hit that reaches into the pinned frame is replaced by edits that
/// dig the same as the game's, but only inside the frame and never below the target (decision 0016).
/// </summary>
/// <remarks>
/// <para>
/// A pickaxe hit spawns a terrain op on the attacker's own client (<c>Attack.SpawnOnHitTerrain</c>),
/// which lowers ground with a falloff (<c>TerrainComp.RaiseTerrain</c> with a negative delta). Its
/// settings travel as a prefab hash, so they can't be changed per hit; instead the hit is worked out
/// here vertex by vertex, as the game would, and sent as single-vertex level tiles, each at the height
/// the vertex should end at. The hit's own effects and any item it drops still happen.
/// </para>
/// <para>
/// Hits that don't reach the frame are left to the game. Vertices outside the frame are never
/// touched by a hit that does, so the frame's edge stays a straight wall.
/// </para>
/// </remarks>
internal static class DepthLock
{
  /// <summary>True when it took the op over; the game's own Awake must not run then.</summary>
  public static bool TakeOver(TerrainOp op)
  {
    if (TerrainOp.m_forceDisableTerrainOps || TerrainTiles.IsTile(op) || Controls.Current?.LockedDepth is not { } floor)
    {
      return false;
    }

    var settings = op.m_settings;
    var player = Player.m_localPlayer;
    if (!settings.m_raise || settings.m_raiseDelta >= 0f || settings.m_level || player == null
        || player.GetRightItem()?.m_shared.m_skillType != Skills.SkillType.Pickaxes)
    {
      return false;
    }

    var frame = Controls.Current.Frame;
    var position = op.transform.position;
    var radius = settings.m_raiseRadius;
    var reach = Mathf.CeilToInt(radius);
    int cx = Mathf.RoundToInt(position.x), cz = Mathf.RoundToInt(position.z);
    var touches = false;
    for (var z = cz - reach; z <= cz + reach && !touches; z++)
    {
      for (var x = cx - reach; x <= cx + reach && !touches; x++)
      {
        touches = frame.Contains(x, z);
      }
    }

    if (!touches)
    {
      return false;
    }

    var tiles = new List<(int, int, int, float)>();
    for (var z = cz - reach; z <= cz + reach; z++)
    {
      for (var x = cx - reach; x <= cx + reach; x++)
      {
        // The same falloff as the game's RaiseTerrain.
        var strength = 1f;
        if (!settings.m_square)
        {
          var distance = Vector2.Distance(new Vector2(cx, cz), new Vector2(x, z));
          if (distance > radius)
          {
            continue;
          }

          if (settings.m_raisePower > 0f)
          {
            strength = Mathf.Pow(1f - distance / radius, settings.m_raisePower);
          }
        }

        if (!frame.Contains(x, z) || Probe.At(x, z) is not { } ground)
        {
          continue;
        }

        var wanted = Math.Max(position.y + settings.m_raiseDelta * strength, floor);
        if (wanted < ground.Height - 0.01f)
        {
          tiles.Add((x, z, 0, wanted));
        }
      }
    }

    RcinelLevelPlugin.Instance.StartCoroutine(TerrainTiles.Level(tiles, int.MaxValue));
    op.OnPlaced();
    UnityEngine.Object.Destroy(op.gameObject);
    return true;
  }
}

/// <summary>
/// The level hoe's one piece. Placing it levels the hoe's brush where it was placed, as far as the
/// probe's frame reaches, to the frame's target height (decision 0014); the piece itself then
/// disappears, as the game's terrain pieces do.
/// </summary>
internal sealed class LevelAction : MonoBehaviour
{
  private void Awake()
  {
    // The build ghost is instantiated from the same prefab with terrain ops switched off; it must
    // stay and show where the player aims, not level anything.
    if (TerrainOp.m_forceDisableTerrainOps)
    {
      return;
    }

    Controls.Current?.LevelAt(transform.position);
    Destroy(gameObject);
  }
}
