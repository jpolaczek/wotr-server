using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsIntegrationFixture;

namespace Wotr.Tests;

public class KingsVariantMatrixTests
{
    public static IEnumerable<object[]> Configurations()
    {
        for (var bits = 0; bits < 512; bits++)
        {
            var config = Configuration(bits);
            if (config.Kings && ConfigurationRules.IsValid(config))
                yield return [bits];
        }
    }

    private static MatchConfiguration Configuration(int bits) => new()
    {
        Lords = (bits & 1) != 0, Kings = (bits & 2) != 0, Fate = (bits & 4) != 0,
        Council = (bits & 8) != 0, Dismay = (bits & 16) != 0, VisibleCorruption = (bits & 32) != 0,
        SequentialCorruption = (bits & 64) != 0, RemovingRulerTokens = (bits & 128) != 0, NewCities = (bits & 256) != 0
    };

    [Theory]
    [MemberData(nameof(Configurations))]
    public void EveryLegalKingsConfigurationPreservesVariantBehaviorAndBothViewsAcrossSave(int flags)
    {
        var config = Configuration(flags);
        var state = Actions(config);
        KingsFixture.Eyes(state, 2);
        KingsFixture.Pool(state, KingsCorruptionTests.One);
        state = KingsFixture.Do(state, CommandKind.AttemptCorruption, new()
        {
            CharacterId = SovereignCatalog.Thranduil, DieId = CorruptionRules.Eyes(state).First().Id
        }, 0);
        if (!config.SequentialCorruption)
        {
            Assert.Equal(DecisionKind.CorruptionTile, state.Pending!.Kind);
            state = KingsFixture.Do(state, CommandKind.ChooseCorruptionTile, new() { Ids = [KingsCorruptionTests.One] });
        }
        Assert.Null(CorruptionRuntime.Current(state));
        var fp = LordsFixture.Views().For(state, Side.FreePeoples).Sovereigns.Single(s => s.Id == SovereignCatalog.Thranduil);
        Assert.Equal(config.VisibleCorruption ? 1 : (int?)null, fp.Corruption);
        Assert.Equal(config.VisibleCorruption ? KingsCorruptionTests.One : "", fp.Tiles.Single().TileId);
        Assert.Equal(config.VisibleCorruption ? "non-Kings Corruption tile" : "hidden Corruption tile",
            KingsCorruptionRemoval.TileDescription(state));
        var sovereign = KingsFixture.Sovereign(state, SovereignCatalog.Denethor);
        SovereignLifecycle.Awaken(state, sovereign, "minas-tirith");
        Assert.Equal(config.RemovingRulerTokens ? 0 : 1, state.Kings!.Tokens.Count);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == sovereign.Id));
        SovereignLifecycle.Reconcile(state);
        Assert.Equal(config.Dismay == true ? 1 : 0, KingsDismay.Tokens(state).Count());
        if (config.Council)
        {
            TurnFlow.SelectNext(state, Side.Shadow);
            state = KingsFixture.Do(state, CommandKind.EnterCharacter, KingsCouncilIntegrationTests.Payload(state));
            Assert.Single(state.Lords!.Council!.SpentTokens);
        }
        var saved = StateCodec.Copy(state);
        Assert.Equal(StateCodec.Write(state), StateCodec.Write(saved));
        foreach (var side in Enum.GetValues<Side>())
            Assert.Equal(KingsFixture.Json(state, side), KingsFixture.Json(saved, side));
        var view = LordsFixture.Views().For(state, Side.FreePeoples);
        Assert.Equal(config.VisibleCorruption, view.Configuration.VisibleCorruption);
        Assert.Equal(config.SequentialCorruption, view.Configuration.SequentialCorruption);
        view.Configuration.VisibleCorruption = !config.VisibleCorruption;
        Assert.Equal(config.VisibleCorruption, state.Configuration.VisibleCorruption);
        Assert.True(ConfigurationRules.IsImplemented(config));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SequentialWestUsesUnspentEyesAndResumesItsEventWithoutAnAdditionalCost(bool visible)
    {
        var state = KingsWestTests.Ready(3);
        state.Configuration.SequentialCorruption = true;
        state.Configuration.VisibleCorruption = visible;
        KingsFixture.Pool(state, HuntTests.Three);
        state = KingsCardsFixture.Play(state, KingsCards02.West);
        state = KingsCardsFixture.Choose(state, SovereignCatalog.Dain, 0);
        Assert.Equal(3, CorruptionRuntime.Current(state)!.DrawLimit);
        Assert.Equal(EffectReturn.Parent, CorruptionRuntime.Current(state)!.Return);
        Assert.Empty(CorruptionRuntime.Current(state)!.DieId);
        state = Draw(state, KingsCorruptionTests.One);
        state = KingsFixture.Do(state, CommandKind.ChooseCorruptionTile, new() { Ids = [KingsCorruptionTests.One] });
        Assert.Equal(new[] { KingsCorruptionTests.One }, KingsFixture.Sovereign(state, SovereignCatalog.Dain).Tiles);
        Assert.Equal(3, CorruptionRules.Eyes(state).Count());
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.PaidDieIds);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(2, state.RandomSamples.Count);
    }
}
