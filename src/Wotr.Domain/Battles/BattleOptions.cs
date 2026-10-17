using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class BattleOptions(EffectCatalog? catalog = null) : IActionOptionsProvider
{
    private readonly BattleCards cards = new(catalog ?? EffectCatalog.All);

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending?.Owner != actor)
            yield break;
        if (ArmyEvents.Current(state, actor) is { } order && ShadowArmyEvents.Supports(order))
        {
            yield return ShadowArmyEvents.Option(state, actor, order, true);
            yield break;
        }
        if (ArmyEvents.Current(state, actor) == ArmyEventOrder.ReliefAttack)
        {
            yield return ArmyEventOptions.Option(state, actor, ArmyEventOrder.ReliefAttack);
            yield break;
        }
        if (state.Pending.Kind == DecisionKind.Action)
        {
            yield return AttackOption(state, actor);
            yield break;
        }
        if (BattleRuntime.Active(state) is not { } battle)
            yield break;
        var force = battle.Force(actor);
        switch (state.Pending.Kind)
        {
            case DecisionKind.CombatCard:
                yield return new()
                {
                    Kind = CommandKind.CommitCombatCard,
                    EligibleIds = state.Player(actor).Hand
                    .Concat(BattleTableCards.Candidates(state, battle, actor)).Where(c => cards.CanCommit(state, battle, actor, c)).ToList(),
                    Maximum = 1
                };
                break;
            case DecisionKind.BattleReroll:
                yield return new() { Kind = CommandKind.RerollBattle, EligibleIds = BattleRoll.Failed(force), Maximum = BattleRoll.RerollMaximum(state, force) };
                break;
            case DecisionKind.Losses:
                yield return new()
                {
                    Kind = CommandKind.TakeBattleLosses,
                    EligibleIds = BattleRules.Units(state, force).Where(BoardRules.IsArmyUnit).Select(u => u.Id).ToList(),
                    Minimum = BattleLosses.Current(state)?.Voluntary == true ? 0 : state.Pending.RequiredCount,
                    Maximum = state.Pending.RequiredCount
                };
                break;
            case DecisionKind.BattleContinue:
                yield return new() { Kind = CommandKind.ContinueBattle };
                yield return new() { Kind = CommandKind.StopBattle };
                break;
            case DecisionKind.BattleRetreat:
                if (BattleMovement.Retreats(state, battle).Any())
                    yield return new() { Kind = CommandKind.RetreatBattle, Targets = BattleMovement.Retreats(state, battle).ToList() };
                if (!battle.Defender.Round.OrderlyWithdrawal)
                    yield return new() { Kind = CommandKind.HoldBattle };
                break;
            case DecisionKind.BattleAdvance:
                yield return new()
                {
                    Kind = CommandKind.AdvanceBattle,
                    Targets = [battle.Region],
                    EligibleIds = BattleRules.Units(state, force).Select(u => u.Id).Concat(BattleRules.Characters(state, force).Where(CharacterRules.Mobile).Select(c => c.Id)).ToList()
                };
                break;
        }
    }

    private static ActionOption AttackOption(MatchState state, Side actor)
    {
        var armies = AttackingArmies(state, actor).ToList();
        var costs = CharacterCosts.ArmyOptions(state, actor, 1)
            .Where(c => !c.RequiresCommander || armies.Any(a => a.CommanderIds.Count > 0))
            .Where(c => c.RequiredCharacterId == "" || armies.Any(a => a.CharacterIds.Contains(c.RequiredCharacterId))).ToList();
        return new()
        {
            Kind = CommandKind.AttackArmy,
            Armies = armies,
            MovementCosts = costs,
            Minimum = 1,
            Maximum = 1,
            EligibleDice = costs.Select(c => c.DieId).Distinct().ToList(),
            EligibleIds = armies.SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).ToList(),
            Targets = armies.SelectMany(a => a.Targets.Select(t => t.RegionId)).Distinct().ToList(),
            Enabled = costs.Count > 0 && armies.Count > 0,
            Reason = costs.Count == 0 ? "Requires an Army die or a Character die with a commander." : armies.Count == 0 ? "No army has a legal attack." : ""
        };
    }

    internal static IEnumerable<ArmyOption> AttackingArmies(MatchState state, Side actor)
    {
        foreach (var group in state.Units.Where(u => BoardRules.Owner(u) == actor && BoardRules.Regions.ContainsKey(u.Location) && BattleRules.AtWar(state, u))
            .GroupBy(u => (u.Location, u.Garrison)).Where(g => g.Any(BoardRules.IsArmyUnit)))
        {
            var characters = state.Characters.Where(c => c.Location == group.Key.Location && c.Garrison == group.Key.Garrison && c.Side == actor).Select(c => c.Id).ToList();
            var army = new ArmyOption
            {
                RegionId = group.Key.Location,
                Garrison = group.Key.Garrison,
                UnitIds = group.Select(u => u.Id).ToList(),
                CharacterIds = characters,
                CommanderIds = group.Where(u => CharacterRules.Leads(state, u)).Select(u => u.Id).Concat(characters).ToList()
            };
            var targets = group.Key.Garrison ? [group.Key.Location] : BoardRules.Regions[group.Key.Location].Neighbors.Append(group.Key.Location);
            foreach (var target in targets)
            {
                var move = new ArmyMoveChoice { From = group.Key.Location, To = target, UnitIds = army.UnitIds, CharacterIds = characters };
                var battle = BattleRules.Attack(state, actor, move, DieFace.Army);
                if (battle is null)
                {
                    // Leaving one army unit can make a mandatory rearguard legal. Never enumerate subsets.
                    move.UnitIds = group.Where(u => u.Id != group.First(BoardRules.IsArmyUnit).Id).Select(u => u.Id).ToList();
                    battle = BattleRules.Attack(state, actor, move, DieFace.Army);
                    if (battle is null)
                        continue;
                }
                army.Targets.Add(new() { RegionId = target, BattleMode = battle.Mode, EligibleIds = army.UnitIds.Concat(characters).ToList() });
            }
            if (army.Targets.Count > 0)
                yield return army;
        }
    }
}
