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
/// A level op with <c>m_square</c> sets every vertex within <c>ceil(radius)</c> of its centre vertex
/// to its own height, with no falloff: a tile of radius k covers (2k + 1)² vertices. Overlapping tiles
/// set the same height, so overlaps are harmless; a tile may never reach outside the frame.
/// </para>
/// </remarks>
internal static class TerrainTiles
{
  /// <summary>Tile radii, largest first: 9×9, 5×5, 3×3 and single vertices for the edges.</summary>
  public static readonly int[] Radii = [4, 2, 1, 0];

  public static readonly Dictionary<int, TerrainOp> Prefabs = new();

  public static string PrefabName(int radius) => $"RcinelLevel_LevelTile{radius}";

  /// <summary>Covers the frame's vertices with tiles, big ones first, none reaching outside.</summary>
  public static List<(int X, int Z, int Radius)> Cover(Frame frame)
  {
    var (minX, minZ, maxX, maxZ) = frame.Bounds;
    int width = maxX - minX + 1, depth = maxZ - minZ + 1;
    var inside = new bool[width, depth];
    var covered = new bool[width, depth];
    foreach (var (x, z) in frame.Vertices())
    {
      inside[x - minX, z - minZ] = true;
    }

    var tiles = new List<(int, int, int)>();
    foreach (var k in Radii)
    {
      for (var gz = k; gz < depth - k; gz++)
      {
        for (var gx = k; gx < width - k; gx++)
        {
          if (Fits(gx, gz, k) && AddsSomething(gx, gz, k))
          {
            tiles.Add((gx + minX, gz + minZ, k));
            for (var dz = -k; dz <= k; dz++)
            {
              for (var dx = -k; dx <= k; dx++)
              {
                covered[gx + dx, gz + dz] = true;
              }
            }
          }
        }
      }
    }

    return tiles;

    bool Fits(int gx, int gz, int k)
    {
      for (var dz = -k; dz <= k; dz++)
      {
        for (var dx = -k; dx <= k; dx++)
        {
          if (!inside[gx + dx, gz + dz])
          {
            return false;
          }
        }
      }

      return true;
    }

    bool AddsSomething(int gx, int gz, int k)
    {
      for (var dz = -k; dz <= k; dz++)
      {
        for (var dx = -k; dx <= k; dx++)
        {
          if (!covered[gx + dx, gz + dz])
          {
            return true;
          }
        }
      }

      return false;
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
      Object.Instantiate(prefab.gameObject, new Vector3(x, height, z), Quaternion.identity);
      if (++placed % perFrame == 0)
      {
        yield return null;
      }
    }
  }
}

/// <summary>
/// The level hoe's one piece. Placing it levels the probe's frame instead of the spot it was placed
/// on; the piece itself then disappears, as the game's terrain pieces do.
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

    Controls.Current?.LevelFrame();
    Destroy(gameObject);
  }
}
