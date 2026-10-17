using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;
using static Wotr.Tests.Shadow04Fixture;

namespace Wotr.Tests;

public class Shadow04InteractionTests
{
    [Fact]
    public void CharacterEntryPreviewIncludesDeferredActivationWithoutChangingTheLiveNation()
    {
        var state = BattleFixture.State();
        Put(state, CharacterRules.Strider, "minas-tirith");
        var gondor = state.Nations.Single(n => n.Id == "gondor");
        gondor.Active = false;
        gondor.StepsToWar = 2;
        var payload = Cost(state, DieFace.WillOfTheWest);
        payload.CharacterId = CharacterRules.Aragorn;
        payload.RegionId = "minas-tirith";
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.EnterCharacter, payload));
        Assert.True(preview.Legal);
        var change = Assert.Single(preview.PoliticsChanges);
        Assert.Equal("gondor", change.Id);
        Assert.True(change.Active);
        Assert.Equal(2, change.StepsToWar);
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Theory]
    [InlineData(CharacterRules.Strider, "north")]
    [InlineData(CharacterRules.Aragorn, "free-peoples")]
    public void OldSaveCorrectsStriderWithoutReplacingHisFigureOrChangingAragorn(string version, string icon)
    {
        var state = BattleFixture.State();
        var figure = Put(state, version, "helms-deep");
        figure.NationIcon = "free-peoples";
        var loaded = StateCodec.Read(StateCodec.Write(state));
        var restored = Assert.Single(loaded.Characters);
        Assert.Equal(icon, restored.NationIcon);
        Assert.Equal(figure.Id, restored.Id);
        Assert.Equal(figure.DefinitionId, restored.DefinitionId);
        Assert.Equal(figure.Location, restored.Location);
        Assert.Equal(state.Pending!.Id, loaded.Pending!.Id);
        Assert.Equal(state.Revision, loaded.Revision);
        Assert.Equal(StateCodec.Write(loaded), StateCodec.Write(StateCodec.Read(StateCodec.Write(loaded))));
    }

    internal static MatchState PalantirGrond()
    {
        var state = State(Grond, true);
        BattleFixture.Put(state, "sauron", UnitKind.Elite, "minas-tirith");
        Table(state, Palantir);
        EventTests.GiveCard(state, Side.Shadow, Wormtongue);
        var cost = EventCost(state, Grond);
        cost.CardCost = DieFace.Event;
        state.Dice.Single(d => d.Id == cost.DieId).Face = DieFace.Event;
        state = Do(state, CommandKind.PlayEvent, cost);
        return Do(state, CommandKind.AttackArmy, Army(state, "minas-tirith", "minas-tirith"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SorcererDrawsAfterFirstRoundPalantirAfterAllFourAndOnlyIfSarumanSurvives(bool killSaruman)
    {
        var state = PalantirGrond();
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = Wormtongue }, new SequenceRandom(new int[7]));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        Assert.Equal("battle.round.closed", state.Pending!.Window);
        Assert.Empty(state.Player(Side.Shadow).Hand);
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = "character.sorcerer" });
        Assert.Single(state.Player(Side.Shadow).Hand);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        state = BlankRound(state);
        state = BlankRound(state);
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        Assert.Single(state.Player(Side.Shadow).Hand);
        var elite = state.Units.Single(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Elite);
        state = Do(state, CommandKind.ExtendSiege, new() { Ids = [elite.Id] });
        if (killSaruman)
            CharacterLifecycle.Eliminate(state, CharacterRules.InPlay(state, CharacterRules.Saruman)!);
        state = BlankRound(StateCodec.Copy(state));
        Assert.Null(BattleRuntime.Active(state));
        if (!killSaruman)
        {
            Assert.Equal("Deck", state.Pending!.Presentation);
            state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        }
        Assert.Equal(killSaruman ? 1 : 2, state.Player(Side.Shadow).Hand.Count);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void OrdinaryEventDrawDoesNotDrawAnAdditionalPalantirCard()
    {
        var state = State();
        Table(state, Palantir);
        var count = state.Player(Side.Shadow).Hand.Count;
        var payload = Cost(state, DieFace.Event);
        payload.Deck = "strategy";
        state = Do(state, CommandKind.DrawCard, payload);
        Assert.Equal(count + 1, state.Player(Side.Shadow).Hand.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [Fact]
    public void ActualWitchKingEntryLeavesWormtongueAndRohanPassive()
    {
        var state = Shadow04WormtongueTests.Passive();
        state.Characters.RemoveAll(c => c.DefinitionId == CharacterRules.WitchKing);
        TurnFlow.SelectNext(state, Side.Shadow);
        var payload = Cost(state, DieFace.Muster);
        payload.CharacterId = CharacterRules.WitchKing;
        payload.RegionId = "osgiliath";
        state = Do(state, CommandKind.EnterCharacter, payload);
        Assert.NotNull(CharacterRules.InPlay(state, CharacterRules.WitchKing));
        Assert.False(state.Nations.Single(n => n.Id == "rohan").Active);
        Assert.Contains(Wormtongue, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData("the-shire", true)]
    [InlineData("edoras", false)]
    [InlineData("minas-tirith", false)]
    public void StridersPrintedNationIconActivatesOnlyTheNorth(string region, bool activates)
    {
        var state = BattleFixture.State(Side.FreePeoples);
        var nation = state.Nations.Single(n => n.Id == BoardRules.Regions[region].Nation);
        nation.Active = false;
        nation.StepsToWar = 2;
        Put(state, CharacterRules.Strider, region);
        var payload = Cost(state, DieFace.Character);
        payload.CompanionMoves = [new() { Ids = [CharacterRules.Strider], Route = [region] }];
        state = Do(state, CommandKind.MoveCompanions, payload);
        Assert.Equal(activates, state.Nations.Single(n => n.Id == nation.Id).Active);
        Assert.Equal(2, state.Nations.Single(n => n.Id == nation.Id).StepsToWar);
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void RetreatIntoRohanDoesNotTriggerEntryOrCompanionActivation(Side retreating)
    {
        var state = BattleFixture.State();
        var rohan = state.Nations.Single(n => n.Id == "rohan");
        rohan.Active = false;
        rohan.StepsToWar = 3;
        var unit = BattleFixture.Put(state, retreating == Side.Shadow ? "isengard" : "rohan", UnitKind.Regular, "fords-of-isen");
        var force = new BattleForce { Side = retreating, Region = "fords-of-isen", Participants = [unit.Id] };
        if (retreating == Side.FreePeoples)
            force.Participants.Add(Put(state, CharacterRules.Grey, "fords-of-isen").Id);
        BattleMovement.Retreat(state, new() { Region = force.Region, Defender = force }, "helms-deep");
        Assert.DoesNotContain(state.PoliticalReactions, r => r.Trigger is PoliticalTrigger.EnemyEntry or PoliticalTrigger.CompanionArrival);
        PoliticalReactions.Complete(state);
        Assert.False(rohan.Active);
    }
}
