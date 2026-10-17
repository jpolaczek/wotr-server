using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsShadowCardsFixture;

namespace Wotr.Tests;

public class LordsWeWontGoBackTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacementByNenyaOrMithrilOpensSmeagolCostBeforeHuntContinues(bool nenya)
    {
        var state = LordsNenyaTests.Window(mithril: !nenya);
        state.HuntTiles.AddRange(SmeagolTiles.Definitions.Select(d => new TileState { Id = d.Id, Location = "set-aside" }));
        SmeagolRules.Enter(state);
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.WeWontGoBack);
        HuntTests.Pool(state, SmeagolTiles.Definitions[1].Id);
        state = LordsFixture.Do(state, CommandKind.RespondEffect, new()
        {
            ReactionId = nenya ? NenyaReaction.Id : FreePeoplesCharacterCards01.ReplaceReaction
        }, new SequenceRandom(0));
        Assert.Equal("SmeagolCorruption", state.Pending!.Presentation);
        state = LordsCardsFixture.Choose(state, LordsShadowCards.WeWontGoBack);
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.NotEqual(DecisionKind.HuntDamage, state.Pending!.Kind);
    }

    internal static MatchState Window(bool protection = false)
    {
        var state = State();
        SmeagolRules.Enter(state);
        state = Play(state, LordsShadowCards.WeWontGoBack);
        EventTests.GiveCard(state, Side.Shadow, ShadowCharacterCards03.Lure);
        if (protection)
            state.Player(Side.FreePeoples).Table.Add("base.fp.character.05");
        HuntTests.Pool(state, SmeagolTiles.Definitions[1].Id);
        var shadow = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        shadow.Face = DieFace.Eye;
        shadow.Location = DieLocation.Hunt;
        shadow.HuntOrigin = "allocated";
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        die.Face = DieFace.Character;
        return LordsFixture.Do(state, CommandKind.MoveFellowship, new() { DieId = die.Id }, new SequenceRandom(5, 0));
    }

    [Theory]
    [InlineData(WeWontGoBackStep.Decline, 0, true)]
    [InlineData(LordsShadowCards.WeWontGoBack, 2, false)]
    [InlineData(ShadowCharacterCards03.Lure, 2, true)]
    public void BothCostsAndDeclineResolveBeforeAnyHuntProtection(string choice, int corruption, bool remains)
    {
        var state = Window(protection: true);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal("SmeagolCorruption", state.Pending.Presentation);
        Assert.Equal(0, state.Fellowship.Corruption);
        var entropy = state.RandomSamples.Count;
        state = LordsCardsFixture.Choose(state, choice);
        Assert.Equal(corruption, state.Fellowship.Corruption);
        Assert.Equal(remains, state.Player(Side.Shadow).Table.Contains(LordsShadowCards.WeWontGoBack));
        Assert.Equal(entropy, state.RandomSamples.Count);
        Assert.Equal("hunt.replace", state.Pending!.Window);
        var steps = ActionLogFamilyTests.Steps(state).Where(s => s.Revision == state.Revision).ToList();
        Assert.Equal(choice == WeWontGoBackStep.Decline ? 0 : 1, steps.SelectMany(s => s.Costs).Count());
        if (choice == ShadowCharacterCards03.Lure)
        {
            Assert.DoesNotContain(choice, ActionLogFamilyTests.Json(steps));
            Assert.Equal(ActionCostKind.Card, Assert.Single(steps.SelectMany(s => s.Costs)).Kind);
        }
    }

    [Fact]
    public void CostIsCharacterDeckAndTheOpponentNeverSeesHandOrSelectedDiscardIdentity()
    {
        var state = Window();
        EventTests.GiveCard(state, Side.Shadow, "base.sh.strategy.01");
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["base.sh.strategy.01"] });
        Assert.DoesNotContain(ShadowCharacterCards03.Lure, LordsCardsFixture.Json(state, Side.FreePeoples));
        state = LordsCardsFixture.Choose(state, ShadowCharacterCards03.Lure);
        Assert.DoesNotContain(ShadowCharacterCards03.Lure, LordsCardsFixture.Json(state, Side.FreePeoples));
        Assert.Contains(ShadowCharacterCards03.Lure, state.Player(Side.Shadow).Discard);
        Assert.Equal(2, state.Fellowship.Corruption);
    }

    [Fact]
    public void EmptyHandStillAllowsTableCostOrDeclineAndCorruptionTwelveEndsImmediately()
    {
        var state = Window(protection: true);
        state.Player(Side.Shadow).Hand.Clear();
        var choices = LordsFixture.Views().For(state, Side.Shadow).Options.Single(o => o.Kind == CommandKind.ChooseEffect);
        Assert.Equal(2, choices.EligibleIds.Count);
        state.Fellowship.Corruption = 10;
        state = LordsCardsFixture.Choose(state, LordsShadowCards.WeWontGoBack);
        Assert.Equal(GamePhase.Finished, state.Phase);
        Assert.Equal(Side.Shadow, state.Winner);
        Assert.Null(state.Pending);
    }

    [Fact]
    public void GuideConditionIsRequiredAtPlayAndCardExpiresWhenSmeagolLeaves()
    {
        var state = State();
        var cost = Cost(state, LordsShadowCards.WeWontGoBack);
        LordsFixture.Reject(state, CommandKind.PlayEvent, cost);
        SmeagolRules.Enter(state);
        state = LordsFixture.Do(state, CommandKind.PlayEvent, cost);
        SmeagolRules.Discard(state);
        ActiveEffects.CheckConditions(state);
        Assert.DoesNotContain(LordsShadowCards.WeWontGoBack, state.Player(Side.Shadow).Table);
        Assert.Contains(LordsShadowCards.WeWontGoBack, state.Player(Side.Shadow).Discard);
        state.Fellowship.Guide = CharacterRules.Strider;
        HuntTests.Pool(state, SmeagolTiles.Definitions[1].Id, HuntTests.Three);
        HuntRuntime.BeginTile(state, "later-hunt");
        var random = new SequenceRandom(1, 0);
        var runtime = new HuntRuntime(EffectCatalog.All);
        for (var step = 0; step < 12 && state.Pending is null; step++)
            runtime.Continue(state, random);
        Assert.Equal(2, random.Calls);
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        Assert.Equal(HuntTests.Three, HuntRuntime.Active(state)!.TileId);
        Assert.Equal(0, state.Fellowship.Corruption);
    }
}
