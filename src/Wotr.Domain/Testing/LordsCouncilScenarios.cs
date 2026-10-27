using Wotr.Contracts;
using static Wotr.Domain.LordsCardScenarios;

namespace Wotr.Domain;

// TEST-12: the Council of Rivendell (LORDS pp. 30-31, T26) from its private setup on: choosing the alternative
// Companions, starting some outside the Fellowship and the Shadow's token for it, the Travel token, and the six Council
// cards' abilities — the Messengers of Elrond and the Prince of Mirkwood, a Champion in battle, the Guides who separate to
// lessen the Hunt or (the hobbits) cancel a tile, the hobbits' Art of Disappearing and Strider's first move into the
// Fellowship box and his change into Aragorn. The alternative versions are chosen through the real setup.
public static class LordsCouncilScenarios
{
    public const string Group = "Lords · Council of Rivendell";

    private const string Boromir = "base.character.boromir";
    private const string Gimli = "base.character.gimli";
    private const string Legolas = "base.character.legolas";
    private const string Merry = "base.character.merry";
    private const string Pippin = "base.character.pippin";
    private const string AlternateBoromir = "lords.character.boromir";
    private const string AlternateGimli = "lords.character.gimli";
    private const string AlternateLegolas = "lords.character.legolas";
    private const string MinasTirith = "minas-tirith";
    private const string Rivendell = "rivendell";
    private const string Shire = "the-shire";

    public static IEnumerable<TestScenario> All() =>
    [
        Lords(new("lords-council-setup", Group, "Council of Rivendell: choose alternative Companions",
            "A new game with the Council of Rivendell before the first turn: privately choose each Companion's version (Gandalf " +
            "the Grey or the Keeper of Narya, and the six Council cards) and the Guide.",
            Side.FreePeoples, (id, random) => LordsBoard.Start(id, random, LordsBoard.Council))),
        Lords(new("lords-council-homelands", Group, "Council of Rivendell: start outside the Fellowship",
            "The first Fellowship phase after choosing the alternative Boromir, Gimli, Legolas and Merry: declare which of them " +
            "start in their homeland instead of the Fellowship (none, one or more); one gives the Shadow a token of its choice, " +
            "more give it both.",
            Side.FreePeoples, Homelands)),
        Lords(new("lords-council-token-choice", Group, "Council of Rivendell: the Shadow chooses a token",
            "The alternative Boromir started in Minas Tirith, outside the Fellowship: the Shadow chooses one action token, Move " +
            "Nazgûl and Minions or Advance Politics.",
            Side.Shadow, TokenChoice)),
        Lords(new("lords-council-token-travel", Group, "Shadow Council token: move Nazgûl and Minions",
            "Boromir and Legolas started outside the Fellowship, so the Shadow holds both tokens: once this turn, instead of a " +
            "die, move the Nazgûl and Minions as a Character die would.",
            Side.Shadow, TokenTravel)),
        Lords(new("lords-council-messengers", Group, "Messengers of Elrond and the Prince of Mirkwood",
            "The alternative Boromir started in Minas Tirith, Gimli in Erebor, Legolas in the Woodland Realm and Merry in the " +
            "Shire: any die activates and advances Gondor, the Dwarves or the North, or advances the Elves.",
            Side.FreePeoples, Messengers)),
        Lords(new("lords-council-champion", Group, "Champion of Gondor: the alternative Boromir in battle",
            "A Sauron army attacks Minas Tirith from Osgiliath, where the alternative Boromir started with the Gondor army: in " +
            "Gondor's region he adds one to the Combat strength. Choose your Combat card, then see the Combat roll.",
            Side.FreePeoples, Champion)),
        Lords(new("lords-council-guide-separate", Group, "Alternative Boromir, Gimli or Legolas as Guide: lessen the Hunt",
            "Only the alternative Boromir, Gimli and Legolas walk with the Fellowship, Boromir guiding. Move it with the Character " +
            "die into an almost certain Hunt of damage tiles: the Guide may separate to lessen the damage by one, each in turn.",
            Side.FreePeoples, GuideSeparate)),
        Lords(new("lords-council-hobbit-guide", Group, "Alternative Merry or Pippin as Guide: cancel a Hunt tile",
            "Only the alternative Merry and Pippin walk with the Fellowship, Merry guiding. Move it with the Character die into an " +
            "almost certain Hunt of Eye and Reveal tiles: the hobbit Guide may leave to cancel the standard tile.",
            Side.FreePeoples, HobbitGuide)),
        Lords(new("lords-council-art-of-disappearing", Group, "Art of Disappearing: the hobbits escape a lost battle",
            "The alternative Merry and Pippin started in the Shire beside its North Regular; a Sauron army attacked from the Old " +
            "Forest and hit twice. Remove the Regular: the army is gone and each hobbit escapes to a region next to the Shire.",
            Side.FreePeoples, ArtOfDisappearing)),
        Lords(new("lords-council-strider-guide", Group, "Alternative Strider as Guide: the first move die stays out of the Hunt",
            "The alternative Strider guides the Fellowship, which moved last turn (Progress 1). Move it with the Character die: " +
            "the Hunt still happens, but this turn's first move die goes to the Fellowship box, not the Hunt Box.",
            Side.FreePeoples, StriderGuide)),
        Lords(new("lords-council-strider-aragorn", Group, "Alternative Strider becomes Aragorn",
            "Will of the West: the alternative Strider has left the Fellowship and stands in Minas Tirith, so Aragorn takes his figure.",
            Side.FreePeoples, StriderAragorn))
    ];

