using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Rcinel.Level;

/// <summary>
/// Draws the frame in the world the way the game draws a workbench's range: its dotted ring segments,
/// once on the ground along the frame's edge and once, tinted blue, at the target height. With the
/// level hoe out, the part its next stroke levels is outlined in orange at the target height.
/// </summary>
/// <remarks>
/// The edge is drawn half a metre outside the outermost vertices, in the middle of the 1 m cell where
/// the levelled ground meets the rest: that is where the wall will stand. The ground line follows the
/// terrain like the game's ring does (a ray down per segment).
/// </remarks>
internal sealed class FrameView
{
  // Segments 1 m apart, wider apart on big frames so a 257 m frame doesn't need a thousand of them.
  private const float Spacing = 1f;
  private const int MaxSegments = 400;
  private static readonly Color TargetTint = new(0.45f, 0.85f, 1f, 1f);
  private static readonly Color BrushTint = new(1f, 0.55f, 0.2f, 1f);
  private static readonly Color MoatTint = new(0.25f, 0.45f, 1f, 1f);

  private readonly List<GameObject> _ground = [];
  private readonly List<GameObject> _target = [];
  private readonly List<GameObject> _brush = [];
  private readonly List<GameObject> _moat = [];
  private readonly List<(Vector3 Point, Vector3 Tangent)> _outline = [];
  private GameObject? _root;
  private int _drawnRevision = -1;
  private float _drawnAt;
  private float? _drawnTarget;
  private (int, int, int, int)? _drawnBrush;

  public void Show(Frame frame, float? target, (int MinX, int MinZ, int MaxX, int MaxZ)? brush)
  {
    if (_root == null)
    {
      // Unity destroys it with the scene; start over then.
      _root = new GameObject("RcinelLevel frame");
      _ground.Clear();
      _target.Clear();
      _brush.Clear();
      _moat.Clear();
      _drawnRevision = -1;
    }

    _root.SetActive(true);
    // The ground changes as people dig, so the ground line is redrawn now and then even when the frame
    // stays the same.
    if (frame.Revision == _drawnRevision && System.Nullable.Equals(target, _drawnTarget) && Time.time - _drawnAt < 0.5f)
    {
      if (!System.Nullable.Equals(brush, _drawnBrush))
      {
        DrawBrush(frame, target, brush);
      }

      return;
    }

    _drawnRevision = frame.Revision;
    _drawnTarget = target;
    _drawnAt = Time.time;
    Outline(frame);
    Draw(_ground, tint: null, point => Physics.Raycast(point + Vector3.up * 500f, Vector3.down, out var hit, 1000f, GroundMask) ? hit.point.y : (float?)null);
    Draw(_target, TargetTint, _ => target);
    DrawMoat(frame);
    DrawBrush(frame, target, brush);
  }

  /// <summary>The moat's outer edge, on the ground, in blue; nothing without a moat.</summary>
  private void DrawMoat(Frame frame)
  {
    _outline.Clear();
    if (frame.Moat != MoatKind.None)
    {
      if (frame.Shape == FrameShape.Circle)
      {
        Circle(frame.CenterX, frame.CenterZ, frame.Radius + 0.5f + frame.MoatWidth);
      }
      else
      {
        Rectangle(frame.OuterBounds);
      }
    }

    Draw(_moat, MoatTint, point => Physics.Raycast(point + Vector3.up * 500f, Vector3.down, out var hit, 1000f, GroundMask) ? hit.point.y : (float?)null);
  }

  /// <summary>The brush, cut to the frame's bounds (a circle's edge is only approximated here).</summary>
  private void DrawBrush(Frame frame, float? target, (int MinX, int MinZ, int MaxX, int MaxZ)? brush)
  {
    _drawnBrush = brush;
    _outline.Clear();
    if (brush is { } b && target is not null)
    {
      var (minX, minZ, maxX, maxZ) = frame.Bounds;
      var clipped = (Math.Max(b.MinX, minX), Math.Max(b.MinZ, minZ), Math.Min(b.MaxX, maxX), Math.Min(b.MaxZ, maxZ));
      if (clipped.Item1 <= clipped.Item3 && clipped.Item2 <= clipped.Item4)
      {
        Rectangle(clipped);
      }
    }

    Draw(_brush, BrushTint, _ => target);
  }

  public void Hide()
  {
    if (_root != null)
    {
      _root.SetActive(false);
    }
  }

  private static readonly int GroundMask = LayerMask.GetMask("terrain");

  private void Outline(Frame frame)
  {
    _outline.Clear();
    if (frame.Shape == FrameShape.Circle)
    {
      Circle(frame.CenterX, frame.CenterZ, frame.Radius + 0.5f);
      return;
    }

    Rectangle(frame.Bounds);
  }

