using System.Collections.Generic;
using Jotunn.Managers;

namespace Rcinel.Level;

/// <summary>The texts players see, as Valheim localization tokens. English only for now.</summary>
internal static class Texts
{
  public const string ProbeName = "$item_rcinellevel_probe";
  public const string ProbeDescription = "$item_rcinellevel_probe_description";
  public const string HoeName = "$item_rcinellevel_levelhoe";
  public const string HoeDescription = "$item_rcinellevel_levelhoe_description";
  public const string LevelPieceName = "$piece_rcinellevel_level";
  public const string LevelPieceDescription = "$piece_rcinellevel_level_description";

  public static void Register() =>
    LocalizationManager.Instance.GetLocalization().AddTranslation("English", new Dictionary<string, string>
    {
      [ProbeName.Substring(1)] = "Ground probe",
      [ProbeDescription.Substring(1)] =
        "A long rod pushed into the ground to feel how it lies. Held in the off hand, it frames an area and finds the height where levelling it digs away as much as it fills.",
      [HoeName.Substring(1)] = "Level hoe",
      [HoeDescription.Substring(1)] =
        "A hoe with a square flint scoop. Levels the probe's frame to one height, with walls as steep as the ground allows.",
      [LevelPieceName.Substring(1)] = "Level the frame",
      [LevelPieceDescription.Substring(1)] = "Levels everything inside the probe's frame to the target height.",
    });
}
