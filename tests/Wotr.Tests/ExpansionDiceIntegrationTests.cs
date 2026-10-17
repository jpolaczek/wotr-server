using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class ExpansionDiceIntegrationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CompleteFateRosterUsesLordsChoicesRulerDiscardsThenTheodensDismay(bool cities, bool removing)
    {
        var state = Rolling(cities, removing);
        foreach (var (side, id) in new[] { (Side.Shadow, LordsDiceCatalog.Gothmog), (Side.FreePeoples, LordsDiceCatalog.Nenya) })
        {
            Assert.Equal(DecisionKind.SpecialDieChoice, state.Pending!.Kind);
            Assert.Equal(side, state.Pending.Owner);
            Assert.Contains(id, LordsDiceRoll.LegalChoices(state, side));
            state = KingsFixture.Do(ExpansionFixture.Reload(state), CommandKind.ChooseSpecialDie, new() { DieId = id });
        }
        foreach (var side in new[] { Side.Shadow, Side.FreePeoples })
        {
            Assert.Equal(DecisionKind.RulerDiscard, state.Pending!.Kind);
            Assert.Equal(side, state.Pending.Owner);
            var candidates = KingsDice.DiscardCandidates(state, side);
            Assert.DoesNotContain(candidates, id => state.Dice.Single(die => die.Id == id).Special is not null);
            var standard = candidates.First(id => state.Dice.Single(die => die.Id == id).Source == "standard");
            state = KingsFixture.Do(ExpansionFixture.Reload(state), CommandKind.DiscardRulerDie, new() { DieId = standard });
        }
        Assert.Equal(DecisionKind.Dismay, state.Pending!.Kind);
        Assert.Equal(SovereignCatalog.Theoden, Assert.Single(KingsDismay.Tokens(state)).SovereignId);
        state = KingsFixture.Do(ExpansionFixture.Reload(state), CommandKind.UseDismay, new()
        {
            DieId = KingsDice.Shadow, Ids = [KingsRulerTokens.Id(SovereignCatalog.Theoden)]
        }, 5);
        Assert.Empty(KingsDismay.Tokens(state));
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(DieLocation.Hunt, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.Equal(2, state.Dice.Count(die => die.Special?.Chosen == true && die.Location == DieLocation.Hunt));
        Assert.DoesNotContain(state.Kings!.Tokens, token => token.SovereignId is SovereignCatalog.Dain or SovereignCatalog.Brand);
        Assert.Equal(removing ? 0 : 1, state.Kings.Tokens.Count(token => token.Location == "free-peoples"));
        ExpansionFixture.Reload(state);
    }

    internal static MatchState Rolling(bool cities, bool removing)
    {
        var state = ExpansionInteractionTests.Actions(cities, removing: removing);
        // Legal later-turn prerequisites are seeded after the real setup; this is not a full game replay.
        foreach (var (dieId, region) in new[] { (LordsDiceCatalog.Vilya, "rivendell"), (LordsDiceCatalog.Nenya, "lorien"),
            (LordsDiceCatalog.Balrog, "moria"), (LordsDiceCatalog.Gothmog, "minas-morgul") })
            LordsCharacterFixture.Put(state, LordsDiceCatalog.Definitions[dieId].CharacterId, region);
        LordsCharacterFixture.Put(state, KingsChieftains.Serpent, "near-harad");
        SovereignLifecycle.Awaken(state, KingsFixture.Sovereign(state, SovereignCatalog.Theoden), "edoras");
        CharacterLifecycle.Eliminate(state, state.Characters.Single(character => character.Id == SovereignCatalog.Theoden));
        SovereignLifecycle.Awaken(state, KingsFixture.Sovereign(state, SovereignCatalog.Thranduil), "woodland-realm");
        state.Turn = 2;
        DiceRules.Recover(state);
        var samples = state.Dice.Where(die => die.Location == DieLocation.Unrolled)
            .Select(die => die.Special is null ? 0 : 5).ToArray();
        TurnFlow.RollAndBeginActions(state, new SequenceRandom(samples));
        return state;
    }
}
