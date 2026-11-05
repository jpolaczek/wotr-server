using Wotr.Contracts;
using Wotr.Domain.Content;
using static Wotr.Domain.LordsCardScenarios;

namespace Wotr.Domain;

// TEST-12: the Lords + Kings integration rules (EXPANSIONS X01, T31; KINGS pp. 17-18), each where it decides: Sméagol's
// tiles are skipped and redrawn in a corruption attempt, the Captain of Despair's Eyes neither pay an attempt nor raise
// its draws, a Keeper's Eye pays one like any eligible Eye, a Council token lets a Dark Chieftain enter without his
// condition, and a Companion without a nation icon cannot remove a Corrupted sovereign. They list Lords first, so the
// client shows them in the Lords tab.
public static class LordsKingsScenarios
{
    public const string Group = "Lords · With Kings";

    private const string MinasTirith = "minas-tirith";
    private const string Boromir = "base.character.boromir";
    private const string Legolas = "base.character.legolas";

    private static MatchConfiguration CouncilAndKings => new() { Lords = true, Kings = true, Council = true };

    public static IEnumerable<TestScenario> All() =>
    [
        Lords(new("lords-kings-smeagol-corruption", Group, "Sméagol's tiles are redrawn in a corruption attempt",
            "Two Eyes wait in the Hunt Box and the pool holds Sméagol's four tiles beside one tile of value 1: attempt to " +
            "corrupt a sovereign; a drawn Sméagol tile is set aside and another drawn, and Sméagol never joins the Fellowship this way.",
            Side.Shadow, SmeagolCorruption), ExpansionModules.Kings),
        Lords(new("lords-kings-captain-corruption", Group, "Captain of Despair Eyes cannot pay a corruption attempt",
            "The Captain of Despair stands on the Shadow table and one of its Character dice joined the Hunt Box beside three " +
            "allocated Eyes: only the allocated Eyes pay a corruption attempt, and the Captain's Eye does not raise the draws.",
            Side.Shadow, CaptainCorruption), ExpansionModules.Kings),
        Lords(new("lords-kings-keeper-eye-corruption", Group, "A Keeper Eye pays a corruption attempt",
            "Elrond is in play and the Free Peoples kept Vilya's Eye, which joined the Hunt Box beside an allocated Eye: the " +
            "Keeper's Eye can pay the attempt and then lies with the Free Peoples' used dice.",
            Side.Shadow, KeeperEyeCorruption), ExpansionModules.Kings),
        Lords(new("lords-kings-council-chieftain", Group, "A Council token pays a Dark Chieftain's entry",
            "Council of Rivendell: Boromir and Legolas started outside the Fellowship, so the Shadow holds both tokens. Gondor " +
            "is not at war, yet a Muster die with a token brings the Black Serpent in: his row lights in the Characters panel, " +
            "then the marked medallion pays.",
            Side.Shadow, CouncilChieftain), ExpansionModules.Kings),
        Lords(new("lords-kings-iconless-cleanse", Group, "A Companion without a nation icon cannot remove a corrupted sovereign",
            "Council of Rivendell: the alternative Boromir, without a nation icon, started in Minas Tirith, where Denethor is " +
            "Corrupted, so he cannot remove him. Move Gandalf the Grey (the Free Peoples' icon) in from Osgiliath with one " +
            "Character die, then remove Denethor with the other.",
            Side.FreePeoples, IconlessCleanse), ExpansionModules.Kings)
    ];

    private static MatchState SmeagolCorruption(string id, IRandomSource random) => CorruptionRoll(LordsBoard.Setup(id, random, LordsBoard.WithKings))
        .Hunt(2, 0)
        .Change(AllSmeagolTiles)
        .KeepHuntTiles(SmeagolAndOneTile())
        .Act(Side.Shadow);

