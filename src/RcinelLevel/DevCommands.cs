using System.Collections.Generic;
using Jotunn.Entities;

namespace Rcinel.Level;

/// <summary>
/// Dev command switches for the level hoe (owner, 2026-11-03). They only last for the session: they
/// are never saved and go off when the player leaves the world, not on death (<see cref="Controls"/>).
/// </summary>
internal static class DevModes
{
  /// <summary>The hoe takes no stone (<c>rcinellevel_nostone</c>).</summary>
  public static bool NoStone { get; set; }

  /// <summary>The hoe also lowers ground above the target, as the game's level ground does (<c>rcinellevel_dig</c>).</summary>
  public static bool Dig { get; set; }

  public static void Reset() => NoStone = Dig = false;

  public static void Register()
  {
    Jotunn.Managers.CommandManager.Instance.AddConsoleCommand(new NoStoneCommand());
    Jotunn.Managers.CommandManager.Instance.AddConsoleCommand(new DigCommand());
  }

  /// <summary>"1"/"on" switches on, "0"/"off" off, nothing toggles.</summary>
  internal static bool Next(bool now, string[] args) => args.Length == 0
    ? !now
    : args[0] is "1" or "on" or "true";
}

/// <summary>
/// A cheat command like the game's own: only with <c>devcommands</c> on, which the game allows the
/// server's admins (and everyone in a single-player world).
/// </summary>
internal abstract class DevCommand : ConsoleCommand
{
  public override bool IsCheat => true;

  public override List<string> CommandOptionList() => ["on", "off"];

  public override void Run(string[] args, Terminal context)
  {
    var on = Apply(args);
    context.AddString($"{Name}: {(on ? "on" : "off")} until you leave the world.");
  }

  protected abstract bool Apply(string[] args);
}

internal sealed class NoStoneCommand : DevCommand
{
  public override string Name => "rcinellevel_nostone";

  public override string Help => "[on|off] The level hoe raises ground without taking stone, for this session.";

  protected override bool Apply(string[] args) => DevModes.NoStone = DevModes.Next(DevModes.NoStone, args);
}

internal sealed class DigCommand : DevCommand
{
  public override string Name => "rcinellevel_dig";

  public override string Help => "[on|off] The level hoe also lowers ground above the target, for this session.";

  protected override bool Apply(string[] args) => DevModes.Dig = DevModes.Next(DevModes.Dig, args);
}
