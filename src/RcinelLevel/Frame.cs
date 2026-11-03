using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rcinel.Level;

internal enum FrameShape
{
  Circle,
  Square,
  Rectangle,
}

/// <summary>
/// The area the probe measures and the hoe levels (decisions 0006, 0010), on the terrain's vertex grid.
/// </summary>
/// <remarks>
/// <para>
/// Valheim's terrain vertices sit on whole metres of world X and Z (heightmaps are 64 m zones centred
/// on multiples of 64, one vertex per metre), and level ops can't rotate. So the frame is a set of
/// vertices: a centre vertex plus extents in whole metres, along the world's axes.
/// </para>
/// <para>
/// Circle and square have one radius. A rectangle has four extents (north = +Z, east = +X), and
/// resizing moves only the side the player faces. The probe owns the frame; the hoe levels whatever it
/// last showed, so there is one frame per game, for the local player.
/// </para>
/// </remarks>
internal sealed class Frame
{
  public FrameShape Shape { get; private set; } = FrameShape.Square;
  public int CenterX { get; private set; }
  public int CenterZ { get; private set; }
  public int Radius { get; private set; } = 4;
  public int North { get; private set; } = 4;
  public int East { get; private set; } = 4;
  public int South { get; private set; } = 4;
  public int West { get; private set; } = 4;

  /// <summary>True once the probe has shown the frame somewhere; until then the hoe has nothing to level.</summary>
  public bool Placed { get; private set; }

  /// <summary>Pinned frames stay put; unpinned ones follow the probe's aim.</summary>
  public bool Pinned { get; private set; }

  /// <summary>The height the hoe levels to; null means "the probe's result".</summary>
  public float? ManualHeight { get; private set; }

  /// <summary>Bumped on every change, so the view and the probe know when to recompute.</summary>
  public int Revision { get; private set; }

  public void CycleShape()
  {
    Shape = Shape switch
    {
      FrameShape.Circle => FrameShape.Square,
      FrameShape.Square => FrameShape.Rectangle,
      _ => FrameShape.Circle,
    };
    if (Shape == FrameShape.Rectangle)
    {
      North = East = South = West = Radius;
    }

    Revision++;
  }

  public void MoveTo(Vector3 point)
  {
    var x = Mathf.RoundToInt(point.x);
    var z = Mathf.RoundToInt(point.z);
    if (Placed && x == CenterX && z == CenterZ)
    {
      return;
    }

    CenterX = x;
    CenterZ = z;
    Placed = true;
    Revision++;
  }

  public void SetPinned(bool pinned)
  {
    Pinned = pinned;
    Revision++;
  }

  /// <summary>
  /// One scroll step. A rectangle grows on the side <paramref name="facing"/> points to, snapped to
  /// the nearest cardinal direction; circle and square grow evenly.
  /// </summary>
  public void Resize(int step, Vector3 facing, int maxRadius)
  {
    if (Shape != FrameShape.Rectangle)
    {
      Radius = Mathf.Clamp(Radius + step, 1, maxRadius);
    }
    else if (Mathf.Abs(facing.x) > Mathf.Abs(facing.z))
    {
      // East-west: the two sides together may span what a square of maxRadius would.
      if (facing.x > 0)
      {
        East = Mathf.Clamp(East + step, 0, 2 * maxRadius - West);
      }
      else
      {
        West = Mathf.Clamp(West + step, 0, 2 * maxRadius - East);
      }
    }
    else if (facing.z > 0)
    {
      North = Mathf.Clamp(North + step, 0, 2 * maxRadius - South);
    }
    else
    {
      South = Mathf.Clamp(South + step, 0, 2 * maxRadius - North);
    }

    Revision++;
  }

  /// <summary>Applies a server's smaller limit to a frame made under a larger one.</summary>
  public void Limit(int maxRadius)
  {
    var before = (Radius, North, East, South, West);
    Radius = Mathf.Min(Radius, maxRadius);
    North = Mathf.Min(North, 2 * maxRadius);
    South = Mathf.Min(South, 2 * maxRadius - North);
    East = Mathf.Min(East, 2 * maxRadius);
    West = Mathf.Min(West, 2 * maxRadius - East);
    if (before != (Radius, North, East, South, West))
    {
      Revision++;
    }
  }

  public void SetManualHeight(float? height)
  {
    ManualHeight = height;
    Revision++;
  }

  /// <summary>The frame's extents from its centre: west, south (negative) and east, north.</summary>
  public (int MinX, int MinZ, int MaxX, int MaxZ) Bounds => Shape == FrameShape.Rectangle
    ? (CenterX - West, CenterZ - South, CenterX + East, CenterZ + North)
    : (CenterX - Radius, CenterZ - Radius, CenterX + Radius, CenterZ + Radius);

