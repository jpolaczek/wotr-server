using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public sealed class BattleCardBoundaryTests
{
    private const string Scouts = FreePeoplesStrategyCards01.SpiritOfMordor;
    private const string GreatHost = ShadowStrategyCards03.ManyKings;

    private static Projection Projection => GameRuntime.Projection(EffectCatalog.All);

    [Theory]
    [InlineData(ShadowStrategyCards01.Dawn)]
    [InlineData(ShadowStrategyCards02.Rage)]
    [InlineData(ShadowStrategyCards03.KingRevealed)]
    public void EveryRelentlessAssaultCardMarksItsVoluntaryLoss(string card)
    {
        var state = CardsRevealed(Side.Shadow, shadowCard: card);

        Assert.True(BattleLosses.Current(state)!.Voluntary);
        Assert.Equal(card, BattleLosses.Current(state)!.SourceId);
        AssertBattleMarked(state, completesPaidAction: true);
    }

    [Fact]
    public void AnotherVoluntaryLossSourceStaysUnmarked()
    {
        var state = CardsRevealed(Side.Shadow, shadowCard: ShadowStrategyCards02.Rage);
        BattleLosses.Current(state)!.SourceId = ShadowStrategyCards01.UrukHai;

        Assert.Null(Projection.For(state, Side.Shadow).ActionBoundary);
    }

    [Fact]
    public void GreatHostMarksTheLossItsOutnumberingArmyDeals()
    {
        var state = GreatHostLoss();

        Assert.Equal(1, BattleLosses.Current(state)!.Hits);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Empty(BattleRuntime.Active(state)!.SpecialAttacks);
        AssertBattleMarked(state, completesPaidAction: false);
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("hits")]
    [InlineData("source")]
    [InlineData("work")]
    [InlineData("army-size")]
    public void GreatHostRequiresItsExactFrames(string change)
    {
        var state = GreatHostLoss();
        var battle = BattleRuntime.Active(state)!;
        switch (change)
        {
            case "cursor": state.Resolution.Frames[1].NextStep = 0; break;
            case "hits": BattleLosses.Current(state)!.Hits = 2; break;
            case "source": BattleLosses.Current(state)!.SourceId = ShadowStrategyCards01.UrukHai; break;
            case "work": battle.NextWork = 0; break;
            case "army-size":
                foreach (var unit in BattleRules.Units(state, battle.Force(Side.Shadow)).Where(BoardRules.IsArmyUnit).Skip(1))
                    BoardRules.ReturnToReserve(unit);
                break;
        }

        Assert.Null(Projection.For(state, state.Pending!.Owner).ActionBoundary);
    }

    [Fact]
    public void ScoutsMarksTheChoiceBeforeTheRoll()
    {
        var state = ScoutsChoice();

        Assert.Equal(DecisionKind.EffectChoice, state.Pending!.Kind);
        Assert.Equal("RegionOrShelter", state.Pending.Presentation);
        Assert.Equal(Scouts, state.Pending.SourceId);
        AssertBattleMarked(state, completesPaidAction: true);
    }

    [Theory]
    [InlineData("cursor")]
    [InlineData("work")]
    [InlineData("stage")]
    [InlineData("revealed")]
    [InlineData("card")]
    [InlineData("choices")]
    [InlineData("payment")]
    public void ScoutsRequiresItsExactFrames(string change)
    {
        var state = ScoutsChoice();
        var battle = BattleRuntime.Active(state)!;
        switch (change)
        {
            case "cursor": state.Resolution.Frames[1].NextStep = 0; break;
            case "work": battle.NextWork = 0; break;
            case "stage": battle.Stage = BattleStage.AfterLosses; break;
            case "revealed": battle.CardsRevealed = false; break;
            case "card": battle.Defender.Round.CardId = ""; break;
            case "choices": state.Pending!.PrivateChoices = ["altered"]; break;
            case "payment":
                state.Dice.Single(die => die.Id == state.Resolution.PaidDieIds[0]).Location = DieLocation.Spent;
                break;
        }

        Assert.Null(Projection.For(state, Side.FreePeoples).ActionBoundary?.Battle);
    }

    [Fact]
    public void TheShadowDoesNotReadTheScoutsOptions()
    {
        var state = ScoutsChoice();

        Assert.NotNull(Projection.For(state, Side.Shadow).ActionBoundary?.Battle);
        Assert.Empty(Projection.For(state, Side.Shadow).Options);
    }

    private static void AssertBattleMarked(MatchState state, bool completesPaidAction)
    {
        Assert.All(Enum.GetValues<Side>(), side =>
        {
            var context = Assert.IsType<BattleContextView>(Projection.For(state, side).ActionBoundary?.Battle);
            Assert.Equal(completesPaidAction, context.CompletesPaidAction);
            Assert.Equal(!completesPaidAction, context.SpecialAttackLoss);
        });
    }

    private static MatchState CardsRevealed(Side attacker, string shadowCard = "", string freePeoplesCard = "",
        int shadowUnits = 3, int freePeoplesUnits = 3)
    {
        var state = BattleFixture.State(attacker);
        var attackingNation = attacker == Side.Shadow ? "sauron" : "gondor";
        var defendingNation = attacker == Side.Shadow ? "gondor" : "sauron";
        var attackingCount = attacker == Side.Shadow ? shadowUnits : freePeoplesUnits;
        var defendingCount = attacker == Side.Shadow ? freePeoplesUnits : shadowUnits;
        var attackers = Enumerable.Range(0, attackingCount)
            .Select(_ => BattleFixture.Put(state, attackingNation, UnitKind.Regular, BattleFixture.Origin)).ToArray();
        for (var index = 0; index < defendingCount; index++)
            BattleFixture.Put(state, defendingNation, UnitKind.Regular, BattleFixture.Target);
        if (shadowCard.Length != 0)
            state.Player(Side.Shadow).Hand.Add(shadowCard);
        if (freePeoplesCard.Length != 0)
            state.Player(Side.FreePeoples).Hand.Add(freePeoplesCard);
        state = BattleFixture.Begin(state, attackers.Select(unit => unit.Id));

        while (state.Pending!.Kind == DecisionKind.CombatCard)
        {
            var card = state.Pending.Owner == Side.Shadow ? shadowCard : freePeoplesCard;
            state = BattleFixture.Execute(state, CommandKind.CommitCombatCard, new() { CardId = card },
                new SequenceRandom(new int[64]));
        }
        return state;
    }

    private static MatchState GreatHostLoss()
    {
        var state = CardsRevealed(Side.Shadow, shadowCard: GreatHost, shadowUnits: 5, freePeoplesUnits: 2);
        while (state.Pending!.Kind == DecisionKind.BattleReroll)
            state = BattleFixture.Execute(state, CommandKind.RerollBattle, new(), new SequenceRandom(new int[64]));
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        return state;
    }

    private static MatchState ScoutsChoice() => CardsRevealed(Side.Shadow, freePeoplesCard: Scouts);
}
