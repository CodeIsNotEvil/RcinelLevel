using HarmonyLib;

namespace Rcinel.Level;

/// <summary>
/// Lets the probe (left hand) and the level hoe (right hand) be held together, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <c>Humanoid.EquipItem</c> empties both hands for a tool, and for a shield it empties the right hand
/// unless that holds a one-handed weapon or a torch. Both would drop the other of our two tools. So
/// for exactly these two items the other one is taken out of the hand before the game's code runs,
/// which then has nothing of ours to unequip, and put back afterwards. Everything else the game does
/// on equip (checks, effects, unequipping any other item) is untouched.
/// </para>
/// <para>
/// The put-back runs <c>SetupEquipment</c> again, which shows the item, sets the animation (the
/// probe's left-hand torch pose) and switches build mode on for the hoe.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
internal static class TwoToolsPatch
{
  private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, out (ItemDrop.ItemData? Left, ItemDrop.ItemData? Right) __state)
  {
    __state = (null, null);
    if (Items.IsLevelHoe(item) && Items.IsProbe(__instance.m_leftItem))
    {
      __state = (__instance.m_leftItem, null);
      __instance.m_leftItem = null;
    }
    else if (Items.IsProbe(item) && Items.IsLevelHoe(__instance.m_rightItem))
    {
      __state = (null, __instance.m_rightItem);
      __instance.m_rightItem = null;
    }
  }

  private static void Postfix(Humanoid __instance, (ItemDrop.ItemData? Left, ItemDrop.ItemData? Right) __state)
  {
    if (__state.Left is null && __state.Right is null)
    {
      return;
    }

    // If the game refused the new item, the hand we emptied is still empty: put ours back anyway.
    if (__state.Left is not null && __instance.m_leftItem is null)
    {
      __instance.m_leftItem = __state.Left;
    }

    if (__state.Right is not null && __instance.m_rightItem is null)
    {
      __instance.m_rightItem = __state.Right;
    }

    __instance.SetupEquipment();
  }
}

/// <summary>
/// Adds the level tiles to the game's table of terrain ops whenever it is rebuilt, so the terrain's
/// owner can look them up by hash (resources/terrain-and-multiplayer.md).
/// </summary>
[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.UpdateRegisters))]
internal static class RegisterTilesPatch
{
  private static void Prefix(ObjectDB __instance)
  {
    foreach (var tile in TerrainTiles.Prefabs.Values)
    {
      if (tile != null && !__instance.m_terrainOps.Contains(tile))
      {
        __instance.m_terrainOps.Add(tile);
      }
    }
  }
}

/// <summary>
/// Hides the scroll wheel from the game while a frame modifier is held with one of our tools, so the
/// camera doesn't zoom while the frame is resized (decision 0012). <see cref="Controls"/> reads the
/// wheel itself through <see cref="Read"/>.
/// </summary>
[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
internal static class ScrollPatch
{
  private static bool _reading;

  /// <summary>Set each frame by <see cref="Controls"/>.</summary>
  public static bool Suppress { get; set; }

  public static float Read()
  {
    _reading = true;
    try
    {
      return ZInput.GetMouseScrollWheel();
    }
    finally
    {
      _reading = false;
    }
  }

  private static void Postfix(ref float __result)
  {
    if (Suppress && !_reading)
    {
      __result = 0f;
    }
  }
}

/// <summary>
/// Gives <see cref="DepthLock"/> the first look at every terrain op as it wakes, before it sends its
/// edit on; only the local player's pickaxe hits into a locked frame are taken over.
/// </summary>
[HarmonyPatch(typeof(TerrainOp), nameof(TerrainOp.Awake))]
internal static class DepthLockPatch
{
  private static bool Prefix(TerrainOp __instance) => !DepthLock.TakeOver(__instance);
}

/// <summary>
/// No build menu for the level hoe: it has one piece, which the game selects on equip, so the menu
/// only ever offered one choice (owner, 2026-11-03). Skips the game's check for the menu button while
/// the local player holds the level hoe; every other build tool keeps its menu.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.UpdateBuildGuiInput))]
internal static class NoBuildMenuPatch
{
  private static bool Prefix(Player __instance) =>
    __instance != Player.m_localPlayer || !Items.IsLevelHoe(__instance.GetRightItem());
}
