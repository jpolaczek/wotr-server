using Wotr.Contracts;

namespace Wotr.Domain;

public static class CharacterPreview
{
    public static void Describe(MatchState state, Side actor, GameCommand command, ActionPreview preview)
    {
        var p = command.Payload;
        if (command.Kind == CommandKind.EnterCharacter)
            DescribeEntry(state, actor, p, preview);
        if (command.Kind == CommandKind.UseCharacterAbility)
        {
            var plan = CharacterAbilities.Plan(state, actor, p)!;
            LordsCharacterAbilities.DescribePreview(state, plan, preview);
            preview.Placements.AddRange(plan.Placements);
            preview.RemovedIds.AddRange(plan.ReturnedIds);
            if (KingsUpgrades.Handles(plan.AbilityId))
                return;
            DescribeExcess(state, actor, plan, preview);
            if (plan.Nation != "")
            {
                var nation = state.Nations.Single(n => n.Id == plan.Nation);
                preview.PoliticsChanges.Add(new() { Id = nation.Id, Active = nation.Active || CouncilAbilities.Activates(plan.AbilityId),
                    StepsToWar = Math.Max(0, nation.StepsToWar - 1) });
            }
        }
        if (command.Kind == CommandKind.MoveShadowCharacters && p.CompanionMoves.Count > 0)
            foreach (var plan in ShadowTravel.Plan(state, p.CompanionMoves, NazgulEventTravel.Active(state, actor))!)
                preview.Placements.AddRange(plan.Ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = plan.Region }));
    }

    private static void DescribeExcess(MatchState state, Side actor, CharacterAbilityPlan plan, ActionPreview preview)
    {
        var figures = state.Units.ToDictionary(u => u.Id);
        foreach (var group in plan.Placements.GroupBy(p => p.RegionId))
        {
            var garrison = KingsRulerRecruitment.Handles(plan.AbilityId) && BoardRules.Besieged(state, group.Key, actor);
            var count = state.Units.Count(u => u.Location == group.Key && u.Garrison == garrison && BoardRules.IsArmyUnit(u) &&
                BoardRules.Owner(u) == actor && !plan.ReturnedIds.Contains(u.Id)) + group.Count(p => BoardRules.IsArmyUnit(figures[p.UnitId]));
            var limit = garrison ? 5 : 10;
            if (count > limit)
                preview.Excess.Add(new() { RegionId = group.Key, Garrison = garrison, Count = count - limit });
        }
    }

    private static void DescribeEntry(MatchState state, Side actor, CommandPayload payload, ActionPreview preview)
    {
        var plan = CharacterEntry.Plan(state, actor, payload)!;
        preview.Placements.Add(new() { UnitId = plan.FigureId, RegionId = plan.Region });
        if (plan.Recruitment is not null)
        {
            preview.Placements.Add(plan.Recruitment);
            var count = state.Units.Count(u => u.Location == plan.Region && !u.Garrison && BoardRules.IsArmyUnit(u) && BoardRules.Owner(u) == actor);
            if (count >= 10)
                preview.Excess.Add(new() { RegionId = plan.Region, Count = count - 9 });
        }
        var planned = StateCodec.Copy(state);
        CharacterEntry.Apply(planned, plan);
        PoliticalReactions.Complete(planned);
        var previous = state.Nations.ToDictionary(n => n.Id);
        foreach (var nation in planned.Nations.Where(n => n.Active != previous[n.Id].Active || n.StepsToWar != previous[n.Id].StepsToWar))
            preview.PoliticsChanges.Add(new() { Id = nation.Id, Active = nation.Active, StepsToWar = nation.StepsToWar });
    }
}
