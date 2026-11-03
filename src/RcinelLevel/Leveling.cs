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
  /// Places the tiles at <paramref name="height"/>, a few per rendered frame, so a large frame doesn't
  /// freeze the game. Each one is a terrain op the game applies and sends on like the hoe's own.
  /// </summary>
  public static IEnumerator Level(List<(int X, int Z, int Radius)> tiles, float height, int perFrame)
  {
    var placed = 0;
    foreach (var (x, z, radius) in tiles)
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