  /// <summary>Whether a vertex belongs to the frame.</summary>
  public bool Contains(int x, int z)
  {
    var (minX, minZ, maxX, maxZ) = Bounds;
    if (x < minX || x > maxX || z < minZ || z > maxZ)
    {
      return false;
    }

    if (Shape != FrameShape.Circle)
    {
      return true;
    }

    // Half a metre more than the radius rounds the circle's staircase outwards, so a radius of 1
    // isn't a plus sign.
    float dx = x - CenterX, dz = z - CenterZ;
    return dx * dx + dz * dz <= (Radius + 0.5f) * (Radius + 0.5f);
  }

  public IEnumerable<(int X, int Z)> Vertices()
  {
    var (minX, minZ, maxX, maxZ) = Bounds;
    for (var z = minZ; z <= maxZ; z++)
    {
      for (var x = minX; x <= maxX; x++)
      {
        if (Contains(x, z))
        {
          yield return (x, z);
        }
      }
    }
  }

  /// <summary>A short description for the readout, such as "Rectangle 9 × 5 m".</summary>
  public string Describe()
  {
    var (minX, minZ, maxX, maxZ) = Bounds;
    return Shape switch
    {
      FrameShape.Circle => $"Circle Ø {2 * Radius + 1} m",
      FrameShape.Square => $"Square {2 * Radius + 1} × {2 * Radius + 1} m",
      _ => $"Rectangle {maxX - minX + 1} (E–W) × {maxZ - minZ + 1} (N–S) m",
    };
  }
}

/// <summary>What the probe measured over the frame.</summary>
/// <param name="Balance">The height where cut equals fill: the mean of the vertices' heights.</param>
/// <param name="Cut">Cubic metres above the target height that levelling digs away.</param>
/// <param name="Fill">Cubic metres below the target height that levelling raises.</param>
/// <param name="OutOfReach">Vertices the game's ±8 m limit keeps from reaching the target.</param>
internal readonly record struct Measurement(float Balance, float Cut, float Fill, int OutOfReach, int Vertices, float Lowest, float Highest);

/// <summary>Reads the ground under a frame (decision 0002).</summary>
internal static class Probe
{
  /// <summary>How far a vertex may be moved from the ground the world was generated with (TerrainComp).</summary>
  public const float MaxChange = 8f;

  /// <summary>
  /// Measures the frame, against <paramref name="target"/> if given, else against its own balance.
  /// Null while the ground under it isn't loaded.
  /// </summary>
  /// <remarks>
  /// Each vertex stands for one square metre, so on the regular grid the height where digging and
  /// raising cancel out is simply the mean of the vertices' heights.
  /// </remarks>
  public static Measurement? Measure(Frame frame, float? target)
  {
    var heights = new List<(float Height, float Generated)>();
    foreach (var (x, z) in frame.Vertices())
    {
      var point = new Vector3(x, 0f, z);
      if (!Heightmap.GetHeight(point, out var height))
      {
        return null;
      }

      heights.Add((height, height - Change(point)));
    }

    if (heights.Count == 0)
    {
      return null;
    }

    double sum = 0;
    float lowest = float.MaxValue, highest = float.MinValue;
    foreach (var (height, _) in heights)
    {
      sum += height;
      lowest = Math.Min(lowest, height);
      highest = Math.Max(highest, height);
    }

    var balance = (float)(sum / heights.Count);
    var level = target ?? balance;
    float cut = 0, fill = 0;
    var outOfReach = 0;
    foreach (var (height, generated) in heights)
    {
      if (height > level)
      {
        cut += height - level;
      }
      else
      {
        fill += level - height;
      }

      if (Math.Abs(level - generated) > MaxChange + 0.01f)
      {
        outOfReach++;
      }
    }

    return new Measurement(balance, cut, fill, outOfReach, heights.Count, lowest, highest);
  }

  /// <summary>
  /// How far a vertex has already been moved from the generated ground. The game keeps this per
  /// heightmap in its terrain compiler, which every client loads from the network; no compiler means
  /// nobody has changed the terrain there.
  /// </summary>
  private static float Change(Vector3 point)
  {
    var compiler = TerrainComp.FindTerrainCompiler(point);
    if (compiler is null || compiler.m_levelDelta is null || compiler.m_hmap is null)
    {
      return 0f;
    }

    compiler.m_hmap.WorldToVertex(point, out var x, out var y);
    var width = compiler.m_width + 1;
    if (x < 0 || y < 0 || x >= width || y >= width)
    {
      return 0f;
    }

    var index = y * width + x;
    return compiler.m_levelDelta[index] + compiler.m_smoothDelta[index];
  }
}
