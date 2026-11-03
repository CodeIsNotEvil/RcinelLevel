using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rcinel.Level;

/// <summary>
/// A translucent red layer on the ground over every spot of the frame the game's ±8 m limit keeps
/// from reaching the target, shown while the probe is held (decision 0018).
/// </summary>
/// <remarks>
/// <para>
/// One mesh for all of them: a 1 m square per vertex, centred on it, whose corners sit at the mean
/// height of the four terrain vertices around each corner, so the layer lies on slopes and steps
/// instead of floating flat. It is lifted a little so the ground doesn't cover it.
/// </para>
/// <para>
/// It uses Unity's built-in "Sprites/Default" shader (unlit, transparent, both sides, in Unity's
/// always-included set), with two fallbacks. Without any there is no overlay, only the readout's count.
/// </para>
/// </remarks>
internal sealed class ReachOverlay(ManualLogSource logger)
{
  private const float Lift = 0.08f;
  private static readonly Color Warning = new(0.9f, 0.12f, 0.06f, 0.38f);

  private GameObject? _root;
  private Mesh? _mesh;
  private bool _noShader;
  private Measurement? _shown;

  public void Show(Measurement measurement)
  {
    if (measurement.Unreachable.Count == 0)
    {
      Hide();
      return;
    }

    if (!Ready())
    {
      return;
    }

    _root!.SetActive(true);
    // Rebuilt only when the probe measured again, twice a second at most.
    if (_shown is { } shown && ReferenceEquals(shown.Unreachable, measurement.Unreachable))
    {
      return;
    }

    _shown = measurement;
    Build(measurement);
  }

  public void Hide()
  {
    if (_root != null)
    {
      _root.SetActive(false);
    }
  }

  private bool Ready()
  {
    if (_root != null)
    {
      return true;
    }

    if (_noShader)
    {
      return false;
    }

    // Fallbacks in case the game's build trims Unity's always-included shaders: uGUI's own, then the
    // one Unity draws its debug lines with. All three are unlit and transparent.
    Shader? shader = null;
    foreach (var name in new[] { "Sprites/Default", "UI/Default", "Hidden/Internal-Colored" })
    {
      shader = Shader.Find(name);
      if (shader != null)
      {
        break;
      }
    }

    if (shader == null)
    {
      _noShader = true;
      logger.LogWarning("No transparent built-in shader was found, so the probe shows no red overlay.");
      return false;
    }

    // Unity destroys it with the scene; made again then.
    _root = new GameObject("RcinelLevel reach overlay");
    _mesh = new Mesh { name = "rcinellevel_reach", indexFormat = IndexFormat.UInt32 };
    _mesh.MarkDynamic();
    _root.AddComponent<MeshFilter>().sharedMesh = _mesh;
    var renderer = _root.AddComponent<MeshRenderer>();
    renderer.sharedMaterial = new Material(shader) { name = "rcinellevel_reach", color = Warning, renderQueue = 3000 };
    renderer.shadowCastingMode = ShadowCastingMode.Off;
    renderer.receiveShadows = false;
    _shown = null;
    return true;
  }

  private void Build(Measurement m)
  {
    var heights = m.Heights;
    int width = heights.GetLength(0), depth = heights.GetLength(1);
    var vertices = new List<Vector3>(m.Unreachable.Count * 4);
    var triangles = new List<int>(m.Unreachable.Count * 6);
    foreach (var (x, z) in m.Unreachable)
    {
      int gx = x - m.MinX, gz = z - m.MinZ;
      var start = vertices.Count;
      vertices.Add(Corner(x, z, gx, gz, -1, -1));
      vertices.Add(Corner(x, z, gx, gz, -1, 1));
      vertices.Add(Corner(x, z, gx, gz, 1, 1));
      vertices.Add(Corner(x, z, gx, gz, 1, -1));
      triangles.AddRange([start, start + 1, start + 2, start, start + 2, start + 3]);
    }

    _mesh!.Clear();
    _mesh.SetVertices(vertices);
    _mesh.SetTriangles(triangles, 0);
    _mesh.RecalculateBounds();

    // The corner half a metre away in (dx, dz): the mean of the up to four vertices around it that
    // are loaded and in the frame; the centre vertex is always one of them.
    Vector3 Corner(int x, int z, int gx, int gz, int dx, int dz)
    {
      float sum = 0;
      var count = 0;
      foreach (var (ox, oz) in new[] { (0, 0), (dx, 0), (0, dz), (dx, dz) })
      {
        int nx = gx + ox, nz = gz + oz;
        if (nx >= 0 && nz >= 0 && nx < width && nz < depth && !float.IsNaN(heights[nx, nz]))
        {
          sum += heights[nx, nz];
          count++;
        }
      }

      return new Vector3(x + dx * 0.5f, sum / count + Lift, z + dz * 0.5f);
    }
  }
}
