using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static Wotr.Domain.ShadowCharacterCards03;
using static CardPackScenario;

internal static class Shadow03Scenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["shadow-lure"] = Event(random, Lure),
        ["shadow-breaking"] = Event(random, Breaking),
        ["shadow-lidless"] = Event(random, Lidless),
        ["shadow-worn"] = WornChoice(random),
        ["shadow-crebain"] = CrebainChoice(random),
        ["shadow-balrog"] = BalrogChoice(random),
        ["shadow-words"] = Combat(random, false),
        ["shadow-table-balrog"] = Combat(random, true)
    };

    private static MatchState Event(IRandomSource random, string card)
    {
        var state = EmptyBoard(random);
        state.Fellowship.RegionId = "hollin";
        state.Fellowship.Hidden = false;
        Pool(state, "base.hunt.standard.3.0.1");
        TurnFlow.SelectNext(state, Side.Shadow);
        return PlayEvent(state, card, card == Lure ? [state.Fellowship.Companions.IndexOf("base.character.gimli")] : card == Breaking ? [0] : []);
    }

    private static MatchState WornChoice(IRandomSource random)
    {
        var state = EmptyBoard(random);
        state.Fellowship.RegionId = "hollin";
        Table(state, Worn, Side.Shadow);
        foreach (var id in new[] { FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.Axe, FreePeoplesCharacterCards02.Horn, FreePeoplesCharacterCards02.Staff })
            Table(state, id, Side.FreePeoples);
        Pool(state, "base.hunt.standard.3.0.1");
        TurnFlow.SelectNext(state, Side.Shadow);
        state = PlayEvent(state, ShadowCharacterCards02.FoulThing);
        state = Execute(state, CommandKind.DeclineReaction, new(), 0);
        state = Execute(state, CommandKind.DeclineReaction, new());
        state = Execute(state, CommandKind.DeclineReaction, new(), state.Fellowship.Companions.IndexOf("base.character.gimli"));
        state = Execute(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        return Execute(state, CommandKind.RespondEffect, new() { ReactionId = WornReaction });
    }

    private static MatchState CrebainChoice(IRandomSource random)
    {
        var state = EmptyBoard(random);
        state.Fellowship.RegionId = "hollin";
        Table(state, Crebain, Side.Shadow);
        var die = state.Dice.First(d => d.Owner == Side.Shadow);
        die.Location = DieLocation.Hunt;
        die.HuntOrigin = "allocated";
        state.Dice[0].Face = DieFace.Character;
        Pool(state, "base.hunt.standard.3.0.1");
        return Execute(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
    }

    private static MatchState BalrogChoice(IRandomSource random)
    {
        var state = EmptyBoard(random);
        state.Fellowship.RegionId = "moria";
        state.Fellowship.Progress = 2;
        state.Fellowship.Corruption = 3;
        Table(state, Balrog, Side.Shadow);
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        Pool(state, "base.hunt.standard.1.1.1");
        return Execute(state, CommandKind.DeclareFellowship, new() { Route = ["moria", "dimrill-dale", "lorien"] });
    }

    private static MatchState Combat(IRandomSource random, bool table)
    {
        var state = EmptyBoard(random);
        state.Control["moria"] = Side.FreePeoples;
        for (var i = 0; i < 3; i++)
        {
            Unit(state, "sauron", UnitKind.Regular, "hollin");
            Unit(state, "gondor", UnitKind.Regular, "moria");
        }
        Unit(state, "sauron", UnitKind.Nazgul, "hollin");
        Unit(state, "gondor", UnitKind.Leader, "moria");
        Character(state, CharacterRules.Aragorn, "moria");
        Character(state, "base.character.gimli", "moria");
        if (table)
            Table(state, Balrog, Side.Shadow);
        else
            Give(state, Worn);
        TurnFlow.SelectNext(state, Side.Shadow);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = DieFace.Army;
        state = Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            Moves = [new() { From = "hollin", To = "moria", UnitIds = state.Units.Where(u => u.Location == "hollin").Select(u => u.Id).ToList() }]
        });
        state = Execute(state, CommandKind.FightFieldBattle, new());
        if (table)
            return state;
        state = Execute(state, CommandKind.CommitCombatCard, new() { CardId = Worn });
        return Execute(state, CommandKind.CommitCombatCard, new());
    }

    private static void Table(MatchState state, string card, Side side)
    {
        Give(state, card);
        HandRules.Remove(state.Player(side), card);
        state.Player(side).Table.Add(card);
        state.ActiveEffects.Add(new() { Id = "table-" + card, SourceId = card, Owner = side });
    }

    private static void Pool(MatchState state, string tile)
    {
        foreach (var hunt in state.HuntTiles)
            hunt.Location = hunt.Id == tile ? "pool" : "removed";
    }

    public static Dictionary<string, PlayerView> LayoutViews(IReadOnlyDictionary<string, MatchState> states)
    {
        var names = new HashSet<string> { "shadow-lure", "shadow-breaking", "shadow-lidless", "shadow-worn", "shadow-crebain", "shadow-balrog", "shadow-words", "shadow-table-balrog" };
        return CardPackScenario.LayoutViews(states, names);
    }
}
