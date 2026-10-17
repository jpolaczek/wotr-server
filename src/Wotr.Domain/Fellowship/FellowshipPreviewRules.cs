using Wotr.Contracts;

namespace Wotr.Domain;

public static class FellowshipPreviewRules
{
    public static void Describe(MatchState state, GameCommand command, ActionPreview preview)
    {
        var kind = command.Kind;
        if (kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship or CommandKind.SelectGuide or CommandKind.EnterMordor ||
            kind is CommandKind.SeparateCompanions or CommandKind.MoveCompanions && state.Pending?.Kind == DecisionKind.CompanionSeparation)
            preview.Costs.Clear();
        if (kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship or CommandKind.HideFellowship)
        {
            var region = kind == CommandKind.HideFellowship ? state.Fellowship.RegionId : command.Payload.Route[^1];
            preview.Fellowship = new()
            {
                RegionId = region,
                Progress = kind == CommandKind.HideFellowship ? state.Fellowship.Progress : 0,
                MordorStep = state.Fellowship.MordorStep,
                Hidden = kind != CommandKind.RevealFellowship,
                Healed = kind == CommandKind.DeclareFellowship && TravelRules.Sanctuary(state, region) ? Math.Min(1, state.Fellowship.Corruption) : 0,
                AdditionalHuntRegions = kind == CommandKind.RevealFellowship ? TravelRules.AdditionalHunts(state, command.Payload.Route) : []
            };
            preview.HasUnknownResult = preview.Fellowship.AdditionalHuntRegions.Count > 0 ||
                kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship && FireAndShadow.Applies(state, command.Payload.Route);
            if (kind == CommandKind.DeclareFellowship && SmeagolRules.Present(state) && TravelRules.Sanctuary(state, region))
                preview.RemovedIds.Add(LordsCharacters.Smeagol);
            if (kind == CommandKind.DeclareFellowship && PoliticalRules.FellowshipActivation(state, region) is { } nation)
                preview.PoliticsChanges.Add(new() { Id = nation.Id, Active = true, StepsToWar = nation.StepsToWar });
        }
        if (kind == CommandKind.EnterMordor)
            preview.Fellowship = new() { RegionId = MordorRules.Track, MordorStep = 0, Hidden = state.Fellowship.Hidden };
        if (kind == CommandKind.SeparateCompanions)
        {
            Place(preview, [CompanionRules.Separate(state, command.Payload.Ids, command.Payload.Route,
                state.Pending?.Kind == DecisionKind.CompanionSeparation, KingsFarSighted.Bonus(command.Payload))!]);
            if (state.Fellowship.Companions.Except(command.Payload.Ids).SequenceEqual([LordsCharacters.Smeagol]))
                preview.RemovedIds.Add(LordsCharacters.Smeagol);
        }
        if (kind == CommandKind.MoveCompanions)
            Place(preview, CompanionRules.Move(state, command.Payload.CompanionMoves, KingsFarSighted.Bonus(command.Payload))!);
    }

    private static void Place(ActionPreview preview, IEnumerable<CompanionMovePlan> plans)
    {
        foreach (var plan in plans)
        {
            preview.RemovedIds.AddRange(plan.Characters.Where(c => c.DefinitionId == LordsCharacters.Smeagol).Select(c => c.Id));
            var companions = plan.Characters.Where(c => c.DefinitionId != LordsCharacters.Smeagol);
            if (plan.RegionId == "casualties")
                preview.RemovedIds.AddRange(companions.Select(c => c.Id));
            else
                preview.Placements.AddRange(companions.Select(c => new UnitPlacementChoice { UnitId = c.Id, RegionId = plan.RegionId }));
        }
    }
}