    private static LordsSetupChoices Versions(params string[] alternatives) =>
        new(CharacterRules.Strider, [CharacterRules.Grey, .. alternatives], []);

    private static LordsSetupChoices Starting(string[] alternatives, params (string Figure, string Region)[] homes) =>
        Versions(alternatives) with { Homelands = [.. homes.Select(h => new UnitPlacementChoice { UnitId = h.Figure, RegionId = h.Region })] };

    private static MatchState Homelands(string id, IRandomSource random) =>
        ChooseVersions(id, random, Versions(AlternateBoromir, AlternateGimli, AlternateLegolas, CouncilCompanions.Merry));

    // The alternative Boromir alone starts in Minas Tirith.
    private static MatchState TokenChoice(string id, IRandomSource random)
    {
        var state = ChooseVersions(id, random, Versions(AlternateBoromir));
        return ExpansionBoard.Execute(state, CommandKind.ChooseCouncilHomelands,
            new() { Placements = [new() { UnitId = Boromir, RegionId = MinasTirith }] });
    }

    private static MatchState ChooseVersions(string id, IRandomSource random, LordsSetupChoices choices) =>
        ExpansionBoard.Execute(LordsBoard.Start(id, random, LordsBoard.Council), CommandKind.ChooseCompanionVersions,
            new() { GuideId = choices.Guide, Ids = [.. choices.Versions] });

