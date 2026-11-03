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

  /// <summary>
  /// A paint-only op: paints one vertex's ground as cleared dirt at full strength, which also clears
  /// the grass there, as the game's own levelling does. The tiles don't paint; strokes and locked
  /// digs paint the ground they leave level with these (owner's test: the grass stayed, 2026-11-03).
  /// </summary>
  public static TerrainOp? Paint { get; set; }

  public const string PaintName = "RcinelLevel_Paint";

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
  /// <remarks>
  /// <paramref name="paint"/> lists the vertices to paint afterwards. Paint ops read and write the
  /// terrain compiler's own paint mask, not the heightmap, so they may overlap each other and the tiles.
  /// </remarks>
  public static IEnumerator Level(List<(int X, int Z, int Radius, float Height)> tiles, int perFrame, List<(int X, int Z, float Height)>? paint = null)
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

    if (paint is null || Paint is null)
    {
      yield break;
    }

    foreach (var (x, z, height) in paint)
    {
      UnityEngine.Object.Instantiate(Paint.gameObject, new Vector3(x, height, z), Quaternion.identity);
      if (++placed % perFrame == 0)
      {
        yield return null;
      }
    }
  }

  public static bool IsTile(TerrainOp op) =>
    op.name.StartsWith("RcinelLevel_LevelTile", StringComparison.Ordinal) || op.name.StartsWith(PaintName, StringComparison.Ordinal);
}

/// <summary>
/// One stroke of the level hoe: it only <b>raises</b> ground that lies below the target, inside the
/// brush and the frame, and pays for it in stone (decision 0015). Digging is the pickaxe's job.
/// </summary>
internal static class Stroke
{
  /// <summary>Ground at most this far above the target counts as level: painted, and no complaint.</summary>
  public const float NearTarget = 0.25f;

  /// <summary>Stone per cubic metre the game's own Raise ground costs; measured from it in <see cref="Items"/>.</summary>
  public static float StonePerCubicMetre { get; set; } = 1f;

  /// <summary>What a stroke would raise: the tiles, the cubic metres and the stone it costs.</summary>
  /// <remarks>
  /// <para>
  /// Each spot's first <paramref name="freeDepth"/> metres of raising are free; only what lies above is
  /// paid for, so evening out almost flat ground costs nothing and the price grows smoothly from there.
  /// </para>
  /// <para>
  /// With <paramref name="dig"/> (the dev command <c>rcinellevel_dig</c>) it also lowers ground above
  /// the target, down to the game's -8 m limit; only raising is paid for.
  /// </para>
  /// </remarks>
  public static (List<(int X, int Z, int Radius, float Height)> Tiles, float Fill, int Stone, List<(int X, int Z, float Height)> Paint) Plan(
    Frame frame, (int MinX, int MinZ, int MaxX, int MaxZ) brush, float target, float costFactor, float freeDepth, bool dig = false)
  {
    // The game clamps every vertex to 8 m from the generated ground, so nothing beyond is raised or paid.
    var raise = new Dictionary<(int, int), float>();
    var lower = new HashSet<(int, int)>();
    var moat = new List<(int X, int Z, float Height)>();
    // Painted as cleared: what this stroke raises and what already lies at the target, the way the
    // game's level ground leaves the whole levelled patch bare.
    var paint = new List<(int, int, float)>();
    for (var z = brush.MinZ; z <= brush.MaxZ; z++)
    {
      for (var x = brush.MinX; x <= brush.MaxX; x++)
      {
        // With the dev dig on, the hoe digs the moat down to its floor as well; it never fills it.
        if (dig && frame.InMoat(x, z) && Probe.At(x, z) is { } bank)
        {
          var bottom = Math.Max(target - MoatSizes.Depth, bank.Generated - Probe.MaxChange);
          if (bank.Height - bottom > 0.02f)
          {
            moat.Add((x, z, bottom));
            paint.Add((x, z, bottom));
          }

          continue;
        }

        if (!frame.Contains(x, z) || Probe.At(x, z) is not { } ground)
        {
          continue;
        }

        var top = Math.Min(target, ground.Generated + Probe.MaxChange);
        if (top - ground.Height > 0.02f)
        {
          raise[(x, z)] = top - ground.Height;
          paint.Add((x, z, top));
        }
        else if (dig && ground.Height - Math.Max(target, ground.Generated - Probe.MaxChange) > 0.02f)
        {
          lower.Add((x, z));
          paint.Add((x, z, target));
        }
        else if (ground.Height - target <= NearTarget)
        {
          paint.Add((x, z, ground.Height));
        }
      }
    }

    float fill = 0f, paid = 0f;
    foreach (var amount in raise.Values)
    {
      fill += amount;
      paid += Math.Max(0f, amount - freeDepth);
    }

    // Where the limit lowers a vertex's top, a tile at the target still ends at the limit: the game clamps.
    var tiles = new List<(int, int, int, float)>();
    foreach (var (x, z, radius) in TerrainTiles.Cover(brush, (x, z) => raise.ContainsKey((x, z)) || lower.Contains((x, z))))
    {
      tiles.Add((x, z, radius, target));
    }

    // The moat's floor follows the -8 m limit per vertex, so each gets a tile of its own.
    foreach (var (x, z, height) in moat)
    {
      tiles.Add((x, z, 0, height));
    }

    return (tiles, fill, Mathf.CeilToInt(paid * StonePerCubicMetre * costFactor - 0.001f), paint);
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
        touches = frame.Contains(x, z) || frame.InMoat(x, z);
      }
    }

    if (!touches)
    {
      return false;
    }

    var tiles = new List<(int, int, int, float)>();
    var paint = new List<(int, int, float)>();
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

        // Inside the frame the floor is the target; in the moat the moat's floor (decision 0022).
        var inFrame = frame.Contains(x, z);
        if ((!inFrame && !frame.InMoat(x, z)) || Probe.At(x, z) is not { } ground)
        {
          continue;
        }

        var wanted = Math.Max(position.y + settings.m_raiseDelta * strength, inFrame ? floor : floor - MoatSizes.Depth);
        if (wanted < ground.Height - 0.01f)
        {
          tiles.Add((x, z, 0, wanted));
          // The game's dig paints the ground it opens; so does this.
          paint.Add((x, z, wanted));
        }
      }
    }

    RcinelLevelPlugin.Instance.StartCoroutine(TerrainTiles.Level(tiles, int.MaxValue, paint));
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
