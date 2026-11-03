using System;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace Rcinel.Level;

/// <summary>
/// What the player does with the probe and the level hoe (decisions 0012, 0014), and what they see.
/// </summary>
/// <remarks>
/// <para>
/// The probe frames an area: the frame follows its aim until pinned, and pinning takes the probe's
/// cut-and-fill height as the target. The level hoe then levels where the player aims, a brush at a
/// time, as far as the frame reaches, to that target. Resizing and the height work with either tool
/// held. Every key is a config entry, read by the player's keyboard layout (<see cref="Keys"/>).
/// </para>
/// <para>
/// The probe measures again whenever the frame changes and twice a second otherwise, because the
/// ground under it changes as people dig.
/// </para>
/// </remarks>
internal sealed class Controls : IDisposable
{
  private const float MeasureInterval = 0.5f;
  private const float AimDistance = 60f;

  private static readonly int GroundMask = LayerMask.GetMask("terrain");
  private static readonly int AnythingMask = LayerMask.GetMask("terrain", "piece", "piece_nonsolid", "static_solid", "Default", "Default_small");

  private readonly Settings _settings;
  private readonly ManualLogSource _logger;
  private readonly Frame _frame = new();
  private readonly FrameView _view = new();
  private readonly Readout _readout = new();
  private Measurement? _measurement;
  private int _measuredRevision = -1;
  private float _measuredAt;

  public Controls(Settings settings, ManualLogSource logger)
  {
    _settings = settings;
    _logger = logger;
    Current = this;
  }

  /// <summary>The one instance, for <see cref="LevelAction"/>, which the game creates.</summary>
  public static Controls? Current { get; private set; }

  /// <summary>The height the hoe levels to: set by hand, else the probe's result from when the frame was pinned, else the live one.</summary>
  private float? Target => _frame.ManualHeight ?? _frame.PinnedBalance ?? _measurement?.Balance;

  private string TargetSource => _frame.ManualHeight is not null ? "by hand" : _frame.PinnedBalance is not null ? "probe, at pinning" : "probe";

  public void Update()
  {
    ScrollPatch.Suppress = false;
    var player = Player.m_localPlayer;
    if (player == null || player.IsDead())
    {
      Hide();
      return;
    }

    var probe = Items.IsProbe(player.GetLeftItem());
    var hoe = Items.IsLevelHoe(player.GetRightItem());
    if (!probe && !hoe)
    {
      Hide();
      return;
    }

    _frame.Limit(_settings.MaxRadius.Value);
    if (TakesInput())
    {
      HandleInput(probe);
    }

    if (probe && !_frame.Pinned && Aim(GroundMask) is { } aim)
    {
      _frame.MoveTo(aim);
    }

    if (_frame.Revision != _measuredRevision || Time.time - _measuredAt > MeasureInterval)
    {
      Measure();
    }

    if (!_frame.Placed)
    {
      _view.Hide();
      _readout.Show("Hold the ground probe and aim at the ground to frame an area.");
      return;
    }

    _view.Show(_frame, Target, hoe ? Brush(player) : null);
    _readout.Show(Describe(probe, hoe));
  }

  /// <summary>Called when the level hoe's piece is placed at <paramref name="point"/>.</summary>
  public void LevelAt(Vector3 point)
  {
    var player = Player.m_localPlayer;
    if (player == null)
    {
      return;
    }

    if (!_frame.Placed)
    {
      player.Message(MessageHud.MessageType.Center, "Frame an area with the ground probe first.");
      return;
    }

    // Levelling with a frame that follows the aim would move the frame, and its target, with every
    // stroke; the first stroke pins it where it is.
    if (!_frame.Pinned)
    {
      Measure();
      _frame.SetPinned(true, _measurement?.Balance);
      player.Message(MessageHud.MessageType.TopLeft, "Frame pinned");
    }

    if (Target is not { } target)
    {
      player.Message(MessageHud.MessageType.Center, "The ground under the frame isn't loaded yet.");
      return;
    }

    var brush = BrushBounds(point);
    var tiles = TerrainTiles.Cover(brush, (x, z) => _frame.Contains(x, z));
    if (tiles.Count == 0)
    {
      player.Message(MessageHud.MessageType.Center, "Aim inside the frame.");
      return;
    }

    RcinelLevelPlugin.Instance.StartCoroutine(TerrainTiles.Level(tiles, target, _settings.OpsPerFrame.Value));
    _logger.LogDebug($"Levelling around ({Mathf.RoundToInt(point.x)}, {Mathf.RoundToInt(point.z)}) to {target:0.00} m with {tiles.Count} terrain edits.");
  }

  public void Dispose()
  {
    Hide();
    if (Current == this)
    {
      Current = null;
    }
  }

  private void Measure()
  {
    _measurement = _frame.Placed ? Probe.Measure(_frame, Target) : null;
    _measuredRevision = _frame.Revision;
    _measuredAt = Time.time;
  }

