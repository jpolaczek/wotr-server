using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class ExpansionConfigurationTests
{
    [Fact]
    public void All512ExplicitFlagsMatchTheSourceTableAndInvalidInputsConsumeNoRandomness()
    {
        var counts = new int[8];
        foreach (var flags in Enumerable.Range(0, 512))
        {
            var configuration = ExpansionConfigurations.FromFlags(flags);
            var allowed = ExpansionConfigurations.Allowed(flags);
            Assert.Equal(allowed, ConfigurationRules.IsValid(configuration));
            if (allowed)
            {
                counts[flags & 7]++;
                continue;
            }
            var random = new SequenceRandom();
            var conflict = Assert.IsType<ConfigurationConflict>(ConfigurationRules.Conflict(configuration));
            var exception = Assert.Throws<ArgumentException>(() => MatchFactory.Create("invalid", random, configuration));
            Assert.Contains(conflict.RuleId, exception.Message);
            Assert.Contains(conflict.Source, exception.Message);
            Assert.Equal(0, random.Calls);
        }
        Assert.Equal(new[] { 1, 2, 4, 24, 2, 4, 8, 48 }, counts);
    }

    [Fact]
    public void OmittedDismayHasTheSameLegalityAndSetupAsItsEffectiveDefaultForAll256Inputs()
    {
        var legal = 0;
        foreach (var flags in Enumerable.Range(0, 512).Where(flags => (flags & 16) == 0))
        {
            var configuration = ExpansionConfigurations.FromFlags(flags);
            configuration.Dismay = null;
            var effective = ConfigurationRules.Normalize(configuration);
            var expectedFlags = flags | (configuration.Lords && configuration.Kings ? 16 : 0);
            var allowed = ExpansionConfigurations.Allowed(expectedFlags);
            Assert.Equal(allowed, ConfigurationRules.IsValid(configuration));
            Assert.Equal(allowed, ConfigurationRules.IsValid(effective));
            if (!allowed)
            {
                var random = new SequenceRandom();
                Assert.Throws<ArgumentException>(() => MatchFactory.Create("invalid", random, configuration));
                Assert.Equal(0, random.Calls);
                continue;
            }
            legal++;
            var omitted = ExpansionFixture.Start(configuration);
            var explicitValue = ExpansionFixture.Start(effective);
            omitted.Configuration.Dismay = effective.Dismay;
            Assert.Equal(StateCodec.Write(omitted), StateCodec.Write(explicitValue));
        }
        Assert.Equal(69, legal);
    }

    [Fact]
    public void ConflictingFateKingsSlotsFailWithPublicSourceDiagnosticsIndependentOfOrder()
    {
        var configuration = new MatchConfiguration { Lords = true, Kings = true, Fate = true };
        var cards = CardCatalog.ConfiguredDeck(configuration).ToArray();
        DeckComposition.Validate(configuration, cards.Reverse().ToArray());
        foreach (var slot in new[] { "19", "22" })
        {
            var fate = "fate.fp.strategy." + slot;
            var kings = CardCatalog.Kings["kings.fp.strategy." + slot];
            foreach (var invalid in new[]
            {
                cards.Where(card => card.Id != fate).ToArray(),
                cards.Append(cards.Single(card => card.Id == fate)).ToArray(),
                cards.Append(kings).ToArray(),
                cards.Where(card => card.Id != fate).Append(kings).Reverse().ToArray()
            })
            {
                var conflict = Assert.Throws<ContentConflictException>(() => DeckComposition.Validate(configuration, invalid));
                Assert.Equal("X03", conflict.RuleId);
                Assert.Equal("KINGS pp.18–19", conflict.SourceReference);
                Assert.Equal("base.fp.strategy." + slot, conflict.SlotId);
            }
        }
    }

    [Fact]
    public void MissingAddedCardsSpecialCardsAndDuplicateSlotsCannotPassComposition()
    {
        var config = new MatchConfiguration { Lords = true, Kings = true };
        var cards = CardCatalog.ConfiguredDeck(config).ToArray();
        var addition = cards.First(card => card.Id.StartsWith("lords.") && card.Replaces == "");
        var missing = cards.Where(card => card.Id != addition.Id).ToArray();
        Assert.Throws<ContentConflictException>(() => DeckComposition.Validate(config, missing));
        Assert.Throws<ContentConflictException>(() => DeckComposition.Validate(config,
            cards.Append(CardCatalog.Lords[SmeagolRules.WeShallGetIt]).ToArray()));
        Assert.Throws<ContentConflictException>(() => DeckComposition.Validate(config, cards.Append(cards[0]).ToArray()));
    }
}
