using Wotr.Contracts;
using Wotr.Domain;
using static Wotr.Domain.FreePeoplesCharacterCards03;
using static CardPackScenario;

internal static class CardPack03Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["mirror"] = MirrorChoice(random),
        ["gwaihir"] = GwaihirChoice(random),
        ["swifter"] = SwifterChoice(random),
        ["there-and-back"] = ThereAndBackChoice(random),
        ["eagles"] = EaglesChoice(random),
        ["sudden-strike"] = SuddenLosses(random)
    };

    private static MatchState BeforeEvent(IRandomSource random)
    {
        var state = FellowshipScenario.Actions(random);
        state.Fellowship.RegionId = "rivendell";
        state.Fellowship.Progress = 0;
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        return state;
    }

    private static MatchState Play(MatchState state, string card)
    {
        Give(state, card);
        return Execute(state, CommandKind.PlayEvent, new() { DieId = state.Dice[0].Id, CardId = card, CardCost = DieFace.Character });
    }

    private static MatchState MirrorChoice(IRandomSource random)
    {
        var state = BeforeEvent(random);
        state.Fellowship.RegionId = "lorien";
        state.Fellowship.Progress = 2;
        state.Fellowship.Corruption = 3;
        return Play(state, Mirror);
    }

    private static MatchState GwaihirChoice(IRandomSource random)
    {
        var state = BeforeEvent(random);
        Character(state, "base.character.gimli", "dimrill-dale");
        Unit(state, "elves", UnitKind.Regular, "lorien");
        state.Units.Single(u => u.Location == "lorien").Garrison = true;
        Unit(state, "sauron", UnitKind.Regular, "lorien");
        return Play(state, Gwaihir);
    }

    private static MatchState SwifterChoice(IRandomSource random)
    {
        var state = BeforeEvent(random);
        Character(state, CharacterRules.White, "rivendell");
        Character(state, "base.character.merry", "rivendell");
        state.Fellowship.Guide = CharacterRules.Strider;
        return Play(state, Swifter);
    }

    private static MatchState ThereAndBackChoice(IRandomSource random)
    {
        var state = BeforeEvent(random);
        state.Fellowship.RegionId = "old-forest-road";
        return Play(state, ThereAndBack);
    }

    private static MatchState EaglesChoice(IRandomSource random)
    {
        var state = BeforeEvent(random);
        Character(state, "base.character.gimli", "dimrill-dale");
        Unit(state, "gondor", UnitKind.Regular, "dimrill-dale");
        Unit(state, "sauron", UnitKind.Regular, "south-anduin-vale");
        Unit(state, "sauron", UnitKind.Regular, "moria");
        Unit(state, "sauron", UnitKind.Nazgul, "moria");
        for (var i = 0; i < 7; i++)
            Unit(state, "sauron", UnitKind.Nazgul, "south-anduin-vale");
        Character(state, CharacterRules.WitchKing, "south-anduin-vale");
        return Play(state, Eagles);
    }

    private static MatchState SuddenLosses(IRandomSource random)
    {
        var state = CardPackBattleScenario.BeforeCards(random, Challenge);
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = Challenge });
        return Execute(state, CommandKind.CommitCombatCard, new(), 4, 0, 0, 0);
    }
}
