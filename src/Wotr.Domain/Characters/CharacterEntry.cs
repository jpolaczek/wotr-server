using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record CharacterEntryPlan(string FigureId, string DefinitionId, string Region, bool Garrison, DieFace Cost,
    UnitPlacementChoice? Recruitment = null);

public static class CharacterEntry
{
    public static IEnumerable<CharacterEntryPlan> Candidates(MatchState state, Side actor)
    {
        foreach (var entry in KingsChieftains.Candidates(state, actor))
            yield return entry;
        foreach (var entry in LordsCharacterEntry.Candidates(state, actor))
            yield return entry;
        if (actor == Side.FreePeoples)
        {
            foreach (var entry in FreePeoples(state))
                yield return entry;
            yield break;
        }
        foreach (var id in new[] { CharacterRules.Saruman, CharacterRules.WitchKing, CharacterRules.Mouth })
        {
            if (state.Characters.Any(c => CharacterRules.SameIdentity(c, id)))
                continue;
            foreach (var region in ShadowRegions(state, id))
                yield return new(id, id, region, BoardRules.Besieged(state, region, Side.Shadow), RequiredCost(id));
        }
    }

    private static IEnumerable<CharacterEntryPlan> FreePeoples(MatchState state)
    {
        var grey = state.Characters.Find(c => c.DefinitionId is CharacterRules.Grey or LordsCharacters.Gandalf);
        var minionEntered = state.MinionEverEntered || state.Characters.Any(c =>
            CharacterCatalog.All.TryGetValue(c.DefinitionId, out var definition) &&
            LordsCharacters.QualifiesForWhite(definition) && c.Location != "reserve");
        if (grey is not null && !state.Fellowship.Companions.Contains(grey.Id) && minionEntered &&
            !state.Characters.Any(c => c.DefinitionId == CharacterRules.White))
        {
            var regions = CharacterRules.OnBoard(grey) ? new[] { grey.Location } : grey.Location == "casualties"
                ? BoardRules.CurrentRegions(state).Where(r => r.Id == "fangorn" || r.Nation == "elves" &&
                    r.Settlement == "Stronghold" && Unconquered(state, r.Id, Side.FreePeoples)).Select(r => r.Id) : [];
            foreach (var region in regions)
                yield return new(grey.Id, CharacterRules.White, region, CharacterRules.OnBoard(grey)
                    ? grey.Garrison : BoardRules.Besieged(state, region, Side.FreePeoples), RequiredCost(CharacterRules.White));
        }
        var strider = state.Characters.Find(c => c.DefinitionId is CharacterRules.Strider or CouncilCompanions.Strider);
        if (strider is not null && !state.Fellowship.Companions.Contains(strider.Id) &&
            strider.Location is "minas-tirith" or "dol-amroth" or "pelargir" &&
            Unconquered(state, strider.Location, Side.FreePeoples) && !state.Characters.Any(c => c.DefinitionId == CharacterRules.Aragorn))
            yield return new(strider.Id, CharacterRules.Aragorn, strider.Location, strider.Garrison, RequiredCost(CharacterRules.Aragorn));
    }

    private static IEnumerable<string> ShadowRegions(MatchState state, string id)
    {
        if (id == CharacterRules.Saruman)
            return AtWar(state, "isengard") && Unconquered(state, "orthanc", Side.Shadow) ? ["orthanc"] : [];
        var freeNations = state.Nations.Where(n => BoardRules.Nations[n.Id].Side == Side.FreePeoples);
        if (id == CharacterRules.WitchKing)
            return BlackCaptainAllowed(state)
                ? state.Units.Where(u => u.Nation == "sauron" && BoardRules.IsArmyUnit(u) && BoardRules.Regions.ContainsKey(u.Location))
                    .Select(u => u.Location).Distinct() : [];
        return state.Fellowship.MordorStep.HasValue || freeNations.All(n => n.StepsToWar == 0)
            ? SauronStrongholds(state) : [];
    }

