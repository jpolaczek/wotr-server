using Wotr.Contracts;
using Wotr.Domain;

internal static class BaseCorpusResume
{
    public static GameCommand For(MatchState state)
    {
        var options = BaseCorpusSeeds.Projection.For(state, state.Pending!.Owner).Options;
        var kind = Kind(state.Pending.Kind);
        var option = options.SingleOrDefault(o => o.Kind == kind)
            ?? throw new InvalidOperationException($"Corpus {state.Id}/{state.Pending.Kind} lacks {kind}; options: {string.Join(", ", options.Select(o => o.Kind))}");
        var payload = new CommandPayload();
        switch (kind)
        {
            case CommandKind.DiscardDie:
                payload.DieId = option.EligibleDice[0];
                break;
            case CommandKind.DiscardCards:
            case CommandKind.AllocateHunt:
            case CommandKind.RemoveExcess:
                payload.Ids = option.EligibleIds.Take(option.Minimum).ToList();
                break;
            case CommandKind.ChooseEffect:
                payload.Ids = [option.EffectChoices[0].Id];
                break;
            case CommandKind.ResolveHuntDamage:
                payload.HuntResponse = HuntResponse.Ring;
                break;
            case CommandKind.SelectGuide:
                payload.GuideId = option.EligibleIds[0];
                break;
            case CommandKind.RevealFellowship:
                payload.Route = ["moria", "dimrill-dale"];
                break;
            case CommandKind.SeparateCompanions:
                payload.Ids = [option.EligibleIds[0]];
                payload.Route = [option.Travel[0].Origin];
                break;
            case CommandKind.TakeBattleLosses:
                payload.Losses = [new() { UnitId = option.EligibleIds[0], Kind = BattleLossKind.Downgrade }];
                break;
            case CommandKind.MoveArmies:
                var army = option.Armies[0];
                var target = army.Targets[0];
                payload.Route = [.. target.Route];
                payload.Moves = [new()
                {
                    From = army.RegionId, To = target.RegionId,
                    UnitIds = [.. army.UnitIds], CharacterIds = [.. army.CharacterIds]
                }];
                break;
        }
        return BaseCorpusSeeds.Command(state, kind, payload);
    }

    private static CommandKind Kind(DecisionKind decision) => decision switch
    {
        DecisionKind.FellowshipPhase => CommandKind.ContinueFellowship,
        DecisionKind.AllocateHunt => CommandKind.AllocateHunt,
        DecisionKind.Action => CommandKind.DiscardDie,
        DecisionKind.ActionSequence => CommandKind.FinishActionSequence,
        DecisionKind.DiscardCards => CommandKind.DiscardCards,
        DecisionKind.Reaction => CommandKind.DeclineReaction,
        DecisionKind.CombatCard => CommandKind.CommitCombatCard,
        DecisionKind.Losses => CommandKind.TakeBattleLosses,
        DecisionKind.HuntDamage => CommandKind.ResolveHuntDamage,
        DecisionKind.RemoveExcess => CommandKind.RemoveExcess,
        DecisionKind.EffectChoice => CommandKind.ChooseEffect,
        DecisionKind.FellowshipRoute => CommandKind.RevealFellowship,
        DecisionKind.Guide => CommandKind.SelectGuide,
        DecisionKind.CompanionSeparation => CommandKind.SeparateCompanions,
        DecisionKind.HuntReroll => CommandKind.RerollHunt,
        DecisionKind.BattleReroll => CommandKind.RerollBattle,
        DecisionKind.BattleContinue => CommandKind.StopBattle,
        DecisionKind.BattleRetreat => CommandKind.HoldBattle,
        DecisionKind.BattleAdvance => CommandKind.AdvanceBattle,
        DecisionKind.StrongholdDefense => CommandKind.FightFieldBattle,
        DecisionKind.SiegeContinuation => CommandKind.StopBattle,
        DecisionKind.ArmyEvent => CommandKind.MoveArmies,
        DecisionKind.CharacterEvent => CommandKind.MoveShadowCharacters,
        _ => throw new InvalidOperationException("No base corpus response for " + decision)
    };
}
