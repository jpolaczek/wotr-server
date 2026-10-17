using Wotr.Contracts;

namespace Wotr.Domain;

public enum PoliticalAttack { ArmyBattle, Ents, DeadMenOfDunharrow }

public static class PoliticalRules
{
    public static bool CanAdvance(NationState nation) => nation.StepsToWar > (nation.Active ? 0 : 1);
    public static bool CanMusterAdvance(MatchState state, NationState nation) =>
        CanAdvance(nation) && !ActiveEffects.BlocksPassiveMuster(state, nation);

    public static void Activate(MatchState state, string nationId, PoliticalTrigger? trigger = null, string region = "")
    {
        var nation = state.Nations.Single(n => n.Id == nationId);
        if (!CanActivate(state, nation, trigger, region))
            return;
        nation.Active = true;
        Record(state, nation, "NationActivated");
        KingsPolitics.Record(state, nationId);
        if (nationId == "rohan")
            ActiveEffects.RemoveWhere(state, e => e.Modifier == EffectModifier.RestrictRohanActivation);
    }

    public static void Advance(MatchState state, string nationId, PoliticalAdvanceCause cause = PoliticalAdvanceCause.ActionDie)
    {
        var nation = state.Nations.Single(n => n.Id == nationId);
        if (!CanAdvance(nation))
            return;
        nation.StepsToWar--;
        Record(state, nation, "PoliticsAdvanced");
        KingsPolitics.Record(state, nationId);
        ActiveEffects.PoliticalAdvance(state, nationId, cause);
    }

    public static void EnemyEntered(MatchState state, string regionId, Side invader)
    {
        var nation = BoardRules.Region(state, regionId).Nation;
        if (nation != null && BoardRules.Nations[nation].Side != invader)
            PoliticalReactions.Add(state, PoliticalTrigger.EnemyEntry, nation, regionId);
    }

    // Remember the attacked nations before casualties; resolve once at the end of the whole action.
    public static void ArmyAttacked(MatchState state, IEnumerable<UnitState> defenders, PoliticalAttack attack)
    {
        if (!Enum.IsDefined(attack))
            throw new ArgumentOutOfRangeException(nameof(attack));
        foreach (var group in defenders.Where(BoardRules.IsArmyUnit).GroupBy(u => u.Nation))
        {
            PoliticalReactions.Add(state, PoliticalTrigger.ArmyAttack, group.Key, group.First().Location);
        }
    }

    public static void SettlementCaptured(MatchState state, string regionId, Side previousOwner, Side conqueror, RegionDefinition? previousRegion = null)
    {
        var region = previousRegion ?? BoardRules.Region(state, regionId);
        if (previousOwner != conqueror && BoardRules.IsSettlement(region) && region.Nation != null &&
            BoardRules.Nations[region.Nation].Side == previousOwner)
            PoliticalReactions.Add(state, PoliticalTrigger.SettlementCapture, region.Nation, regionId);
    }

    public static void FellowshipDeclared(MatchState state, string regionId) => ActivateAtSettlement(state, regionId, "free-peoples", true);
    public static void CompanionArrived(MatchState state, string regionId, string nationIcon) => ActivateAtSettlement(state, regionId, nationIcon);

    public static NationState? FellowshipActivation(MatchState state, string regionId)
    {
        var id = SettlementNation(state, regionId, "free-peoples");
        var nation = state.Nations.FirstOrDefault(n => n.Id == id);
        return nation is not null && CanActivate(state, nation, PoliticalTrigger.CompanionArrival, regionId) ? nation : null;
    }

    private static bool CanActivate(MatchState state, NationState nation, PoliticalTrigger? trigger, string region) =>
        !nation.Active && !WormtongueRules.Blocks(state, nation.Id, trigger, region);

    private static void ActivateAtSettlement(MatchState state, string regionId, string icon, bool declaration = false)
    {
        var nation = SettlementNation(state, regionId, icon);
        if (nation != null)
        {
            if (declaration)
                Activate(state, nation, PoliticalTrigger.CompanionArrival, regionId);
            else
                PoliticalReactions.Add(state, PoliticalTrigger.CompanionArrival, nation, regionId);
        }
    }

    private static string? SettlementNation(MatchState state, string regionId, string icon)
    {
        var region = BoardRules.Region(state, regionId);
        return region.Nation != null && region.Settlement is "City" or "Stronghold" &&
            state.Control.GetValueOrDefault(regionId) == Side.FreePeoples && BoardRules.Nations[region.Nation].Side == Side.FreePeoples &&
            (icon == "free-peoples" || icon == region.Nation) ? region.Nation : null;
    }

    private static void Record(MatchState state, NationState nation, string kind) => state.Events.Add(new()
    {
        Revision = state.Revision,
        Kind = kind,
        Activity = HistoryFacts.Political(state, nation),
        Text = $"{nation.Id}: {(nation.Active ? "active" : "passive")}, {nation.StepsToWar} steps to war."
    });
}
