using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rcinel.Level;

/// <summary>
/// The tools' controls in the game's key hint bar at the bottom of the screen, in place of the build
/// hammer's hints (which have nothing to do with the level hoe) and the combat hints (owner, 2026-11-03).
/// </summary>
/// <remarks>
/// <para>
/// The rows are built from the game's own templates, so they look like its hints: the build hints'
/// <c>Keyboard</c> row holds <c>Place</c> (one key and a label), <c>Copy</c> (key + key) and
/// <c>rotate</c> (the mouse wheel), read from <c>IngameGui_HUD.prefab</c> with UnityPy.
/// </para>
/// <para>
/// Jotunn's key hint manager was the first choice but only takes the left-hand item when it is a weapon,
/// and the probe is a shield to the game (decision 0013). One row is built per set of hints and kept
/// until the keys change in the config file.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
internal static class ToolHints
{
  private static readonly Dictionary<string, GameObject> Rows = new();

  /// <summary>Set each frame by <see cref="Controls"/>: the hints to show, or null for the game's own.</summary>
  public static List<(string[] Keys, string Label)>? Current { get; set; }

  /// <summary>The keys in the config file changed: build the rows again.</summary>
  public static void Invalidate()
  {
    foreach (var row in Rows.Values)
    {
      if (row != null)
      {
        Object.Destroy(row);
      }
    }

    Rows.Clear();
  }

  private static void Postfix(KeyHints __instance)
  {
    var wanted = Current;
    var key = wanted is null ? null : string.Join("|", wanted.ConvertAll(h => string.Join("+", h.Keys) + "=" + h.Label));
    foreach (var pair in Rows)
    {
      if (pair.Value != null)
      {
        pair.Value.SetActive(pair.Key == key);
      }
    }

    if (wanted is null || key is null || !__instance.m_keyHintsEnabled)
    {
      return;
    }

    if (!Rows.TryGetValue(key, out var shown) || shown == null)
    {
      shown = Build(__instance, wanted);
      if (shown == null)
      {
        return;
      }

      Rows[key] = shown;
    }

    __instance.m_buildHints.SetActive(false);
    __instance.m_combatHints.SetActive(false);
    shown.SetActive(true);
  }

  private static GameObject? Build(KeyHints hints, List<(string[] Keys, string Label)> entries)
  {
    var source = hints.m_buildHints;
    var keyboard = source.transform.Find("Keyboard");
    var single = keyboard?.Find("Place");
    var pair = keyboard?.Find("Copy");
    var wheel = keyboard?.Find("rotate");
    if (single is null || pair is null || wheel is null)
    {
      return null;
    }

    var row = Object.Instantiate(source, source.transform.parent, worldPositionStays: false);
    row.name = "RcinelLevel hints";
    // The row only shows what this class puts in it: no switching to gamepad hints, no localizer
    // rewriting the labels from the game's tokens.
    foreach (var component in row.GetComponentsInChildren<MonoBehaviour>(true))
    {
      var type = component.GetType().Name;
      if (type == "UIInputHint" || type.Contains("Locali"))
      {
        Object.DestroyImmediate(component);
      }
    }

    var rowKeyboard = row.transform.Find("Keyboard");
    foreach (Transform child in row.transform)
    {
      child.gameObject.SetActive(child == rowKeyboard);
    }

    var templates = new List<Transform>();
    foreach (Transform child in rowKeyboard)
    {
      templates.Add(child);
    }

    foreach (var (keys, label) in entries)
    {
      var template = keys.Length == 2 && keys[1] == "Wheel" && keys[0] == "" ? wheel : keys.Length >= 2 ? pair : single;
      var entry = Object.Instantiate(template.gameObject, rowKeyboard, worldPositionStays: false);
      entry.name = label;
      entry.SetActive(true);
      var texts = entry.GetComponentsInChildren<TMP_Text>(true);
      // Template order: the label first, then each key (and the "+" text between keys in Copy).
      var keyTexts = new List<TMP_Text>();
      var labelSet = false;
      foreach (var text in texts)
      {
        if (text.transform.parent != null && text.transform.parent.name.StartsWith("key_bkg"))
        {
          keyTexts.Add(text);
        }
        else if (!labelSet)
        {
          text.text = label;
          labelSet = true;
        }
      }

      for (var i = 0; i < keyTexts.Count; i++)
      {
        keyTexts[i].text = i < keys.Length ? keys[i] : string.Empty;
      }
    }

    foreach (var template in templates)
    {
      Object.Destroy(template.gameObject);
    }

    return row;
  }
}