    // The Shadow played the Captain with the Witch-king in play; its second Character die then joined the Hunt Box.
    private static MatchState CaptainCorruption(string id, IRandomSource random)
    {
        var board = CorruptionRoll(LordsBoard.Setup(id, random, LordsBoard.WithKings))
            .Hunt(3, 0)
            .Enter(CharacterRules.WitchKing, "minas-morgul");
        board = LordsBoard.Played(board, Side.Shadow, LordsShadowCards.Captain);
        return board.Change(CaptainEye).Act(Side.Shadow);
    }

    private static void CaptainEye(MatchState state)
    {
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Character);
        CaptainOfDespair.RecordCost(state, die);
        CaptainOfDespair.CompleteCost(state, die);
    }

    private static MatchState KeeperEyeCorruption(string id, IRandomSource random) => CorruptionRoll(LordsBoard.Setup(id, random, LordsBoard.WithKings))
        .Hunt(1, 0)
        .Change(state =>
        {
            LordsBoard.Put(state, LordsCharacters.Elrond, "rivendell");
            var vilya = ExpansionBoard.Show(state, LordsDiceCatalog.Vilya, DieFace.Eye);
            vilya.Location = DieLocation.Hunt;
            vilya.HuntOrigin = LordsDiceCatalog.KeeperEye;
        })
        .Act(Side.Shadow);

    private static MatchState CouncilChieftain(string id, IRandomSource random) =>
        LordsBoard.Setup(id, random, CouncilAndKings, Homelands((Boromir, MinasTirith), (Legolas, "woodland-realm")))
            .Dice(Side.Shadow, DieFace.Muster, DieFace.Army, DieFace.Character, DieFace.Event, DieFace.Army, DieFace.Muster)
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event)
            .Act(Side.Shadow);

    // Denethor was corrupted in Minas Tirith; Gandalf the Grey left the Fellowship to Osgiliath. The Shadow has used its dice.
    private static MatchState IconlessCleanse(string id, IRandomSource random) =>
        LordsBoard.Setup(id, random, CouncilAndKings, Homelands((Boromir, MinasTirith)))
            .Companion(CharacterRules.Grey, "osgiliath")
            .Dice(Side.FreePeoples, DieFace.Character, DieFace.Character, DieFace.Army, DieFace.Event)
            .Spent(Side.Shadow, DieFace.Army, DieFace.Muster, DieFace.Character, DieFace.Event, DieFace.Army, DieFace.Muster)
            .Hunt(1, 0)
            .Change(CorruptDenethor)
            .Act(Side.FreePeoples);

    private static void CorruptDenethor(MatchState state)
    {
        var denethor = ExpansionBoard.Sovereign(state, SovereignCatalog.Denethor);
        state.Units.RemoveAll(u => u.Id == denethor.Id);
        denethor.Status = SovereignStatus.Corrupted;
        denethor.CorruptedRegion = MinasTirith;
    }

    private static LordsSetupChoices Homelands(params (string Figure, string Region)[] homes) => new(CharacterRules.Strider,
        [CharacterRules.Grey, .. homes.Select(h => "lords.character." + h.Figure["base.character.".Length..])],
        [.. homes.Select(h => new UnitPlacementChoice { UnitId = h.Figure, RegionId = h.Region })]);

    // You Know the Way There added Sméagol's other two tiles to the pool earlier.
    private static void AllSmeagolTiles(MatchState state)
    {
        foreach (var tile in state.HuntTiles.Where(t => HuntPool.Definitions[t.Id].Pool == SmeagolTiles.Pool))
            tile.Location = "pool";
    }

    // Sméagol's four tiles and one standard tile of value 1 (not a Kings tile) stay in the pool.
    private static Func<HuntTileDefinition, bool> SmeagolAndOneTile()
    {
        var numbered = 0;
        return tile => tile.Pool == SmeagolTiles.Pool ||
            tile.Pool == "standard" && tile.Value == "1" && !KingsTiles.IsKings(tile.Id) && numbered++ < 1;
    }

    // Four Shadow results, two of them Character, beside the Eyes each scenario puts in the Hunt Box.
    private static ScenarioBoard CorruptionRoll(ScenarioBoard board) => board
        .Dice(Side.Shadow, DieFace.Character, DieFace.Character, DieFace.Army, DieFace.Muster)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);
}
