using Wotr.Contracts;

namespace Wotr.Domain;

public enum EffectDuration { UntilRemoved, EndTurn, EndBattle, EndCombatRound }
public enum EffectModifier { None, PreventPassiveMusterAdvance, PreventFellowshipHuntDie, PreventShadowArmyEntry, RestrictRohanActivation, PreventMinasTirithCombatCards }
public enum EffectExpiry { None, FreeNationAdvancedByAttackOrCompanion, CharacterLeavesPlay, FellowshipCompanionsAbsent, AragornArmyOutsideFreeNation, FellowshipDeclaresSanctuary, MinasTirithSiegeEnds, SmeagolNoLongerGuide, CaptainConditionsLost }
public enum PoliticalAdvanceCause { ActionDie, Event, Attack, CompanionAbility, SettlementCapture }

public sealed class ActiveEffectState
{
    public string Id { get; set; } = "";
    public string SourceId { get; set; } = "";
    public Side Owner { get; set; }
    public Side? OnlyFor { get; set; }
    public string TargetId { get; set; } = "";
    public EffectModifier Modifier { get; set; }
    public EffectDuration Duration { get; set; }
    public EffectExpiry ExpiresWhen { get; set; }
    public int Turn { get; set; }
    public string ScopeId { get; set; } = "";
    public List<string> RequiredCompanions { get; set; } = [];
}

public static class ActiveEffects
{
    public static bool PreventsFellowshipHuntDie(MatchState state) =>
        state.ActiveEffects.Any(e => e.Modifier == EffectModifier.PreventFellowshipHuntDie);

    public static void FellowshipLocated(MatchState state) =>
        RemoveWhere(state, e => e.ExpiresWhen == EffectExpiry.AragornArmyOutsideFreeNation);

    public static void EndAction(MatchState state) => RemoveWhere(state, e =>
        e.ExpiresWhen == EffectExpiry.AragornArmyOutsideFreeNation && !new LastBattleCondition().CanPlay(state, e.Owner));

    public static bool BlocksPassiveMuster(MatchState state, NationState nation) => !nation.Active &&
        BoardRules.Nations[nation.Id].Side == Side.FreePeoples &&
        state.ActiveEffects.Any(e => e.Modifier == EffectModifier.PreventPassiveMusterAdvance);

    public static void PoliticalAdvance(MatchState state, string nation, PoliticalAdvanceCause cause)
    {
        if (BoardRules.Nations[nation].Side == Side.FreePeoples &&
            cause is PoliticalAdvanceCause.Attack or PoliticalAdvanceCause.CompanionAbility)
            RemoveWhere(state, e => e.ExpiresWhen == EffectExpiry.FreeNationAdvancedByAttackOrCompanion);
    }

    public static void ExpireBoundary(MatchState state, EffectDuration duration, string scopeId = "") =>
        RemoveWhere(state, e => e.Duration == duration && duration != EffectDuration.UntilRemoved &&
            (duration == EffectDuration.EndTurn ? e.Turn <= state.Turn : e.ScopeId == scopeId));

    public static void CheckConditions(MatchState state) => RemoveWhere(state,
        e => e.ExpiresWhen switch
        {
            EffectExpiry.CharacterLeavesPlay => !state.Characters.Any(c => c.Id == e.TargetId && CharacterRules.OnBoard(c)) ||
                e.Modifier == EffectModifier.RestrictRohanActivation && state.Nations.Single(n => n.Id == "rohan").Active,
            EffectExpiry.FellowshipCompanionsAbsent => !state.Fellowship.Companions.Any(e.RequiredCompanions.Contains),
            EffectExpiry.MinasTirithSiegeEnds => !DenethorRules.Siege(state),
            EffectExpiry.SmeagolNoLongerGuide => !new SmeagolGuideCondition().CanPlay(state, e.Owner),
            EffectExpiry.CaptainConditionsLost => !new CaptainOfDespairCondition().CanPlay(state, e.Owner),
            _ => false
        });

    public static void RemoveWhere(MatchState state, Func<ActiveEffectState, bool> predicate)
    {
        foreach (var effect in state.ActiveEffects.Where(predicate).ToArray())
        {
            state.ActiveEffects.Remove(effect);
            var player = state.Player(effect.Owner);
            if (!state.ActiveEffects.Any(e => e.SourceId == effect.SourceId) && player.Table.Remove(effect.SourceId))
                player.Discard.Add(effect.SourceId);
            state.Events.Add(new()
            {
                Revision = state.Revision,
                Kind = "EffectExpired",
                OnlyFor = effect.OnlyFor,
                Activity = HistoryFacts.Card(state, effect.Owner, ActionCardUse.TableRemoved, effect.SourceId),
                Text = $"{effect.SourceId}: effect ended."
            });
        }
    }
}
