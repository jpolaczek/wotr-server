using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class CouncilSetup
{
    public static bool BeginHomelands(MatchState state)
    {
        if (!state.Configuration.Council || state.Turn != 1 || state.Lords?.Council is not { HomelandsChosen: false })
            return false;
        // Always open this decision, even with no eligible figures: its existence must not reveal versions.
        state.Open(DecisionKind.CouncilHomelands, Side.FreePeoples, [CommandKind.ChooseCouncilHomelands]);
        return true;
    }

    public static bool ValidHomelands(MatchState state, CommandPayload payload)
    {
        if (payload.Placements.Count != payload.Placements.Select(p => p.UnitId).Distinct(StringComparer.Ordinal).Count())
            return false;
        var eligible = CouncilCompanions.CanStartOutside(state).ToDictionary(c => c.Id);
        return payload.Placements.All(p => eligible.TryGetValue(p.UnitId, out var character) &&
            CouncilCompanions.Homelands(character).Contains(p.RegionId));
    }

    public static void ChooseHomelands(MatchState state, IReadOnlyList<UnitPlacementChoice> placements)
    {
        foreach (var placement in placements)
        {
            var character = CharacterCatalog.Companion(state, placement.UnitId);
            LordsCompanionSetup.Reveal(state, character);
            state.Fellowship.Companions.Remove(character.Id);
            character.Location = placement.RegionId;
            character.Garrison = false;
            PoliticalRules.CompanionArrived(state, character.Location, character.NationIcon);
            state.Events.Add(new() { Revision = state.Revision, Kind = "CouncilHomeland",
                Text = $"{character.Id} starts in {character.Location}." });
        }
        state.Lords!.Council!.HomelandsChosen = true;
        PoliticalReactions.Complete(state);
        if (placements.Count == 1)
            state.Open(DecisionKind.CouncilToken, Side.Shadow, [CommandKind.ChooseCouncilToken]);
        else
        {
            if (placements.Count > 1)
                Grant(state, CouncilTokens.All);
            TurnFlow.OpenFellowshipPhase(state);
        }
    }

    public static void Grant(MatchState state, IEnumerable<string> tokens)
    {
        state.Lords!.Council!.GrantedTokens.AddRange(tokens);
        state.Events.Add(new() { Revision = state.Revision, Kind = "CouncilTokensGranted",
            Text = "Shadow received: " + string.Join(", ", state.Lords.Council.GrantedTokens) + "." });
    }
}
