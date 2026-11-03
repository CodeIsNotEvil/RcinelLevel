using System;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace Rcinel.Level;

/// <summary>
/// What the player does with the probe and the level hoe (decision 0012), and what they see of it.
/// </summary>
/// <remarks>
/// <para>
/// While the probe is in the left hand the frame follows the aim until it is pinned; the hoe levels
/// the frame the probe last showed. Resizing and the height work with either tool held. Every key is a
/// config entry (<see cref="Settings"/>).
/// </para>
/// <para>
/// The probe measures again whenever the frame changes and twice a second otherwise, because the
/// ground under it changes as the player (or anyone else) digs.
/// </para>
/// </remarks>
internal sealed class Controls : IDisposable
{
  private const float MeasureInterval = 0.5f;
  private const float AimDistance = 60f;

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

  /// <summary>The height the hoe levels to: set by hand, else the probe's balance.</summary>
  private float? Target => _frame.ManualHeight ?? _measurement?.Balance;

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
      _measurement = _frame.Placed ? Probe.Measure(_frame, _frame.ManualHeight) : null;
      _measuredRevision = _frame.Revision;
      _measuredAt = Time.time;
    }

    if (!_frame.Placed)
    {
      _view.Hide();
      _readout.Show("Hold the ground probe and aim at the ground to frame an area.");
      return;
    }

    _view.Show(_frame, Target);
    _readout.Show(Describe(probe));
  }

  /// <summary>Called when the level hoe's piece is placed.</summary>
  public void LevelFrame()
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

    var target = Target;
    if (target is null)
    {
      player.Message(MessageHud.MessageType.Center, "The ground under the frame isn't loaded yet.");
      return;
    }

    var tiles = TerrainTiles.Cover(_frame);
    RcinelLevelPlugin.Instance.StartCoroutine(TerrainTiles.Level(tiles, target.Value, _settings.OpsPerFrame.Value));
    _logger.LogInfo($"Levelling {_frame.Describe()} at ({_frame.CenterX}, {_frame.CenterZ}) to {target.Value:0.00} m with {tiles.Count} terrain edits.");
    player.Message(MessageHud.MessageType.TopLeft, $"Levelling to {target.Value:0.00} m");
  }

  public void Dispose()
  {
    Hide();
    if (Current == this)
    {
      Current = null;
    }
  }

  private void HandleInput(bool probe)
  {
    var resizeHeld = ZInput.GetKey(_settings.ResizeModifier.Value, logWarning: false);
    var heightHeld = ZInput.GetKey(_settings.HeightModifier.Value, logWarning: false);
    ScrollPatch.Suppress = resizeHeld || heightHeld;

    if (probe && Pressed(_settings.CycleShape.Value))
    {
      _frame.CycleShape();
    }

    if (probe && Pressed(_settings.PinFrame.Value))
    {
      if (_frame.Pinned)
      {
        _frame.SetPinned(false);
      }
      else if (Aim(GroundMask) is { } point)
      {
        _frame.MoveTo(point);
        _frame.SetPinned(true);
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

    if (Pressed(_settings.PickHeight.Value) && Aim(AnythingMask) is { } picked)
    {
      _frame.SetManualHeight(picked.y);
    }

    if (Pressed(_settings.ResetHeight.Value))
    {
      _frame.SetManualHeight(null);
    }
  }

  private string Describe(bool probe)
  {
    var text = new StringBuilder();
    text.Append(probe ? "Ground probe" : "Level hoe").Append(" · ").Append(_frame.Describe());
    if (probe)
    {
      text.Append(" · ").Append(_frame.Pinned ? "pinned" : "follows your aim");
    }

    text.AppendLine();
    if (_measurement is { } m && Target is { } target)
    {
      text.Append($"Target {target:0.00} m ({(_frame.ManualHeight is null ? "probe" : "by hand")})")
        .Append($" · dig {m.Cut:0.0} m³ · fill {m.Fill:0.0} m³");
      if (m.OutOfReach > 0)
      {
        text.Append($" · {m.OutOfReach} points beyond the game's ±{Probe.MaxChange:0} m limit");
      }

      text.AppendLine().Append($"Probe: balance {m.Balance:0.00} m · ground {m.Lowest:0.0} to {m.Highest:0.0} m");
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
    if (!probe)
    {
      text.Append("   · place to level");
    }

    return text.Append("</size>").ToString();
  }

  private void Hide()
  {
    _view.Hide();
    _readout.Hide();
  }

  private static bool Pressed(KeyCode key) => key != KeyCode.None && ZInput.GetKeyDown(key, logWarning: false);

  /// <summary>The cardinal direction the camera looks along, for the rectangle's sides.</summary>
  private static Vector3 Facing() => GameCamera.instance != null ? GameCamera.instance.transform.forward : Vector3.forward;

  private static readonly int GroundMask = LayerMask.GetMask("terrain");
  private static readonly int AnythingMask = LayerMask.GetMask("terrain", "piece", "piece_nonsolid", "static_solid", "Default", "Default_small");

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
