using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class StrongholdCommands : ICommandHandler, IActionOptionsProvider
{
    public bool Handles(CommandKind kind) => kind is CommandKind.FightFieldBattle or CommandKind.RetreatIntoSiege or CommandKind.ExtendSiege or CommandKind.UseEvilKnowledge;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        var battle = BattleRuntime.Current(state);
        var legal = battle is not null && command.Kind switch
        {
            CommandKind.FightFieldBattle => state.Pending?.Kind == DecisionKind.StrongholdDefense && StrongholdRules.CanShelter(state, battle),
            CommandKind.RetreatIntoSiege => state.Pending?.Kind is DecisionKind.StrongholdDefense or DecisionKind.BattleRetreat && StrongholdRules.CanShelter(state, battle),
            CommandKind.ExtendSiege => state.Pending?.Kind == DecisionKind.SiegeContinuation && battle.Mode == BattleMode.Siege &&
                actor == battle.Attacker.Side && DraftSelection.IsSubset(command.Payload.Ids, StrongholdRules.ExtensionCosts(state, battle).ToArray(), 1, 1),
            CommandKind.UseEvilKnowledge => state.Pending?.Kind == DecisionKind.SiegeContinuation && actor == battle.Attacker.Side &&
                command.Payload.Ids.Count == 0 && command.Payload.DieId == "" && EvilKnowledge.Available(state, battle),
            _ => false
        };
        return legal ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        var battle = BattleRuntime.Current(state)!;
        switch (command.Kind)
        {
            case CommandKind.FightFieldBattle:
                battle.Defender.Round.FieldDefenseChosen = true;
                battle.Stage = BattleStage.AttackerCard;
                state.Pending = null;
                break;
            case CommandKind.RetreatIntoSiege:
                StrongholdRules.Shelter(state, battle);
                break;
            case CommandKind.ExtendSiege:
                var selected = command.Payload.Ids.Single();
                if (KingsWeaknesses.SiegeRegulars(state, battle).Contains(selected))
                    KingsWeaknesses.ExtendSiege(state, battle, selected);
                else
                    StrongholdRules.Extend(state, battle, selected);
                break;
            case CommandKind.UseEvilKnowledge:
                EvilKnowledge.Use(state, battle);
                break;
        }
    }

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending?.Owner != actor || BattleRuntime.Current(state) is not { } battle)
            yield break;
        if (state.Pending.Kind == DecisionKind.StrongholdDefense)
            yield return new() { Kind = CommandKind.FightFieldBattle };
        if (state.Pending.Kind is DecisionKind.StrongholdDefense or DecisionKind.BattleRetreat && StrongholdRules.CanShelter(state, battle))
            yield return new() { Kind = CommandKind.RetreatIntoSiege, Targets = [battle.Region] };
        if (state.Pending.Kind == DecisionKind.SiegeContinuation)
        {
            var costs = StrongholdRules.ExtensionCosts(state, battle).ToList();
            yield return new() { Kind = CommandKind.ExtendSiege, EligibleIds = costs, Minimum = 1, Maximum = 1, Enabled = costs.Count > 0 };
            if (EvilKnowledge.Available(state, battle))
                yield return new() { Kind = CommandKind.UseEvilKnowledge, Targets = [battle.Region] };
            yield return new() { Kind = CommandKind.StopBattle };
        }
    }
}
