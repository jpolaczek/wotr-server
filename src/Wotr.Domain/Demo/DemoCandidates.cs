using Wotr.Contracts;

namespace Wotr.Domain;

// DEMO-02: candidate commands for the CPU's current decision, most preferred first. The engine judges them; a
// candidate that breaks a rule is simply skipped. Every decision ends with a plain fallback over its options.
internal static class DemoCandidates
{
    public static IEnumerable<(CommandKind Kind, CommandPayload Payload)> For(DemoContext context) =>
        Preferred(context).Concat(Fallback(context));

    private static IEnumerable<(CommandKind, CommandPayload)> Preferred(DemoContext context) => context.View.Decision!.Kind switch
    {
        DecisionKind.Action => DemoActionCandidates.For(context),
        DecisionKind.ActionSequence => Plain(CommandKind.FinishActionSequence),
        DecisionKind.FellowshipPhase => DemoFellowshipCandidates.Phase(context),
        DecisionKind.Guide => DemoFellowshipCandidates.Guide(context),
        DecisionKind.FellowshipRoute => DemoFellowshipCandidates.Reveal(context),
        DecisionKind.CompanionSeparation => DemoFellowshipCandidates.Separation(context),
        DecisionKind.HuntDamage => DemoFellowshipCandidates.HuntDamage(),
        DecisionKind.AllocateHunt => DemoHuntCandidates.Allocate(context),
        DecisionKind.HuntReroll => DemoHuntCandidates.Reroll(context),
        DecisionKind.CombatCard or DecisionKind.BattleReroll or DecisionKind.BattleContinue or DecisionKind.BattleRetreat or
            DecisionKind.BattleAdvance or DecisionKind.StrongholdDefense or DecisionKind.SiegeContinuation => DemoBattleCandidates.For(context),
        DecisionKind.Losses => DemoLossCandidates.For(context),
        DecisionKind.DiscardCards => DemoChoiceCandidates.Discard(context),
        DecisionKind.RemoveExcess => DemoChoiceCandidates.Excess(context),
        DecisionKind.EffectChoice => DemoChoiceCandidates.Effect(context),
        DecisionKind.Reaction => Plain(CommandKind.DeclineReaction),
        DecisionKind.ArmyEvent or DecisionKind.CharacterEvent => DemoActionCandidates.EventMoves(context),
        // EXP-03: the expansions' own decisions.
        DecisionKind.CouncilVersions => DemoExpansionCandidates.Versions(context),
        DecisionKind.CouncilHomelands => DemoExpansionCandidates.Homelands(context),
        DecisionKind.CouncilToken => DemoExpansionCandidates.CouncilToken(context),
        DecisionKind.Dismay => DemoExpansionCandidates.Dismay(context),
        DecisionKind.CorruptionTile => DemoExpansionCandidates.CorruptionTile(context),
        _ => []
    };

    // Every enabled option once without a payload and once with each of its dice; then each allowed command alone.
    private static IEnumerable<(CommandKind, CommandPayload)> Fallback(DemoContext context)
    {
        foreach (var option in context.View.Options.Where(o => o.Enabled))
        {
            yield return (option.Kind, new CommandPayload());
            foreach (var die in option.EligibleDice)
                yield return (option.Kind, new CommandPayload { DieId = die });
        }
        foreach (var kind in context.State.Pending!.Commands)
            yield return (kind, new CommandPayload());
    }

    public static IEnumerable<(CommandKind, CommandPayload)> Plain(params CommandKind[] kinds) =>
        kinds.Select(kind => (kind, new CommandPayload()));
}