    public static bool BlackCaptainAllowed(MatchState state) => AtWar(state, "sauron") &&
        state.Nations.Any(n => BoardRules.Nations[n.Id].Side == Side.FreePeoples && n.StepsToWar == 0);

    public static IEnumerable<string> SauronStrongholds(MatchState state) => BoardRules.CurrentRegions(state)
        .Where(r => r.Nation == "sauron" && r.Settlement == "Stronghold" && Unconquered(state, r.Id, Side.Shadow)).Select(r => r.Id);

    private static bool AtWar(MatchState state, string nation) => state.Nations.Any(n => n.Id == nation && n.StepsToWar == 0);
    public static bool Unconquered(MatchState state, string region, Side side) => state.Control.GetValueOrDefault(region) == side;

    public static CharacterEntryPlan? Plan(MatchState state, Side actor, CommandPayload payload)
    {
        var candidates = string.IsNullOrEmpty(payload.CouncilTokenId) ? Candidates(state, actor)
            : KingsCouncilEntry.Candidates(state, actor);
        var plan = candidates.FirstOrDefault(p => p.DefinitionId == payload.CharacterId && p.Region == payload.RegionId);
        if (plan is null || DiceRules.AvailableDie(state, actor, payload.DieId) is not { } die || !CanPay(die, plan))
            return null;
        if (KingsChieftains.IsChieftain(plan.DefinitionId) && die.Source == SovereignCatalog.RulerSource)
            plan = plan with { Cost = die.Face };
        return BalrogEntryRecruit.WithRecruitment(state, plan, payload.Placements);
    }

    public static DieFace RequiredCost(string definitionId) =>
        definitionId is CharacterRules.White or CharacterRules.Aragorn ? DieFace.WillOfTheWest : DieFace.Muster;

    public static bool CanPay(DieState die, string definitionId) => CanPay(die, definitionId, RequiredCost(definitionId));

    public static bool CanPay(DieState die, CharacterEntryPlan plan) => CanPay(die, plan.DefinitionId, plan.Cost);

    private static bool CanPay(DieState die, string definitionId, DieFace cost) => DiceRules.CanPay(die, cost) ||
        KingsChieftains.IsChieftain(definitionId) && die.Source == SovereignCatalog.RulerSource &&
        die.Location == DieLocation.Available && die.Face != DieFace.Eye;

    public static void Apply(MatchState state, CharacterEntryPlan plan)
    {
        var definition = CharacterCatalog.All[plan.DefinitionId];
        var character = state.Characters.Find(c => c.Id == plan.FigureId);
        if (state.Lords is not null && character is not null && CharacterRules.IsWitchKing(character) && character.DefinitionId != plan.DefinitionId)
            state.Lords.WitchKingReplaced = true;
        if (character is null)
        {
            character = new() { Id = plan.FigureId };
            state.Characters.Add(character);
        }
        CharacterLifecycle.ApplyVersion(character, definition);
        LordsCompanionSetup.Reveal(state, character);
        character.Location = plan.Region;
        character.Garrison = plan.Garrison;
        state.MinionEverEntered |= LordsCharacters.QualifiesForWhite(definition);
        LordsCharacterEntry.Entered(state, character);
        if (plan.Recruitment is not null)
            RecruitmentRules.Place(state, character.Side, [plan.Recruitment]);
        if (character.Side == Side.FreePeoples)
            PoliticalRules.CompanionArrived(state, plan.Region, character.NationIcon);
        if (definition.Id == CharacterRules.WitchKing)
            foreach (var nation in state.Nations.Where(n => BoardRules.Nations[n.Id].Side == Side.FreePeoples))
                PoliticalRules.Activate(state, nation.Id);
        CharacterLifecycle.ReconcileDice(state);
        state.Events.Add(new() { Revision = state.Revision, Kind = "CharacterEntered", Text = $"{definition.Title} entered play in {plan.Region}." });
    }
}
