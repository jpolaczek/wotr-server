using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.FreePeoplesCharacterCards04;
using static CardPackScenario;

internal static class CardPack04Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["ents-loss"] = Ents(random, true),
        ["ents-follow-up"] = Ents(random, false),
        ["dead-men"] = DeadMenChoice(random),
        ["stewards"] = StewardsChoice(random),
        ["grey-company"] = CompanyChoice(random)
    };

    private static MatchState BeforeEvent(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        state.Fellowship.RegionId = "rivendell";
        state.Fellowship.Progress = 0;
        return state;
    }

    private static MatchState Play(MatchState state, string card, params int[] rolls)
    {
        Give(state, card);
        return Execute(state, CommandKind.PlayEvent, new() { DieId = state.Dice[0].Id, CardId = card, CardCost = DieFace.Character }, rolls);
    }

    private static MatchState Ents(IRandomSource random, bool army)
    {
        var state = BeforeEvent(random);
        Character(state, CharacterRules.White, "fangorn");
        Character(state, CharacterRules.Saruman, "orthanc");
        Give(state, Huorns);
        Give(state, FreePeoplesCharacterCards02.Bilbo);
        state.Fellowship.Corruption = 2;
        if (army)
        {
            Unit(state, "isengard", UnitKind.Regular, "orthanc");
            Unit(state, "isengard", UnitKind.Elite, "orthanc");
        }
        return Play(state, Treebeard, 3, 4, 0);
    }

    private static MatchState DeadMenChoice(IRandomSource random)
    {
        var state = BeforeEvent(random);
        Character(state, CharacterRules.Strider, "edoras");
        Character(state, "base.character.gimli", "edoras");
        Character(state, "base.character.legolas", "edoras");
        for (var i = 0; i < 8; i++)
            Unit(state, "sauron", UnitKind.Regular, "pelargir");
        Character(state, CharacterRules.WitchKing, "pelargir");
        Unit(state, "sauron", UnitKind.Nazgul, "pelargir");
        state.Control["pelargir"] = Side.Shadow;
        return Play(state, DeadMen);
    }

    private static MatchState StewardsChoice(IRandomSource random)
    {
        var state = BeforeEvent(random);
        Character(state, "base.character.boromir", "minas-tirith");
        return Play(state, Stewards);
    }

    private static MatchState CompanyChoice(IRandomSource random)
    {
        var state = BeforeEvent(random);
        Character(state, CharacterRules.Aragorn, "fangorn");
        Unit(state, "gondor", UnitKind.Regular, "fangorn");
        Unit(state, "rohan", UnitKind.Regular, "fangorn");
        return Play(state, GreyCompany);
    }
}
