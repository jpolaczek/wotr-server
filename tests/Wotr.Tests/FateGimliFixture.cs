using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class FateGimliFixture
{
    public static MatchState Setup(bool fate = true, bool lords = true, bool council = true, bool kings = false)
    {
        var config = new MatchConfiguration { Fate = fate, Lords = lords, Council = council, Kings = kings };
        var samples = CardCatalog.ConfiguredDeck(config).Count() - 4;
        var state = MatchFactory.Create("fate-gimli", new SequenceRandom(new int[samples]), config);
        TurnFlow.Start(state);
        return state;
    }

    public static MatchState Versions(string? gimli = null)
    {
        var ids = new List<string> { CharacterRules.Grey };
        if (gimli is not null)
            ids.Add(gimli);
        return KingsFixture.Do(Setup(), CommandKind.ChooseCompanionVersions, new() { Ids = ids, GuideId = CharacterRules.Grey });
    }

    public static MatchState Actions(string definition, string region = "iron-hills", bool kings = false)
    {
        var state = FateFixture.Setup(lords: definition == FateGimli.Alternate, kings: kings);
        if (kings)
            KingsSetup.Initialize(state);
        if (definition == FateGimli.Alternate)
        {
            state.Configuration.Lords = true;
            state.Configuration.Council = true;
            LordsDiceSetup.Initialize(state);
            state.Lords!.Council = new() { HomelandsChosen = true };
        }
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
        }
        LordsCharacterFixture.Put(state, definition, region, FateGimli.Figure);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    public static CommandPayload Ability(MatchState state, DieFace face = DieFace.Event)
    {
        var gimli = state.Characters.Single(c => c.Id == FateGimli.Figure);
        var payload = CharacterFixture.Cost(state, face);
        payload.CharacterId = gimli.Id;
        payload.AbilityId = gimli.DefinitionId == FateGimli.Standard ? FateGimli.Renown : "messenger-dwarves";
        payload.Ids = [gimli.Location];
        return payload;
    }

    public static MatchState Hunt(string tile, params string[] companions)
    {
        var names = companions.Length == 0 ? new[] { "gimli" } : companions;
        var state = Actions(FateGimli.Alternate, "fellowship");
        state.Fellowship.RegionId = "hollin";
        foreach (var name in names.Where(name => name != "gimli"))
            CouncilFixture.Inside(state, name);
        state.Fellowship.Companions = names.Select(name => "base.character." + name).ToList();
        state.Fellowship.Guide = state.Fellowship.Companions[0];
        LordsCompanionSetup.Reveal(state, CharacterCatalog.Companion(state, state.Fellowship.Guide));
        state.Dice.First(d => d.Owner == Side.Shadow).Location = DieLocation.Hunt;
        HuntTests.Pool(state, tile);
        return state;
    }
}
