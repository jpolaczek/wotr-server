using Wotr.Contracts;

namespace Wotr.Domain;

internal static class TurnDecisionCommands
{
    public static List<CommandKind> Fellowship() =>
        [CommandKind.ContinueFellowship, CommandKind.DeclareFellowship, CommandKind.SelectGuide, CommandKind.EnterMordor];

    public static List<CommandKind> Actions(MatchConfiguration configuration, Side side)
    {
        List<CommandKind> commands =
        [
            CommandKind.DiscardDie, CommandKind.Pass, CommandKind.DrawCard, CommandKind.UseRing,
            CommandKind.AdvancePolitics, CommandKind.Recruit, CommandKind.MoveArmies, CommandKind.PlayEvent,
            CommandKind.HideFellowship, CommandKind.SeparateCompanions, CommandKind.MoveCompanions,
            CommandKind.MoveFellowship, CommandKind.AttackArmy, CommandKind.EnterCharacter,
            CommandKind.UseCharacterAbility, CommandKind.MoveShadowCharacters, CommandKind.DiscardProtection,
            CommandKind.DiscardPalantir, CommandKind.DiscardDenethor
        ];
        if (configuration.Lords)
            commands.AddRange([CommandKind.UseNarya, CommandKind.DiscardLore, CommandKind.DiscardGates]);
        if (configuration.Council && side == Side.Shadow)
            commands.Add(CommandKind.UseCouncilToken);
        if (configuration.Kings)
            commands.AddRange(side == Side.FreePeoples
                ? [CommandKind.AwakenSovereign, CommandKind.RemoveCorruptedSovereign, CommandKind.DiscardWelcome, CommandKind.DiscardBaradPalantir]
                : [CommandKind.AttemptCorruption]);
        return commands;
    }
}
