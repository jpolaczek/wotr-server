using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public sealed class LordsBoundaryProjectionTests
{
    internal static readonly string[] Windows = ["setup", "homelands", "token", "fellowship", "allocation", "action", "special", "paid", "vilya"];
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [MemberData(nameof(Cases))]
    public void DescribesOnlyPublicLordsObligationsForBothPlayers(string window, bool fate)
    {
        var state = At(window, fate);
        var original = StateCodec.Write(state);
        foreach (var side in Enum.GetValues<Side>())
        {
            var view = Views.For(state, side);
            var facts = Assert.IsType<ActionBoundaryView>(view.ActionBoundary);
            Assert.Equal(45, facts.Version);
            Assert.NotNull(facts.Lords);
            Assert.Equal(state.Lords!.PaidWillOfWestIds, facts.Lords.PaidWillOfWestIds);
            Assert.Equal(state.Lords.ChoiceOrder, facts.Lords.SpecialDieChoiceOrder);
            Assert.Equal(state.Lords.Council!.HomelandsChosen, facts.Lords.Council!.HomelandsChosen);
            Assert.Equal(state.Lords.ActionCompletion?.FellowshipMove, facts.Lords.CompletedActionWasFellowshipMove);
            var smeagol = facts.HuntTiles.SelectMany(z => z.SpecialTileIds).Where(id => id.StartsWith("lords.hunt.smeagol."));
            Assert.Equal(4, smeagol.Count());
            if (side == Side.Shadow)
                Assert.DoesNotContain(facts.Characters, c => state.Fellowship.Companions.Contains(c.Character.Id) &&
                    !state.Lords.RevealedCompanionVersions.ContainsKey(c.Character.Id));
            facts.Lords.PaidWillOfWestIds.Clear();
            facts.Lords.SpecialDieChoiceOrder.Clear();
            facts.Lords.Council.FellowshipBoxDieId = "changed";
        }
        Assert.Equal(original, StateCodec.Write(state));
    }

    public static IEnumerable<object[]> Cases() => Windows.SelectMany(w => new[] { false, true }.Select(f => new object[] { w, f }));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrivateCompanionVersionsAndOrdersLeaveTheEntireShadowPayloadIdentical(bool fate)
    {
        var state = At("action", fate);
        var first = JsonSerializer.Serialize(Views.For(state, Side.Shadow));
        foreach (var pair in CouncilCompanions.AvailableVersions(state))
        {
            if (state.Lords!.RevealedCompanionVersions.ContainsKey(pair.FigureId))
                continue;
            var character = CharacterCatalog.Companion(state, pair.FigureId);
            CharacterLifecycle.ApplyVersion(character, CharacterCatalog.All[pair.BaseId]);
        }
        state.Player(Side.FreePeoples).PrivateCompanionVersions = [CharacterRules.Grey];
        foreach (var deck in state.Player(Side.FreePeoples).Decks.Values)
            deck.Reverse();
        state.HuntTiles.Reverse();
        state.RandomSamples.Add(new() { Value = 999 });
        Assert.Equal(first, JsonSerializer.Serialize(Views.For(state, Side.Shadow)));
    }

    [Theory]
    [InlineData("frames")]
    [InlineData("return")]
    [InlineData("missing")]
    [InlineData("kings")]
    public void UnreviewedExpansionContinuationsHaveNoBoundary(string change)
    {
        var state = At("action");
        switch (change)
        {
            case "frames": state.Resolution.Suspend(state, "unreviewed", 0); break;
            case "return": state.Lords!.Council!.BattleReturns.Add(new("base.character.merry", "the-shire")); break;
            case "missing": state.Lords = null; break;
            case "kings": state.Configuration.Kings = true; break;
        }
        Assert.Null(new Projection().For(state, Side.Shadow).ActionBoundary);
    }

    internal static MatchState At(string window, bool fate = false)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), new SeededRandom(137),
            new() { Lords = true, Council = true, Fate = fate });
        TurnFlow.Start(state);
        if (window == "setup")
            return state;
        state = Apply(state, CommandKind.ChooseCompanionVersions, new()
        {
            Ids = [CharacterRules.Grey, .. CouncilCompanions.AvailableVersions(state).Skip(1).Select(p => p.AlternateId)],
            GuideId = CharacterRules.Grey
        });
        if (window == "homelands")
            return state;
        state = Apply(state, CommandKind.ChooseCouncilHomelands, new()
        {
            Placements = window == "token" ? [new() { UnitId = "base.character.legolas", RegionId = "woodland-realm" }] : []
        });
        if (window is "token" or "fellowship")
            return state;
        state = Apply(state, CommandKind.ContinueFellowship);
        if (window == "allocation")
            return state;
        if (window == "special")
        {
            Put(state, KeeperRings.Elrond, "rivendell");
            Put(state, KeeperRings.Galadriel, "lorien");
            LordsDiceRecovery.Recover(state, true);
        }
        state = Apply(state, CommandKind.AllocateHunt);
        if (window == "special")
            return state;
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        die.Face = DieFace.Muster;
        if (window == "action")
            return state;
        if (window == "vilya")
        {
            Put(state, KeeperRings.Elrond, "rivendell");
            return Apply(state, CommandKind.DrawCard, new() { DieId = die.Id, Deck = "character" }, DieFace.Event);
        }
        var unit = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        var shadowDie = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        shadowDie.Face = DieFace.Muster;
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 0;
        TurnFlow.SelectNext(state, Side.Shadow);
        return Apply(state, CommandKind.Recruit, new()
        {
            DieId = shadowDie.Id, Stepwise = true, Placements = [new() { UnitId = unit.Id, RegionId = "barad-dur" }]
        });
    }

    private static MatchState Apply(MatchState state, CommandKind kind, CommandPayload? payload = null, DieFace? face = null)
    {
        if (face.HasValue)
            state.Dice.Single(d => d.Id == payload!.DieId).Face = face.Value;
        var result = GameRuntime.Engine(EffectCatalog.All).Execute(state, state.Pending!.Owner,
            BattleFixture.Command(state, kind, payload), new SeededRandom(137));
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    private static void Put(MatchState state, string definition, string region)
    {
        var character = new CharacterState { Id = definition, Location = region };
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.All[definition]);
        state.Characters.Add(character);
    }
}
