using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class ExpansionPrivacyTests
{
    [Theory]
    [MemberData(nameof(ExpansionConfigurations.Legal), MemberType = typeof(ExpansionConfigurations))]
    public void EveryConfigurationHidesPrivateSetupHandsDeckOrderAndEntropyAcrossReload(int flags)
    {
        var state = ExpansionFixture.Start(ExpansionConfigurations.FromFlags(flags));
        if (state.Configuration.Lords)
            CheckPrivateVersions(state);
        state = ExpansionFixture.CompleteSetup(state);
        foreach (var viewer in Enum.GetValues<Side>())
        {
            var changed = StateCodec.Copy(state);
            var opponent = changed.Player(TurnFlow.Other(viewer));
            var deck = opponent.Decks[CardCatalog.All[opponent.Hand[0]].Deck];
            (opponent.Hand[0], deck[0]) = (deck[0], opponent.Hand[0]);
            foreach (var player in changed.Players)
                foreach (var hiddenDeck in player.Decks.Values)
                    hiddenDeck.Reverse();
            foreach (var sample in changed.RandomSamples)
                sample.Value = (sample.Value + 1) % sample.ExclusiveMaximum;
            var original = ExpansionFixture.Json(state, viewer);
            Assert.Equal(original, ExpansionFixture.Json(StateCodec.Copy(changed), viewer));
            var detached = ExpansionFixture.Views.For(changed, viewer);
            detached.Configuration.Fate = !state.Configuration.Fate;
            Assert.Equal(state.Configuration.Fate, changed.Configuration.Fate);
        }
    }

    private static void CheckPrivateVersions(MatchState state)
    {
        Assert.Null(ExpansionFixture.Views.For(state, Side.Shadow).Decision);
        Assert.Empty(ExpansionFixture.Views.For(state, Side.Shadow).Options);
        var normal = KingsFixture.Do(state, CommandKind.ChooseCompanionVersions, ExpansionFixture.Versions(state));
        var different = KingsFixture.Do(state, CommandKind.ChooseCompanionVersions,
            new() { Ids = [LordsCharacters.Gandalf], GuideId = CharacterRules.Strider });
        Assert.Equal(ExpansionFixture.Json(normal, Side.Shadow), ExpansionFixture.Json(different, Side.Shadow));
        Assert.NotEqual(ExpansionFixture.Json(normal, Side.FreePeoples), ExpansionFixture.Json(different, Side.FreePeoples));
        ExpansionFixture.Reload(normal);
        ExpansionFixture.Reload(different);
    }
}
