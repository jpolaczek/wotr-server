using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class KingsBoundaryProjectionTests
{
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);
    internal static readonly string[] Windows = ["fellowship", "allocation", "action", "ruler-shadow", "ruler-fp", "dismay"];
    public static IEnumerable<object[]> Cases() => Enumerable.Range(0, 6).SelectMany(c =>
        Windows.Where(w => w != "dismay" || c % 2 == 1).SelectMany(w =>
            new[] { false, true }.Select(v => new object[] { w, c, v })));

    [Theory]
    [MemberData(nameof(Cases))]
    public void BothSeatsReceiveOnlyTheirKnownZonesAndPublicResourceFacts(string window, int configuration, bool visible)
    {
        var state = At(window, configuration, visible);
        var before = StateCodec.Write(state);
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Views.For(state, side);
            var facts = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            Assert.Equal(54, facts.Version);
            Assert.NotNull(facts.Kings);
            Assert.Equal(state.HuntTiles.Count, facts.HuntTiles.Sum(z => z.HiddenCount + z.Tiles.Sum(t => t.Count)));
            Assert.Equal(facts.HuntTiles.Sum(z => z.HiddenCount), facts.Kings.UnassignedHuntTiles.Sum(t => t.Count));
            Assert.Equal(state.Kings!.RulerDiscardOrder, facts.Kings.RulerDiscardOrder);
            Assert.Equal(state.Kings.PalantirUsedTurn == state.Turn, facts.Kings.PalantirUsedThisTurn);
            Assert.Equal(state.Kings.DismayDeclinedTurn == state.Turn, facts.Kings.DismayDeclinedThisTurn);
            var covered = side == Side.FreePeoples && !visible;
            Assert.Equal(covered, facts.Kings.UnassignedHuntTiles.Count > 0);
            var denethor = view.Sovereigns.Single(s => s.Id == SovereignCatalog.Denethor);
            Assert.Equal(covered ? null : 1, denethor.Corruption);
            if (covered)
            {
                Assert.Equal(1, facts.HuntTiles.Single(z => z.Location == denethor.Id).HiddenCount);
                Assert.Empty(facts.HuntTiles.Single(z => z.Location == denethor.Id).Tiles);
                Assert.Null(denethor.Tiles.Single().Value);
            }
            facts.Kings.UnassignedHuntTiles.Clear();
            facts.Kings.RulerDiscardOrder.Clear();
        }
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(5)]
    public void PoolAndCoveredSovereignPermutationsLeaveTheEntireFreePeoplesPayloadUnchanged(int configuration)
    {
        var state = At("action", configuration);
        var first = JsonSerializer.Serialize(Views.For(state, Side.FreePeoples));
        var denethor = state.Kings!.Sovereigns.Single(s => s.Id == SovereignCatalog.Denethor);
        var thranduil = state.Kings.Sovereigns.Single(s => s.Id == SovereignCatalog.Thranduil);
        Swap(denethor, thranduil.Tiles[0]);
        var three = state.HuntTiles.First(t => t.Location == "pool" && KingsTiles.IsNumeric(t.Id) &&
            !KingsTiles.IsKings(t.Id) && KingsTiles.CorruptionValue(t.Id) == 3).Id;
        Swap(thranduil, three);
        state.HuntTiles.Reverse();
        foreach (var deck in state.Player(Side.Shadow).Decks.Values)
            deck.Reverse();
        state.RandomSamples.Add(new() { Value = 999 });
        Assert.Equal(first, JsonSerializer.Serialize(Views.For(state, Side.FreePeoples)));

        void Swap(SovereignState owner, string replacement)
        {
            var current = owner.Tiles[0];
            var other = state.Kings.Sovereigns.SingleOrDefault(s => s.Tiles.Contains(replacement));
            var location = state.HuntTiles.Single(t => t.Id == replacement).Location;
            owner.Tiles[0] = replacement;
            if (other is not null)
                other.Tiles[other.Tiles.IndexOf(replacement)] = current;
            state.HuntTiles.Single(t => t.Id == current).Location = location;
            state.HuntTiles.Single(t => t.Id == replacement).Location = owner.Id;
        }
    }

    [Fact]
    public void ActiveCorruptionDrawStillHasNoOrdinaryBoundaryEvenWithVisibleAttachments()
    {
        var state = At("action", visible: true);
        state.Resolution.Frames.Add(new() { Corruption = new() { SovereignId = SovereignCatalog.Denethor } });
        Assert.Null(new Projection().For(state, Side.FreePeoples).ActionBoundary);
    }

    internal static MatchState At(string window, int configuration = 0, bool visible = false)
    {
        var lords = configuration % 2 == 1;
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), new SeededRandom(137), new()
        {
            Kings = true, Lords = lords, Council = lords, Fate = configuration >= 2,
            NewCities = configuration >= 4, VisibleCorruption = visible
        });
        TurnFlow.Start(state);
        if (lords)
        {
            state = Apply(state, CommandKind.ChooseCompanionVersions, new() { Ids = [CharacterRules.Grey], GuideId = CharacterRules.Grey });
            state = Apply(state, CommandKind.ChooseCouncilHomelands);
        }
        Attach(state, SovereignCatalog.Denethor, 1);
        Attach(state, SovereignCatalog.Thranduil, 2);
        if (window == "fellowship")
            return state;
        state = Apply(state, CommandKind.ContinueFellowship);
        if (window == "allocation")
            return state;
        if (window is "ruler-shadow" or "ruler-fp" or "dismay")
        {
            SovereignLifecycle.Awaken(state, SovereignLifecycle.Find(state, SovereignCatalog.Theoden)!, "edoras");
            var chieftain = new CharacterState { Id = KingsChieftains.Ugluk, Location = "orthanc" };
            CharacterLifecycle.ApplyVersion(chieftain, CharacterCatalog.All[KingsChieftains.Ugluk]);
            state.Characters.Add(chieftain);
            KingsDice.Recover(state);
            if (window == "dismay")
                KingsDismay.UsedRulerToken(state, state.Kings!.Tokens.Single());
        }
        state = Apply(state, CommandKind.AllocateHunt);
        if (window == "ruler-shadow")
            return state;
        if (window is "ruler-fp" or "dismay")
            state = Apply(state, CommandKind.DiscardRulerDie,
                new() { DieId = KingsDice.DiscardCandidates(state, Side.Shadow).First() });
        if (window == "ruler-fp")
            return state;
        if (window == "dismay")
            state = Apply(state, CommandKind.DiscardRulerDie,
                new() { DieId = KingsDice.DiscardCandidates(state, Side.FreePeoples).First() });
        return state;
    }

    internal static MatchState Apply(MatchState state, CommandKind kind, CommandPayload? payload = null)
    {
        var result = GameRuntime.Engine(EffectCatalog.All).Execute(state, state.Pending!.Owner,
            BattleFixture.Command(state, kind, payload), new SeededRandom(137));
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    private static void Attach(MatchState state, string sovereign, int value)
    {
        var tile = state.HuntTiles.First(t => t.Location == "pool" && KingsTiles.IsNumeric(t.Id) &&
            !KingsTiles.IsKings(t.Id) && KingsTiles.CorruptionValue(t.Id) == value);
        CorruptionRules.Attach(state, SovereignLifecycle.Find(state, sovereign)!, tile.Id);
    }
}