  private void Circle(int centerX, int centerZ, float radius)
  {
    var count = Mathf.Clamp(Mathf.CeilToInt(2 * Mathf.PI * radius / Spacing), 8, MaxSegments);
    for (var i = 0; i < count; i++)
    {
      var angle = 2 * Mathf.PI * i / count;
      var point = new Vector3(centerX + Mathf.Sin(angle) * radius, 0f, centerZ + Mathf.Cos(angle) * radius);
      _outline.Add((point, new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle))));
    }
  }

  private void Rectangle((int MinX, int MinZ, int MaxX, int MaxZ) bounds)
  {
    var (minX, minZ, maxX, maxZ) = bounds;
    float west = minX - 0.5f, east = maxX + 0.5f, south = minZ - 0.5f, north = maxZ + 0.5f;
    var spacing = Mathf.Max(Spacing, 2 * (east - west + north - south) / MaxSegments);
    Edge(new Vector3(west, 0, south), new Vector3(east, 0, south));
    Edge(new Vector3(east, 0, south), new Vector3(east, 0, north));
    Edge(new Vector3(east, 0, north), new Vector3(west, 0, north));
    Edge(new Vector3(west, 0, north), new Vector3(west, 0, south));

    void Edge(Vector3 from, Vector3 to)
    {
      var length = Vector3.Distance(from, to);
      var count = Mathf.Max(1, Mathf.RoundToInt(length / spacing));
      var tangent = (to - from).normalized;
      for (var i = 0; i < count; i++)
      {
        _outline.Add((Vector3.Lerp(from, to, (i + 0.5f) / count), tangent));
      }
    }
  }

  private void Draw(List<GameObject> pool, Color? tint, Func<Vector3, float?> height)
  {
    while (pool.Count < _outline.Count)
    {
      pool.Add(MakeSegment(tint));
    }

    for (var i = 0; i < pool.Count; i++)
    {
      var segment = pool[i];
      if (i >= _outline.Count || height(_outline[i].Point) is not { } y)
      {
        segment.SetActive(false);
        continue;
      }

      var (point, tangent) = _outline[i];
      segment.SetActive(true);
      segment.transform.SetPositionAndRotation(new Vector3(point.x, y, point.z), Quaternion.LookRotation(tangent, Vector3.up));
    }
  }

  private GameObject MakeSegment(Color? tint)
  {
    GameObject segment;
    if (Items.SegmentPrefab != null)
    {
      segment = Object.Instantiate(Items.SegmentPrefab, _root!.transform);
    }
    else
    {
      // Without the game's segment (renamed in an update): a plain thin bar, so the frame still shows.
      segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
      Object.Destroy(segment.GetComponent<Collider>());
      segment.transform.SetParent(_root!.transform, worldPositionStays: false);
      segment.transform.localScale = new Vector3(0.05f, 0.05f, 0.6f);
    }

    if (tint is { } color)
    {
      foreach (var renderer in segment.GetComponentsInChildren<Renderer>(true))
      {
        // .material makes a copy for this segment, leaving the workbench's own ring untouched.
        var material = renderer.material;
        if (material.HasProperty("_Color"))
        {
          material.color = color;
        }

        if (material.HasProperty("_EmissionColor"))
        {
          material.SetColor("_EmissionColor", color);
        }
      }
    }

    return segment;
  }
}

/// <summary>The probe's readout at the top of the screen, in the game's font.</summary>
internal sealed class Readout
{
  private static readonly Color Orange = new(1f, 0.63f, 0.24f, 1f);
  private Text? _text;

  public void Show(string text)
  {
    if (_text == null && !Create())
    {
      return;
    }

    _text!.gameObject.SetActive(true);
    _text.text = text;
  }

  public void Hide()
  {
    if (_text != null)
    {
      _text.gameObject.SetActive(false);
    }
  }

  private bool Create()
  {
    var root = GUIManager.CustomGUIFront;
    if (root == null)
    {
      return false;
    }

    var label = GUIManager.Instance.CreateText(
      text: string.Empty, parent: root.transform,
      anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f), position: new Vector2(0f, -120f),
      font: GUIManager.Instance.AveriaSerifBold, fontSize: 18, color: Orange,
      outline: true, outlineColor: Color.black, width: 1100f, height: 140f, addContentSizeFitter: false);
    _text = label.GetComponent<Text>();
    _text.alignment = TextAnchor.UpperCenter;
    _text.supportRichText = true;
    _text.raycastTarget = false;
    return true;
  }
}
