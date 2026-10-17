using Wotr.Contracts;

namespace Wotr.Domain;

public static class CouncilPreview
{
    public static void Describe(MatchState state, GameCommand command, ActionPreview preview)
    {
        var p = command.Payload;
        if (command.Kind == CommandKind.ChooseCouncilHomelands)
        {
            preview.Placements.AddRange(p.Placements);
            preview.Summary = p.Placements.Count switch
            {
                0 => "Keep all Companions in the Fellowship.",
                1 => "Start one Companion in their homeland. Shadow chooses one Action Token.",
                _ => "Start selected Companions in their homelands. Shadow receives both Action Tokens."
            };
            var planned = StateCodec.Copy(state);
            CouncilSetup.ChooseHomelands(planned, p.Placements);
            AddPolitics(state, planned, preview);
        }
        if (command.Kind != CommandKind.UseCouncilToken)
            return;
        preview.Costs.Add(p.AbilityId);
        if (p.AbilityId == CouncilTokens.Politics)
        {
            var nation = state.Nations.Single(n => n.Id == p.NationId);
            preview.PoliticsChanges.Add(new() { Id = nation.Id, Active = nation.Active, StepsToWar = nation.StepsToWar - 1 });
        }
        else
            foreach (var plan in ShadowTravel.Plan(state, p.CompanionMoves)!)
                preview.Placements.AddRange(plan.Ids.Select(id => new UnitPlacementChoice { UnitId = id, RegionId = plan.Region }));
    }

    private static void AddPolitics(MatchState state, MatchState planned, ActionPreview preview)
    {
        var previous = state.Nations.ToDictionary(n => n.Id);
        preview.PoliticsChanges.AddRange(planned.Nations
            .Where(n => n.Active != previous[n.Id].Active || n.StepsToWar != previous[n.Id].StepsToWar)
            .Select(n => new NationView { Id = n.Id, Active = n.Active, StepsToWar = n.StepsToWar }));
    }
}
