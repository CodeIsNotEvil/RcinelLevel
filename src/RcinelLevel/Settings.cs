using System;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace Rcinel.Level;

/// <summary>
/// RcinelLevel's config file (<c>BepInEx/config/io.github.codeisnotevil.rcinellevel.cfg</c>).
/// </summary>
/// <remarks>
/// <para>
/// Keys and the model placement are each player's own. The largest frame is admin-only, so Jotunn
/// syncs the server's value to every client: it decides how much terrain one click may change.
/// </para>
/// <para>
/// The file is watched while the game runs, so the model placement can be tuned in a text editor and
/// seen after re-equipping the tool, without a restart. Key defaults avoid every key Valheim binds by
/// default (decision 0012; G, T and Left Shift were taken).
/// </para>
/// </remarks>
internal sealed class Settings : IDisposable
{
  private readonly ConfigFile _config;
  private readonly ManualLogSource _logger;
  private readonly FileSystemWatcher? _watcher;
  private volatile bool _changed;

  public Settings(ConfigFile config, ManualLogSource logger)
  {
    _config = config;
    _logger = logger;

    CycleShape = config.Bind("Keys", "Cycle shape", KeyCode.Y,
      "While holding the probe: switch the frame between circle, square and rectangle.");
    PinFrame = config.Bind("Keys", "Pin frame", KeyCode.B,
      "While holding the probe: pin the frame where you aim, or pick it up again so it follows your aim.");
    ResizeModifier = config.Bind("Keys", "Resize modifier", KeyCode.LeftAlt,
      "Hold and scroll to resize the frame. A rectangle only grows or shrinks on the side you face.");
    HeightModifier = config.Bind("Keys", "Height modifier", KeyCode.H,
      "Hold and scroll to raise or lower the target height by the height step.");
    PickHeight = config.Bind("Keys", "Take height from aim", KeyCode.J,
      "Set the target height to the ground or building piece you aim at.");
    ResetHeight = config.Bind("Keys", "Reset height to probe", KeyCode.U,
      "Set the target height back to the probe's cut-and-fill result.");
    HeightStep = config.Bind("Controls", "Height step", 0.25f,
      new ConfigDescription("Metres per scroll step with the height modifier.", new AcceptableValueRange<float>(0.05f, 2f)));

    MaxRadius = config.Bind("Server", "Largest frame", 16,
      new ConfigDescription(
        "How far the frame may reach from its centre, in metres (a square of 16 is 33 by 33 m). Synced from the server.",
        new AcceptableValueRange<int>(1, 64),
        new ConfigurationManagerAttributes { IsAdminOnly = true }));
    OpsPerFrame = config.Bind("Performance", "Terrain edits per frame", 8,
      new ConfigDescription(
        "How many terrain edits the hoe sends per rendered frame. Lower it if levelling a large frame stutters.",
        new AcceptableValueRange<int>(1, 64)));

    // Starting points from the game's own prefabs (read with UnityPy, 2026-11-03). The probe sits under
    // the torch's attach point, where the torch's rod runs along +Z from the hand: +90° about X turns the
    // model's +Y (handle up) onto it. The hoe's model sits under the game hoe's "visual", whose handle
    // runs along Z with the blade towards -Y and the hand at z = -0.51: (-90, 0, 180) puts the head at +Z
    // and the blade on the same side. Tuned in the game from there (decision 0011).
    ProbePosition = config.Bind("Model placement", "Probe position", Vector3.zero,
      "Offset of the probe's model in the hand, in metres. Applied when the file is saved; re-equip to see it.");
    ProbeRotation = config.Bind("Model placement", "Probe rotation", new Vector3(90f, 0f, 0f),
      "Rotation of the probe's model in the hand, in degrees (x, y, z).");
    HoePosition = config.Bind("Model placement", "Level hoe position", new Vector3(0f, 0f, -0.51f),
      "Offset of the level hoe's model in the hand, in metres.");
    HoeRotation = config.Bind("Model placement", "Level hoe rotation", new Vector3(-90f, 0f, 180f),
      "Rotation of the level hoe's model in the hand, in degrees (x, y, z).");

    try
    {
      _watcher = new FileSystemWatcher(Path.GetDirectoryName(config.ConfigFilePath)!, Path.GetFileName(config.ConfigFilePath))
      {
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
        EnableRaisingEvents = true,
      };
      // Raised on a thread-pool thread; Unity objects may only be touched on the main thread, so this
      // only sets a flag that Update picks up.
      _watcher.Changed += (_, _) => _changed = true;
    }
    catch (Exception e)
    {
      _logger.LogWarning($"The config file isn't watched, so changes need a restart: {e.Message}");
    }
  }

  public ConfigEntry<KeyCode> CycleShape { get; }
  public ConfigEntry<KeyCode> PinFrame { get; }
  public ConfigEntry<KeyCode> ResizeModifier { get; }
  public ConfigEntry<KeyCode> HeightModifier { get; }
  public ConfigEntry<KeyCode> PickHeight { get; }
  public ConfigEntry<KeyCode> ResetHeight { get; }
  public ConfigEntry<float> HeightStep { get; }
  public ConfigEntry<int> MaxRadius { get; }
  public ConfigEntry<int> OpsPerFrame { get; }
  public ConfigEntry<Vector3> ProbePosition { get; }
  public ConfigEntry<Vector3> ProbeRotation { get; }
  public ConfigEntry<Vector3> HoePosition { get; }
  public ConfigEntry<Vector3> HoeRotation { get; }

  /// <summary>Raised on the main thread after the file was edited and read again.</summary>
  public event Action? Reloaded;

  public void ReloadIfChanged()
  {
    if (!_changed)
    {
      return;
    }

    _changed = false;
    try
    {
      _config.Reload();
      Reloaded?.Invoke();
    }
    catch (Exception e)
    {
      // A half-written file (the editor still saving) parses badly; the next write triggers again.
      _logger.LogWarning($"Couldn't read the changed config file: {e.Message}");
    }
  }

  public void Dispose() => _watcher?.Dispose();
}
