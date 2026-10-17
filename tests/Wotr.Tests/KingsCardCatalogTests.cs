using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class KingsCardCatalogTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void CatalogAppliesOnlyEnabledModuleReplacementsAndPreservesFateBrand(bool kings, bool lords, bool fate)
    {
        var configuration = new MatchConfiguration { Kings = kings, Lords = lords, Fate = fate };
        var deck = CardCatalog.ConfiguredDeck(configuration).Select(c => c.Id).ToHashSet();
        foreach (var card in CardCatalog.Kings.Values)
        {
            var enabled = kings && !(fate && card.ExcludedWithFate);
            Assert.Equal(enabled, deck.Contains(card.Id));
            Assert.Equal(enabled, CardCatalog.Enabled(configuration, card.Id));
            if (card.Replaces.Length > 0)
            {
                var replacedByFate = fate && card.ExcludedWithFate;
                Assert.Equal(!enabled && !replacedByFate, deck.Contains(card.Replaces));
                Assert.Equal(!enabled && !replacedByFate, CardCatalog.Enabled(configuration, card.Replaces));
                if (replacedByFate)
                    Assert.Contains(CardCatalog.Fate.Values.Single(c => c.Replaces == card.Replaces).Id, deck);
            }
        }
        Assert.Equal(!lords, deck.Contains(ShadowCharacterCards03.Balrog));
        Assert.Equal(lords, deck.Contains(LordsFreePeoplesCards.Rangers));
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, deck);
        Assert.False(CardCatalog.Enabled(configuration, "unknown"));
    }
}
