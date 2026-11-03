using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Rcinel.Level;

/// <summary>
/// Draws the frame in the world the way the game draws a workbench's range: its dotted ring segments,
/// once on the ground along the frame's edge and once, tinted, at the target height.
/// </summary>
/// <remarks>
/// The edge is drawn half a metre outside the outermost vertices, in the middle of the 1 m cell where
/// the levelled ground meets the rest: that is where the wall will stand. The ground line follows the
/// terrain like the game's ring does (a ray down per segment).
/// </remarks>
internal sealed class FrameView
{
  private const float Spacing = 1f;
  private static readonly Color TargetTint = new(0.45f, 0.85f, 1f, 1f);

  private readonly List<GameObject> _ground = [];
  private readonly List<GameObject> _target = [];
  private readonly List<(Vector3 Point, Vector3 Tangent)> _outline = [];
  private GameObject? _root;
  private int _drawnRevision = -1;
  private float _drawnAt;
  private float? _drawnTarget;

  public void Show(Frame frame, float? target)
  {
    if (_root == null)
    {
      // Unity destroys it with the scene; start over then.
      _root = new GameObject("RcinelLevel frame");
      _ground.Clear();
      _target.Clear();
      _drawnRevision = -1;
    }

    _root.SetActive(true);
    // The ground changes as people dig, so the ground line is redrawn now and then even when the frame
    // stays the same.
    if (frame.Revision == _drawnRevision && System.Nullable.Equals(target, _drawnTarget) && Time.time - _drawnAt < 0.5f)
    {
      return;
    }

    _drawnRevision = frame.Revision;
    _drawnTarget = target;
    _drawnAt = Time.time;
    Outline(frame);
    Draw(_ground, tint: null, point => Physics.Raycast(point + Vector3.up * 500f, Vector3.down, out var hit, 1000f, GroundMask) ? hit.point.y : (float?)null);
    Draw(_target, TargetTint, _ => target);
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
      var radius = frame.Radius + 0.5f;
      var count = Mathf.Max(8, Mathf.CeilToInt(2 * Mathf.PI * radius / Spacing));
      for (var i = 0; i < count; i++)
      {
        var angle = 2 * Mathf.PI * i / count;
        var point = new Vector3(frame.CenterX + Mathf.Sin(angle) * radius, 0f, frame.CenterZ + Mathf.Cos(angle) * radius);
        _outline.Add((point, new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle))));
      }

      return;
    }

    var (minX, minZ, maxX, maxZ) = frame.Bounds;
    float west = minX - 0.5f, east = maxX + 0.5f, south = minZ - 0.5f, north = maxZ + 0.5f;
    Edge(new Vector3(west, 0, south), new Vector3(east, 0, south));
    Edge(new Vector3(east, 0, south), new Vector3(east, 0, north));
    Edge(new Vector3(east, 0, north), new Vector3(west, 0, north));
    Edge(new Vector3(west, 0, north), new Vector3(west, 0, south));

    void Edge(Vector3 from, Vector3 to)
    {
      var length = Vector3.Distance(from, to);
      var count = Mathf.Max(1, Mathf.RoundToInt(length / Spacing));
      var tangent = (to - from).normalized;
      for (var i = 0; i < count; i++)
      {
        _outline.Add((Vector3.Lerp(from, to, (i + 0.5f) / count), tangent));
      }
    }
  }

  private void Draw(List<GameObject> pool, Color? tint, System.Func<Vector3, float?> height)
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
