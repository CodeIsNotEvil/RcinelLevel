using System.IO;
using BepInEx;
using HarmonyLib;
using Jotunn;
using Jotunn.Managers;
using Jotunn.Utils;

namespace Rcinel.Level;

/// <summary>
/// RcinelLevel: a probe that finds the height where levelling an area digs away as much as it raises,
/// and a level hoe that levels the probe's frame to that height (or one set by hand) with walls as
/// steep as Valheim's terrain allows.
/// </summary>
/// <remarks>
/// <para>
/// Every player and the server need the mod (decision 0005): a terrain edit is applied by whoever owns
/// that patch of terrain, which looks the op up by prefab hash, so Jotunn enforces the same version
/// everywhere.
/// </para>
/// <para>
/// The second brain (<c>.claude/second-brain/</c>) has the why of every part: the terrain rules in
/// <c>resources/terrain-and-multiplayer.md</c>, the decisions 0002 to 0012.
/// </para>
/// </remarks>
[BepInPlugin(Guid, Name, Version)]
[BepInDependency(Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public sealed class RcinelLevelPlugin : BaseUnityPlugin
{
  public const string Guid = "io.github.codeisnotevil.rcinellevel";
  public const string Name = "RcinelLevel";

  /// <summary>Must match <c>&lt;Version&gt;</c> in Directory.Build.props.</summary>
  public const string Version = "0.1.0";

  internal static RcinelLevelPlugin Instance { get; private set; } = null!;

  internal Settings Settings { get; private set; } = null!;

  private Controls? _controls;

  private void Awake()
  {
    Instance = this;
    Settings = new Settings(Config, Logger);
    var files = new PluginFiles(Path.GetDirectoryName(Info.Location)!, Logger);
    Texts.Register();
    DevModes.Register();
    Items.Prepare(files, Settings, Logger);
    PrefabManager.OnVanillaPrefabsAvailable += Items.Create;
    new Harmony(Guid).PatchAll(typeof(RcinelLevelPlugin).Assembly);
    _controls = new Controls(Settings, Logger);

    // Iron Gate asks every mod that changes gameplay to say so. It also switches off Steam achievements
    // for the session (Achievements.IsCheatedAtAll); the Thunderstore page says that.
    Game.isModded = true;
  }

  private void Update()
  {
    Settings.ReloadIfChanged();
    _controls?.Update();
  }

  private void OnDestroy()
  {
    _controls?.Dispose();
    Settings.Dispose();
  }
}
