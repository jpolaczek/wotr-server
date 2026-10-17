using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class ExpansionCompositionTests
{
    [Theory]
    [MemberData(nameof(ExpansionConfigurations.Legal), MemberType = typeof(ExpansionConfigurations))]
    public void EveryLegalConfigurationHasExactSourceSlotsFiguresDiceTilesAndFirstTurn(int flags)
    {
        var config = ExpansionConfigurations.FromFlags(flags);
        var state = ExpansionFixture.Start(config);
        CheckCards(state);
        CheckFiguresAndMap(state);
        CheckResources(state);
        Assert.Equal(config.Lords, state.Lords is not null);
        Assert.Equal(config.Kings, state.Kings is not null);
        Assert.Equal(config.Council, state.Lords?.Council is not null);
        Assert.Equal(96 + (config.Lords ? 12 : 0) + (config.Kings ? 8 : 0) - 4, state.RandomSamples.Count);
        Assert.All(state.Players, player => Assert.Equal(config.Lords ? 0 : 2, player.Hand.Count));
        var started = StateCodec.Write(state);
        Assert.Throws<InvalidOperationException>(() => TurnFlow.Start(state));
        Assert.Equal(started, StateCodec.Write(state));
        state = ExpansionFixture.CompleteSetup(state);
        Assert.Equal(1, state.Turn);
        Assert.Equal(GamePhase.ActionResolution, state.Phase);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.All(state.Players, player => Assert.Equal(2, player.Hand.Count));
        Assert.Equal(4, TurnFlow.Available(state, Side.FreePeoples));
        Assert.Equal(7, TurnFlow.Available(state, Side.Shadow));
        Assert.Equal(config.Council ? 6 : 7, state.Fellowship.Companions.Count);
        Assert.Equal(config.Council ? new[] { CouncilTokens.Politics } : [],
            state.Lords?.Council?.GrantedTokens ?? []);
        CheckCards(state);
        CheckFirstActions(state);
    }

    private static void CheckCards(MatchState state)
    {
        var cards = state.Players.SelectMany(player => player.Hand.Concat(player.Decks.Values.SelectMany(deck => deck))).ToArray();
        var expected = ExpectedCards(state.Configuration);
        Assert.Equal(expected.Order(), cards.Order());
        Assert.Equal(cards.Length, cards.Distinct().Count());
        foreach (var card in CardCatalog.All.Values)
        {
            Assert.Equal(expected.Contains(card.Id) || state.Configuration.Lords && card.Id == SmeagolRules.WeShallGetIt,
                CardCatalog.Enabled(state.Configuration, card.Id));
            if (expected.Contains(card.Id))
            {
                Assert.Contains(card.Id, EffectCatalog.All.Events.Keys);
                Assert.Contains(card.Id, EffectCatalog.All.Combats.Keys);
            }
        }
    }

    internal static HashSet<string> ExpectedCards(MatchConfiguration config)
    {
        var slots = (from side in new[] { "fp", "sh" }
                     from deck in new[] { "character", "strategy" }
                     from number in Enumerable.Range(1, 24)
                     select $"{side}.{deck}.{number:00}").ToDictionary(slot => slot, slot => "base." + slot);
        if (config.Lords)
        {
            slots["sh.character.17"] = "lords.sh.character.17";
            Add("lords", 4, 2);
        }
        if (config.Kings)
        {
            foreach (var slot in new[] { "fp.character.23", "fp.strategy.08", "fp.strategy.16", "fp.strategy.24",
                "sh.strategy.01", "sh.strategy.03", "sh.strategy.05", "sh.strategy.06", "sh.strategy.18" })
                slots[slot] = "kings." + slot;
            if (!config.Fate)
                foreach (var slot in new[] { "fp.strategy.19", "fp.strategy.22" })
                    slots[slot] = "kings." + slot;
            Add("kings", 2, 2);
        }
        if (config.Fate)
            foreach (var slot in new[] { "fp.character.17", "fp.strategy.04", "fp.strategy.19", "fp.strategy.22",
                "sh.strategy.20", "sh.strategy.22" })
                slots[slot] = "fate." + slot;
        return slots.Values.ToHashSet(StringComparer.Ordinal);

        void Add(string module, int character, int strategy)
        {
            foreach (var side in new[] { "fp", "sh" })
                foreach (var (deck, count) in new[] { ("character", character), ("strategy", strategy) })
                    foreach (var number in Enumerable.Range(25, count))
                    {
                        var id = $"{module}.{side}.{deck}.{number}";
                        slots.Add(id, id);
                    }
        }
    }

    private static void CheckFiguresAndMap(MatchState state)
    {
        var config = state.Configuration;
        var sovereigns = config.Kings ? config.Fate ? 3 : 5 : 0;
        Assert.Equal(193 + sovereigns, state.Units.Count);
        Assert.Equal(state.Units.Count, state.Units.Select(unit => unit.Id).Distinct().Count());
        Assert.Equal(sovereigns, state.Kings?.Sovereigns.Count ?? 0);
        if (config.Kings && config.Fate)
            Assert.Equal(new[] { SovereignCatalog.Denethor, SovereignCatalog.Theoden, SovereignCatalog.Thranduil },
                state.Kings!.Sovereigns.Select(sovereign => sovereign.Id).ToArray());
        Assert.All(state.Units.Where(unit => unit.Id.StartsWith("kings.")), unit => Assert.Equal(UnitKind.Leader, unit.Kind));
        Assert.Equal(config.Fate ? "sauron" : "dwarves", BoardRules.Region(state, "erebor").Nation);
        Assert.Equal(config.Fate ? Side.Shadow : Side.FreePeoples, state.Control["erebor"]);
        Assert.Equal(config.NewCities ? "City" : "Town", BoardRules.Region(state, "ered-luin").Settlement);
        Assert.Equal(config.NewCities ? "City" : "Town", BoardRules.Region(state, "south-rhun").Settlement);
        Assert.Equal(config.Fate, state.Nations.Single(nation => nation.Id == "dwarves").Active);
        foreach (var side in Enum.GetValues<Side>())
            Assert.All(ExpansionFixture.Views.For(state, side).Players, player => Assert.Equal(0, player.VictoryPoints));
        var gimli = CharacterCatalog.Companion(state, FateGimli.Figure);
        Assert.Equal(config.Fate ? FateGimli.Standard : FateGimli.Figure, gimli.DefinitionId);
    }

    private static void CheckResources(MatchState state)
    {
        var config = state.Configuration;
        Assert.Equal(16 + (config.Lords ? 5 : 0) + (config.Kings ? 2 : 0), state.Dice.Count);
        Assert.Equal(state.Dice.Count, state.Dice.Select(die => die.Id).Distinct().Count());
        Assert.Equal(4, state.Dice.Count(die => die.Owner == Side.FreePeoples && die.Location == DieLocation.Unrolled));
        Assert.Equal(7, state.Dice.Count(die => die.Owner == Side.Shadow && die.Location == DieLocation.Unrolled));
        Assert.All(state.Dice.Where(die => die.Source != "standard"), die => Assert.Equal(DieLocation.Removed, die.Location));
        Assert.Equal(config.Lords ? new[] { LordsDiceSetup.VilyaRing, LordsDiceSetup.NenyaRing, LordsDiceSetup.NaryaRing }
            : new[] { "base.ring.1", "base.ring.2", "base.ring.3" }, state.Rings.Select(ring => ring.Id));
        Assert.Equal(24 + (config.Lords ? 4 : 0) + (config.Kings ? 2 : 0), state.HuntTiles.Count);
        Assert.Equal(state.HuntTiles.Count, state.HuntTiles.Select(tile => tile.Id).Distinct().Count());
        foreach (var tile in state.HuntTiles)
        {
            var inPool = tile.Id is "lords.hunt.smeagol.1" or "lords.hunt.smeagol.2" || HuntPool.Definitions[tile.Id].Pool == "standard";
            Assert.Equal(inPool ? "pool" : "set-aside", tile.Location);
        }
        Assert.Empty(state.Kings?.Tokens ?? []);
    }

    private static void CheckFirstActions(MatchState state)
    {
        foreach (var side in Enum.GetValues<Side>())
        {
            Assert.Equal(side, state.Pending!.Owner);
            var die = state.Dice.First(candidate => candidate.Owner == side && candidate.Location == DieLocation.Available);
            var count = TurnFlow.Available(state, side);
            var samples = state.RandomSamples.Count;
            state = KingsFixture.Do(ExpansionFixture.Reload(state), CommandKind.DiscardDie, new() { DieId = die.Id });
            Assert.Equal(count - 1, TurnFlow.Available(state, side));
            Assert.Equal(samples, state.RandomSamples.Count);
        }
        ExpansionFixture.Reload(state);
    }
}
