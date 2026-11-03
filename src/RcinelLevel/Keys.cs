using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Rcinel.Level;

/// <summary>
/// Reads the configured keys so that a letter means the key with that letter on the player's own
/// keyboard.
/// </summary>
/// <remarks>
/// Valheim reads keys through Unity's Input System, whose <c>Key</c> values are positions on a US
/// keyboard (<c>ZInput.TryGetKeyStateLowLevel</c> maps <c>KeyCode.Y</c> to <c>Key.Y</c>). On a German
/// keyboard that position is labelled Z, so "Y" in the config did nothing for the owner (2026-11-03).
/// Letters and digits are therefore looked up by what the key types on the current layout; everything
/// else (Alt, Shift, F-keys) has no layout and goes through <c>ZInput</c> as the game's own keys do.
/// </remarks>
internal static class Keys
{
  private static readonly Dictionary<KeyCode, KeyControl?> ByLayout = new();
  private static string? _layout;

  public static bool Held(KeyCode key) => key != KeyCode.None && (Find(key) is { } control
    ? control.isPressed
    : ZInput.GetKey(key, logWarning: false));

  public static bool Pressed(KeyCode key) => key != KeyCode.None && (Find(key) is { } control
    ? control.wasPressedThisFrame
    : ZInput.GetKeyDown(key, logWarning: false));

  /// <summary>
  /// The key that types the letter or digit on the current layout, or null for keys without one (and
  /// letters the layout doesn't have), which are then read by position like the game's own.
  /// </summary>
  private static KeyControl? Find(KeyCode key)
  {
    var keyboard = Keyboard.current;
    if (keyboard is null || Character(key) is not { } character)
    {
      return null;
    }

    // The player can switch layouts while playing; look the keys up again then.
    if (keyboard.keyboardLayout != _layout)
    {
      _layout = keyboard.keyboardLayout;
      ByLayout.Clear();
    }

    if (!ByLayout.TryGetValue(key, out var control))
    {
      control = keyboard.FindKeyOnCurrentKeyboardLayout(character);
      ByLayout[key] = control;
    }

    return control;
  }

  private static string? Character(KeyCode key) => key switch
  {
    >= KeyCode.A and <= KeyCode.Z => ((char)('a' + (key - KeyCode.A))).ToString(),
    >= KeyCode.Alpha0 and <= KeyCode.Alpha9 => ((char)('0' + (key - KeyCode.Alpha0))).ToString(),
    _ => null,
  };
}
