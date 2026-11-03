using System;
using System.Collections.Generic;
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
  private const string StoneName = "$item_stone";

  private static readonly int GroundMask = LayerMask.GetMask("terrain");
  private static readonly int AnythingMask = LayerMask.GetMask("terrain", "piece", "piece_nonsolid", "static_solid", "Default", "Default_small");

  private readonly Settings _settings;
  private readonly ManualLogSource _logger;
  private readonly Frame _frame = new();
  private readonly FrameView _view = new();
  private readonly Readout _readout = new();
  private readonly ReachOverlay _overlay;
  private Measurement? _measurement;
  private int _measuredRevision = -1;
  private float _measuredAt;
  private (float Fill, int Stone)? _nextStroke;
  private float _plannedAt;
  private Player? _player;
  private ZNet? _session;
  private int _savedRevision = -1;

  public Controls(Settings settings, ManualLogSource logger)
  {
    _settings = settings;
    _logger = logger;
    _overlay = new ReachOverlay(logger);
    Current = this;
    settings.Reloaded += ToolHints.Invalidate;
  }

  /// <summary>The one instance, for <see cref="LevelAction"/>, which the game creates.</summary>
  public static Controls? Current { get; private set; }

  public Frame Frame => _frame;

  /// <summary>The floor the pickaxe may not dig below inside the frame, while the lock is on.</summary>
  public float? LockedDepth => _frame.Pinned && _frame.DepthLocked ? Target : null;

  /// <summary>The height the hoe levels to: set by hand, else the probe's result from when the frame was pinned, else the live one.</summary>
  private float? Target => _frame.ManualHeight ?? _frame.PinnedBalance ?? _measurement?.Balance;

  private string TargetSource => _frame.ManualHeight is not null ? "by hand" : _frame.PinnedBalance is not null ? "probe, at pinning" : "probe";

  public void Update()
  {
    ScrollPatch.Suppress = false;
    ToolHints.Current = null;
    var player = Player.m_localPlayer;
    if (player == null || player.IsDead())
    {
      Hide();
      return;
    }

    KeepFrame(player);

    var probe = Items.IsProbe(player.GetLeftItem());
    var hoe = Items.IsLevelHoe(player.GetRightItem());
    // A pickaxe is two-handed, so the probe can't be held with it; the locked frame shows anyway.
    var pickaxe = LockedDepth is not null && player.GetRightItem()?.m_shared.m_skillType == Skills.SkillType.Pickaxes;
    if (!probe && !hoe && !pickaxe)
    {
      Hide();
      return;
    }

    _frame.Limit(_settings.MaxRadius.Value);
    MoatSizes.Depth = _settings.MoatDepth.Value;
    if (TakesInput() && (probe || hoe))
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

    var brush = hoe ? Brush(player) : null;
    if (brush is { } b && Target is { } target && Time.time - _plannedAt > 0.25f)
    {
      var plan = Stroke.Plan(_frame, b, target, _settings.StoneCost.Value, _settings.FreeDepth.Value, DevModes.Dig);
      _nextStroke = (plan.Fill, plan.Stone);
      _plannedAt = Time.time;
    }

    _view.Show(_frame, Target, brush);
    if (probe && _measurement is { } measured)
    {
      _overlay.Show(measured);
    }
    else
    {
      _overlay.Hide();
    }

    _readout.Show(Describe(probe, hoe, pickaxe, player));
    ToolHints.Current = probe || hoe ? Hints(probe, hoe) : null;
  }

  /// <summary>
  /// Keeps the frame with the character, one per world, so it is there again after leaving and joining
  /// (owner, 2026-11-03). It goes into the character's own custom data, which the game saves with the
  /// character; the key holds the world's id, which clients get from the server too (<c>ZNet.World</c>).
  /// </summary>
  private void KeepFrame(Player player)
  {
    var world = ZNet.World;
    if (world is null)
    {
      return;
    }

    // Dev switches end with the session they were used in: a new ZNet is a new world joined, while a
    // new Player can also just be a respawn after dying.
    if (!ReferenceEquals(ZNet.instance, _session))
    {
      _session = ZNet.instance;
      DevModes.Reset();
    }

    var key = $"RcinelLevel.Frame.{world.m_uid}";
    if (!ReferenceEquals(player, _player))
    {
      // A character entering a world (or respawning): take its frame for this world, if it has one.
      _player = player;
      if (player.m_customData.TryGetValue(key, out var saved) && !_frame.Load(saved))
      {
        _logger.LogWarning($"The saved frame for this world couldn't be read and was dropped: {saved}");
      }

      _savedRevision = _frame.Revision;
      return;
    }

    if (_frame.Placed && _frame.Revision != _savedRevision)
    {
      player.m_customData[key] = _frame.Save();
      _savedRevision = _frame.Revision;
    }
  }

  /// <summary>The controls for the key hint bar, in the order the tools are used.</summary>
  private List<(string[] Keys, string Label)> Hints(bool probe, bool hoe)
  {
    var hints = new List<(string[], string)>();
    if (hoe)
    {
      hints.Add(([ZInput.instance.GetBoundKeyString("Attack", emptyStringOnMissing: false)], "Fill"));
    }

    if (probe)
    {
      hints.Add(([KeyName(_settings.CycleShape.Value)], "Shape"));
      hints.Add(([KeyName(_settings.PinFrame.Value)], _frame.Pinned ? "Unpin" : "Pin"));
      hints.Add(([KeyName(_settings.LockDepth.Value)], _frame.DepthLocked ? "Unlock pickaxe" : "Lock pickaxe"));
      hints.Add(([KeyName(_settings.CycleMoat.Value)], "Moat: " + MoatName(_frame.Moat switch
      {
        MoatKind.None => MoatKind.Rustic,
        MoatKind.Rustic => MoatKind.Drawbridge,
        _ => MoatKind.None,
      })));
    }

    hints.Add(([KeyName(_settings.ResizeModifier.Value), "Wheel"], "Size"));
    hints.Add(([KeyName(_settings.HeightModifier.Value), "Wheel"], "Height"));
    hints.Add(([KeyName(_settings.PickHeight.Value)], "Take height"));
    hints.Add(([KeyName(_settings.ResetHeight.Value)], "Probe height"));
    return hints;
  }

  /// <summary>The game's own name of the bridge a moat is sized for, in the player's language.</summary>
  private static string MoatName(MoatKind kind) => kind switch
  {
    MoatKind.Rustic => Localization.instance.Localize("$piece_drawbridge_log"),
    MoatKind.Drawbridge => Localization.instance.Localize("$piece_drawbridge"),
    _ => "none",
  };

  /// <summary>"Left Alt" for LeftAlt, "1" for Alpha1: what the key says on the keyboard.</summary>
  private static string KeyName(KeyCode key)
  {
    var name = key.ToString();
    if (name.StartsWith("Alpha", StringComparison.Ordinal) && name.Length == 6)
    {
      return name.Substring(5);
    }

    var spaced = new StringBuilder();
    foreach (var c in name)
    {
      if (char.IsUpper(c) && spaced.Length > 0)
      {
        spaced.Append(' ');
      }

      spaced.Append(c);
    }

    return spaced.ToString();
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
    // With the dev dig the moat counts as well: the hoe digs it then.
    bool Reachable(int x, int z) => _frame.Contains(x, z) || (DevModes.Dig && _frame.InMoat(x, z));
    if (!Reachable(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.z)) && TerrainTiles.Cover(brush, Reachable).Count == 0)
    {
      player.Message(MessageHud.MessageType.Center, "Aim inside the frame.");
      return;
    }

    var (tiles, fill, stone, paint) = Stroke.Plan(_frame, brush, target, _settings.StoneCost.Value, _settings.FreeDepth.Value, DevModes.Dig);
    if (tiles.Count == 0 && paint.Count == 0)
    {
      // Ground at or just above the target is done, not a mistake: only say something when there is
      // real digging left (owner, 2026-11-03).
      if (!DevModes.Dig && HighestUnder(brush) > target + Stroke.NearTarget)
      {
        player.Message(MessageHud.MessageType.Center, "Nothing here lies below the target. Dig with the pickaxe.");
      }

      return;
    }

    // Raising ground costs stone like the game's Raise ground; no-cost mode (devcommands) skips it.
    if (stone > 0 && !player.NoCostCheat() && !DevModes.NoStone)
    {
      var inventory = player.GetInventory();
      var have = inventory.CountItems(StoneName);
      if (have < stone)
      {
        player.Message(MessageHud.MessageType.Center, $"This needs {stone} stone, you have {have}.");
        return;
      }

      inventory.RemoveItem(StoneName, stone);
    }

    RcinelLevelPlugin.Instance.StartCoroutine(TerrainTiles.Level(tiles, _settings.OpsPerFrame.Value, paint));
    _plannedAt = 0f;
    _logger.LogDebug($"Filling {fill:0.0} m³ around ({Mathf.RoundToInt(point.x)}, {Mathf.RoundToInt(point.z)}) to {target:0.00} m for {stone} stone with {tiles.Count} terrain edits.");
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

    if (probe && Keys.Pressed(_settings.LockDepth.Value))
    {
      if (!_frame.Pinned && _frame.Placed)
      {
        Measure();
        _frame.SetPinned(true, _measurement?.Balance);
      }

      _frame.SetDepthLocked(!_frame.DepthLocked);
    }

    if (probe && Keys.Pressed(_settings.CycleMoat.Value))
    {
      _frame.CycleMoat();
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

  /// <summary>The highest ground under the brush, inside the frame.</summary>
  private float HighestUnder((int MinX, int MinZ, int MaxX, int MaxZ) brush)
  {
    var highest = float.MinValue;
    for (var z = brush.MinZ; z <= brush.MaxZ; z++)
    {
      for (var x = brush.MinX; x <= brush.MaxX; x++)
      {
        if (_frame.Contains(x, z) && Probe.At(x, z) is { } ground)
        {
          highest = Math.Max(highest, ground.Height);
        }
      }
    }

    return highest;
  }

  private (int MinX, int MinZ, int MaxX, int MaxZ) BrushBounds(Vector3 point)
  {
    int x = Mathf.RoundToInt(point.x), z = Mathf.RoundToInt(point.z), r = _settings.HoeBrush.Value;
    return (x - r, z - r, x + r, z + r);
  }

  private string Describe(bool probe, bool hoe, bool pickaxe, Player player)
  {
    var text = new StringBuilder();
    text.Append(probe ? "Ground probe" : hoe ? "Level hoe" : "Pickaxe").Append(" · ").Append(_frame.Describe())
      .Append(" · ").Append(_frame.Pinned ? "pinned" : "follows your aim");
    if (_frame.DepthLocked && Target is { } floor)
    {
      text.Append($" · pickaxe locked at {floor:0.00} m");
    }

    if (_frame.Moat != MoatKind.None)
    {
      text.Append($" · moat {_frame.MoatWidth} m ({MoatName(_frame.Moat)})");
    }

    if (DevModes.NoStone || DevModes.Dig)
    {
      text.Append(" · dev:").Append(DevModes.NoStone ? " no stone" : "").Append(DevModes.Dig ? " dig" : "");
    }

    text.AppendLine();
    if (_measurement is { } m && Target is { } target)
    {
      text.Append($"Target {target:0.00} m ({TargetSource})").Append($" · dig {m.Cut:0} m³ · fill {m.Fill:0} m³");
      if (_frame.Moat != MoatKind.None)
      {
        text.AppendLine().Append($"Moat {_frame.MoatWidth} m wide, floor {target - MoatSizes.Depth:0.00} m ({MoatSizes.Depth:0.#} m deep): dig {m.MoatDig:0} m³");
        if (m.MoatTooDeep > 0)
        {
          text.Append($" · {m.MoatTooDeep} m² can't be dug that deep (red)");
        }
      }

      if (m.OutOfReach > 0)
      {
        // The game moves no vertex more than 8 m from the generated ground; the probe shows them in red.
        text.AppendLine().Append($"Beyond the game's ±{Probe.MaxChange:0} m limit (red on the ground):");
        if (m.TooLow > 0)
        {
          text.Append($" {m.TooLow} m² can't be raised that far");
        }

        if (m.TooHigh > 0)
        {
          text.Append(m.TooLow > 0 ? "," : string.Empty).Append($" {m.TooHigh} m² can't be dug that deep");
        }
      }

      if (hoe && _nextStroke is { } next)
      {
        var side = 2 * _settings.HoeBrush.Value + 1;
        var price = DevModes.NoStone || player.NoCostCheat() ? 0 : next.Stone;
        text.AppendLine().Append($"Next stroke ({side} × {side} m): fill {next.Fill:0.0} m³ for {price} stone")
          .Append($" · you carry {player.GetInventory().CountItems(StoneName)}");
      }
      else
      {
        text.AppendLine().Append($"Probe now: balance {m.Balance:0.00} m · ground {m.Lowest:0.0} to {m.Highest:0.0} m");
      }

      if (m.Loaded < m.Expected)
      {
        text.Append($" · only {100 * m.Loaded / Math.Max(1, m.Expected)}% of the frame is loaded, the rest is too far away");
      }
    }
    else
    {
      text.Append("The ground under the frame isn't loaded yet.");
    }

    // The controls are in the key hint bar at the bottom (ToolHints), as the game shows its own.
    return text.ToString();
  }

  private void Hide()
  {
    _view.Hide();
    _overlay.Hide();
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