    private static MatchState TokenTravel(string id, IRandomSource random) =>
        Council(id, random, Starting([AlternateBoromir, AlternateLegolas], (Boromir, MinasTirith), (Legolas, "woodland-realm")))
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.ArmyMuster, DieFace.Army)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Act(Side.Shadow);

    private static MatchState Messengers(string id, IRandomSource random) =>
        Council(id, random, Starting([AlternateBoromir, AlternateGimli, AlternateLegolas, CouncilCompanions.Merry],
                (Boromir, MinasTirith), (Gimli, "erebor"), (Legolas, "woodland-realm"), (Merry, Shire)))
            .Dice(Side.FreePeoples, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
            .Act(Side.FreePeoples);

    // Three Sauron Regulars took Osgiliath from Gondor and attack Minas Tirith, defended in the field.
    private static MatchState Champion(string id, IRandomSource random)
    {
        var board = Council(id, random, Starting([AlternateBoromir], (Boromir, MinasTirith)))
            .AtWar("sauron", "gondor")
            .Clear("osgiliath")
            .Place("sauron", UnitKind.Regular, "osgiliath", 3)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
        var state = LordsBoard.Field(LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, "osgiliath", MinasTirith));
        return LordsBoard.CombatChoice(state, Side.FreePeoples);
    }

    private static MatchState GuideSeparate(string id, IRandomSource random) =>
        Council(id, random, Versions(AlternateBoromir, AlternateGimli, AlternateLegolas))
            .Companion(CharacterRules.Grey, Rivendell)
            .Companion(CharacterRules.Strider, Rivendell)
            .Companion(Merry, Rivendell)
            .Companion(Pippin, Rivendell)
            .Change(state => Guide(state, Boromir))
            .HuntAhead(Side.FreePeoples, DieFace.Event, tile => tile.Pool == "standard" && tile.Value is "1" or "2" or "3")
            .Act(Side.FreePeoples);

    private static MatchState HobbitGuide(string id, IRandomSource random) =>
        Council(id, random, Versions(CouncilCompanions.Merry, CouncilCompanions.Pippin))
            .Companion(CharacterRules.Grey, Rivendell)
            .Companion(CharacterRules.Strider, Rivendell)
            .Companion(Boromir, Rivendell)
            .Companion(Gimli, Rivendell)
            .Companion(Legolas, Rivendell)
            .Change(state => Guide(state, Merry))
            .HuntAhead(Side.FreePeoples, DieFace.Event, tile => tile.Pool == "standard" && (tile.Value == "eye" || tile.Reveal))
            .Act(Side.FreePeoples);

    // Two Sauron Regulars attack the Shire from the Old Forest: both hit and the North Regular misses; nobody re-rolls.
    private static MatchState ArtOfDisappearing(string id, IRandomSource random)
    {
        var board = Council(id, random, Starting([CouncilCompanions.Merry, CouncilCompanions.Pippin], (Merry, Shire), (Pippin, Shire)))
            .AtWar("sauron", "north")
            .Place("sauron", UnitKind.Regular, "old-forest", 2)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster, DieFace.Army)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
        var state = LordsBoard.Attack(board.Act(Side.Shadow), Side.Shadow, "old-forest", Shire);
        state = ExpansionBoard.Execute(state, CommandKind.CommitCombatCard, new() { CardId = "" });
        state = ExpansionBoard.Execute(state, CommandKind.CommitCombatCard, new() { CardId = "" }, LordsBoard.Rolls(5, 5, 0));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = ExpansionBoard.Execute(state, CommandKind.RerollBattle);
        if (state.Pending is not { Kind: DecisionKind.Losses, Owner: Side.FreePeoples })
            throw new InvalidOperationException($"The Free Peoples must take the battle's losses, not {state.Pending?.Kind}.");
        return state;
    }

    private static MatchState StriderGuide(string id, IRandomSource random) =>
        Council(id, random, Versions(CouncilCompanions.Strider))
            .FellowshipAt(Rivendell, 1)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
            .Hunt(1, 0)
            .Act(Side.FreePeoples);

    private static MatchState StriderAragorn(string id, IRandomSource random) =>
        Council(id, random, Versions(CouncilCompanions.Strider) with { Guide = CharacterRules.Grey })
            .Companion(CharacterRules.Strider, MinasTirith)
            .Dice(Side.FreePeoples, DieFace.WillOfTheWest, DieFace.Character, DieFace.Army, DieFace.Muster)
            .Dice(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.ArmyMuster)
            .Act(Side.FreePeoples);

    private static ScenarioBoard Council(string id, IRandomSource random, LordsSetupChoices choices) =>
        LordsBoard.Setup(id, random, LordsBoard.Council, choices);

    // A Companion of the remaining Fellowship took the Guide's role, which reveals his version.
    private static void Guide(MatchState state, string figure)
    {
        state.Fellowship.Guide = figure;
        LordsCompanionSetup.Reveal(state, Content.CharacterCatalog.Companion(state, figure));
    }
}
