using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// UI-198 (user report): the history names cards and characters by title, never by id ("Drew base.fp.character.07.").
public sealed class EventTextTests
{
    [Fact]
    public void ADrawnCardIsNamedByItsTitleOnlyForItsOwner()
    {
        var state = TurnTests.Actions();
        var card = state.Player(Side.FreePeoples).Decks["character"][0];
        var title = CardCatalog.Title(card);
        Assert.NotEqual(card, title);

        CardFlow.Draw(state, Side.FreePeoples, "character");

        var own = state.Events.Last(e => e.Kind == "CardDrawn");
        Assert.Equal($"Drew {title}.", own.Text);
        Assert.Equal(Side.FreePeoples, own.OnlyFor);
        Assert.DoesNotContain(state.Events.Where(e => e.OnlyFor != Side.FreePeoples),
            e => e.Text.Contains(title, StringComparison.Ordinal) || e.Text.Contains(card, StringComparison.Ordinal));
    }

    [Fact]
    public void TitlesComeFromTheContentAndAnUnknownIdStandsForItself()
    {
        foreach (var card in CardCatalog.Base.Values)
            Assert.Equal(card.Title, CardCatalog.Title(card.Id));
        foreach (var character in CharacterCatalog.Base.Values)
            Assert.Equal(character.Title, CharacterCatalog.Title(character.Id));
        Assert.Equal("unknown.card", CardCatalog.Title("unknown.card"));
        Assert.Equal("unknown.character", CharacterCatalog.Title("unknown.character"));
    }
}