  private void HandleInput(bool probe)
  {
    var resizeHeld = Keys.Held(_settings.ResizeModifier.Value);
    var heightHeld = Keys.Held(_settings.HeightModifier.Value);
    ScrollPatch.Suppress = resizeHeld || heightHeld;

    if (probe && Keys.Pressed(_settings.CycleShape.Value))
    {
      _frame.CycleShape();
    }

    if (probe && Keys.Pressed(_settings.PinFrame.Value))
    {
      if (_frame.Pinned)
      {
        _frame.SetPinned(false, null);
      }
      else if (Aim(GroundMask) is { } point)
      {
        _frame.MoveTo(point);
        Measure();
        _frame.SetPinned(true, _measurement?.Balance);
      }
    }

    var scroll = ScrollPatch.Suppress ? ScrollPatch.Read() : 0f;
    if (scroll != 0f)
    {
      var step = scroll > 0f ? 1 : -1;
      if (resizeHeld)
      {
        _frame.Resize(step, Facing(), _settings.MaxRadius.Value);
      }
      else if (Target is { } height)
      {
        _frame.SetManualHeight(height + step * _settings.HeightStep.Value);
      }
    }

    if (Keys.Pressed(_settings.PickHeight.Value) && Aim(AnythingMask) is { } picked)
    {
      _frame.SetManualHeight(picked.y);
    }

    if (Keys.Pressed(_settings.ResetHeight.Value))
    {
      _frame.SetManualHeight(null);
      // Measure without the old target, then let a pinned frame take the fresh result.
      Measure();
      _frame.Remeasured(_measurement?.Balance);
    }
  }

  /// <summary>Where the hoe would level now: the build ghost's spot, as the game would place the piece.</summary>
  private (int MinX, int MinZ, int MaxX, int MaxZ)? Brush(Player player)
  {
    var ghost = player.m_placementGhost;
    if (ghost != null && ghost.activeSelf)
    {
      return BrushBounds(ghost.transform.position);
    }

    return Aim(GroundMask) is { } point ? BrushBounds(point) : null;
  }

  private (int MinX, int MinZ, int MaxX, int MaxZ) BrushBounds(Vector3 point)
  {
    int x = Mathf.RoundToInt(point.x), z = Mathf.RoundToInt(point.z), r = _settings.HoeBrush.Value;
    return (x - r, z - r, x + r, z + r);
  }

  private string Describe(bool probe, bool hoe)
  {
    var text = new StringBuilder();
    text.Append(probe ? "Ground probe" : "Level hoe").Append(" · ").Append(_frame.Describe())
      .Append(" · ").Append(_frame.Pinned ? "pinned" : "follows your aim");
    if (hoe)
    {
      var side = 2 * _settings.HoeBrush.Value + 1;
      text.Append($" · brush {side} × {side} m");
    }

    text.AppendLine();
    if (_measurement is { } m && Target is { } target)
    {
      text.Append($"Target {target:0.00} m ({TargetSource})").Append($" · dig {m.Cut:0} m³ · fill {m.Fill:0} m³");
      if (m.OutOfReach > 0)
      {
        text.Append($" · {m.OutOfReach} points beyond the game's ±{Probe.MaxChange:0} m limit");
      }

      text.AppendLine().Append($"Probe now: balance {m.Balance:0.00} m · ground {m.Lowest:0.0} to {m.Highest:0.0} m");
      if (m.Loaded < m.Expected)
      {
        text.Append($" · only {100 * m.Loaded / Math.Max(1, m.Expected)}% of the frame is loaded, the rest is too far away");
      }
    }
    else
    {
      text.Append("The ground under the frame isn't loaded yet.");
    }

    text.AppendLine().Append("<size=14>");
    if (probe)
    {
      text.Append($"[{_settings.CycleShape.Value}] shape   [{_settings.PinFrame.Value}] pin   ");
    }

    text.Append($"[{_settings.ResizeModifier.Value} + wheel] size   [{_settings.HeightModifier.Value} + wheel] height   ")
      .Append($"[{_settings.PickHeight.Value}] take height   [{_settings.ResetHeight.Value}] probe height");
    return text.Append("</size>").ToString();
  }

  private void Hide()
  {
    _view.Hide();
    _readout.Hide();
  }

  /// <summary>The direction the camera looks along, for the rectangle's sides.</summary>
  private static Vector3 Facing() => GameCamera.instance != null ? GameCamera.instance.transform.forward : Vector3.forward;

  private static Vector3? Aim(int mask)
  {
    var camera = GameCamera.instance;
    if (camera == null)
    {
      return null;
    }

    var ray = new Ray(camera.transform.position, camera.transform.forward);
    return Physics.Raycast(ray, out var hit, AimDistance, mask) ? hit.point : null;
  }

  /// <summary>Not while typing, in a menu or picking a piece.</summary>
  private static bool TakesInput() =>
    !(Chat.instance != null && Chat.instance.HasFocus())
    && !global::Console.IsVisible()
    && !InventoryGui.IsVisible()
    && !Menu.IsVisible()
    && !TextInput.IsVisible()
    && !Minimap.IsOpen()
    && !StoreGui.IsVisible()
    && !Hud.IsPieceSelectionVisible();
}
