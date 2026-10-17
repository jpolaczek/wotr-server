using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards03;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow03Fixture;

namespace Wotr.Tests;

public class Shadow03EventTests
{
    [Theory]
    [InlineData(Lure)]
    [InlineData(Breaking)]
    [InlineData(Worn)]
    [InlineData(Crebain)]
    [InlineData(Balrog)]
    [InlineData(Lidless)]
    public void EveryEventValidatesActorCostWindowAndOwnership(string card)
    {
        var state = RevealedEvent(card);
        var cost = EventCost(state, card);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, cost);
        Assert.Null(Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(Engine().Validate(state, Side.FreePeoples, command));
        cost.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, cost);
        cost.CardCost = DieFace.Character;
        HandRules.Remove(state.Player(Side.Shadow), card);
        Reject(state, CommandKind.PlayEvent, cost);
        HandRules.Add(state.Player(Side.Shadow), card);
        state.Dice.Single(d => d.Id == cost.DieId).Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, cost);
        state.Dice.Single(d => d.Id == cost.DieId).Location = DieLocation.Available;
        state.Pending!.Kind = DecisionKind.Reaction;
        Reject(state, CommandKind.PlayEvent, cost);
    }

    [Theory]
    [InlineData(Lure, false)]
    [InlineData(Lure, true)]
    [InlineData(Breaking, false)]
    [InlineData(Breaking, true)]
    public void RevealedConditionAndGollumUseDirectCorruptionWithoutRandomness(string card, bool mordor)
    {
        var state = RevealedEvent(card, mordor);
        state.Fellowship.Hidden = true;
        Reject(state, CommandKind.PlayEvent, EventCost(state, card));
        state.Fellowship.Hidden = false;
        state.Fellowship.Companions.Clear();
        state.Fellowship.Guide = CharacterRules.Gollum;
        state.Fellowship.Corruption = 11;
        state = Play(state, card);
        Assert.Equal(12, state.Fellowship.Corruption);
        Assert.Equal(Side.Shadow, state.Winner);
        Assert.Empty(state.RandomSamples);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LureOffersCorruptionOrTheAlreadyDrawnCompanionWithoutHuntProtection(bool eliminate)
    {
        var state = RevealedEvent(Lure);
        Table(state, Worn);
        Table(state, FreePeoplesCharacterCards01.Mithril);
        Table(state, FreePeoplesCharacterCards01.Axe);
        var id = "base.character.gimli";
        state = Play(state, Lure, state.Fellowship.Companions.IndexOf(id));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal("LureCompanion", state.Pending.Presentation);
        Assert.Null(HuntRuntime.Active(state));
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [eliminate ? LureChoiceStep.Eliminate : LureChoiceStep.Corruption] });
        state = ResolveGuide(state);
        Assert.Equal(eliminate ? 0 : 2, state.Fellowship.Corruption);
        Assert.Equal(!eliminate, state.Fellowship.Companions.Contains(id));
        Assert.Single(state.RandomSamples);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntCasualty");
        Assert.Contains(Worn, state.Player(Side.Shadow).Table);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LureHobbitReturnsOnlyOutsideMordor(bool mordor)
    {
        var state = RevealedEvent(Lure, mordor);
        var id = "base.character.merry";
        state = Play(state, Lure, state.Fellowship.Companions.IndexOf(id));
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = [LureChoiceStep.Eliminate] });
        if (!mordor)
        {
            Assert.Equal(DecisionKind.CompanionSeparation, state.Pending!.Kind);
            state = Do(state, CommandKind.SeparateCompanions, new() { Ids = [id], Route = ["hollin", "north-dunland"] });
        }
        state = ResolveGuide(state);
        Assert.Equal(mordor ? "casualties" : "north-dunland", state.Characters.Single(c => c.Id == id).Location);
        Assert.Equal(0, state.Fellowship.Corruption);
    }

    [Theory]
    [InlineData(Worn)]
    [InlineData(Crebain)]
    [InlineData(Balrog)]
    public void TableCardsStayInPlayAndExpireOnlyOnTheirDeclarationCondition(string card)
    {
        var state = Play(BeforeEvent(card), card);
        Assert.Contains(card, state.Player(Side.Shadow).Table);
        Assert.Contains(state.ActiveEffects, e => e.SourceId == card);
        state.Fellowship.RegionId = "rivendell";
        state.Phase = GamePhase.Fellowship;
        TurnFlow.OpenFellowshipPhase(state);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["rivendell"] });
        Assert.Equal(card == Balrog, state.Player(Side.Shadow).Table.Contains(card));
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void LidlessMovesUpToThreeUnusedDiceWithoutUsingThePaidDie(int count)
    {
        var state = BeforeEvent(Lidless);
        var paid = EventCost(state, Lidless).DieId;
        state = Play(state, Lidless);
        var choices = Choices(state);
        Assert.DoesNotContain(paid, choices.EligibleIds);
        var ids = choices.EligibleIds.Take(count).ToList();
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [paid] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = choices.EligibleIds.Take(4).ToList() });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [choices.EligibleIds[0], choices.EligibleIds[0]] });
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [null!] });
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ids });
        Assert.Equal(count, state.Dice.Count(d => d.HuntOrigin == "lidless-eye" && d.Location == DieLocation.Hunt && d.Face == DieFace.Eye));
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == paid).Location);
        Assert.Empty(state.RandomSamples);
    }
}
