using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.FellowshipEventCardTests;
using static Wotr.Domain.FreePeoplesCharacterCards03;

namespace Wotr.Tests;

public class MirrorAndChallengeTests
{
    [Theory]
    [InlineData(Mirror)]
    [InlineData(Challenge)]
    [InlineData(Gwaihir)]
    [InlineData(Swifter)]
    [InlineData(ThereAndBack)]
    [InlineData(Eagles)]
    public void AllSixEventsValidateOwnershipCostAndLocation(string card)
    {
        var state = card == Challenge ? ChallengeState() : card == Eagles ? EaglesCardTests.BeforeAttack() : TurnTests.Actions();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        Cost(state, DieFace.Character);
        var payload = EventTests.Play(state, card, DieFace.Character).Payload;
        payload.CardCost = DieFace.Army;
        Reject(state, CommandKind.PlayEvent, payload);
        payload.CardCost = DieFace.Character;
        state.Dice.First(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        Reject(state, CommandKind.PlayEvent, payload);
        state.Dice.First(d => d.Id == payload.DieId).Location = DieLocation.Available;
        HandRules.Remove(state.Player(Side.FreePeoples), card);
        Reject(state, CommandKind.PlayEvent, payload);
    }

    internal static MatchState MirrorChoice()
    {
        var state = TurnTests.Actions();
        foreach (var die in state.Dice.Where(d => d.Owner == Side.FreePeoples))
            die.Face = DieFace.Character;
        state.Fellowship.RegionId = "lorien";
        state.Fellowship.Progress = 2;
        state.Fellowship.Corruption = 2;
        return Play(state, Mirror);
    }

    [Fact]
    public void MirrorExcludesPaidDieAndHealsAtTheFigureDespiteProgressAndSiege()
    {
        var state = MirrorChoice();
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "lorien");
        Assert.Equal(2, state.Fellowship.Corruption);
        Assert.DoesNotContain(state.Dice[0].Id, state.Pending!.PrivateChoices);
        Reject(state, CommandKind.ChooseEffect, new() { Ids = [state.Dice[0].Id] });
        var selected = state.Pending.PrivateChoices[0];
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [selected] });
        Assert.Equal(DieFace.WillOfTheWest, state.Dice.Single(d => d.Id == selected).Face);
        Assert.Equal(DieLocation.Available, state.Dice.Single(d => d.Id == selected).Location);
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
    }

    [Theory]
    [InlineData("lorien", Side.FreePeoples, 1)]
    [InlineData("lorien", Side.Shadow, 2)]
    [InlineData("rivendell", Side.FreePeoples, 2)]
    public void MirrorStillPlaysWithoutAnUnusedCharacterResult(string region, Side owner, int corruption)
    {
        var state = TurnTests.Actions();
        foreach (var die in state.Dice)
            die.Face = DieFace.Army;
        state.Fellowship.RegionId = region;
        state.Control["lorien"] = owner;
        state.Fellowship.Corruption = 2;
        state = Play(state, Mirror);
        Assert.Equal(corruption, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Dice, d => d.Face == DieFace.WillOfTheWest);
    }

    [Fact]
    public void MirrorAutomaticallyConvertsItsOnlyEligibleDieAndHealingStopsAtZero()
    {
        var state = TurnTests.Actions();
        foreach (var die in state.Dice)
            die.Face = DieFace.Army;
        var target = state.Dice.Last(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        target.Face = DieFace.Character;
        state.Fellowship.RegionId = "lorien";
        state = Play(state, Mirror);
        Assert.Equal(DieFace.WillOfTheWest, state.Dice.Single(d => d.Id == target.Id).Face);
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
    }

    internal static MatchState ChallengeState(string region = "minas-tirith", bool aragorn = false)
    {
        var state = BattleFixture.State();
        Put(state, CharacterRules.Strider, region);
        if (aragorn)
            CharacterLifecycle.ApplyVersion(state.Characters.Single(), CharacterCatalog.Base[CharacterRules.Aragorn]);
        CharacterLifecycle.ReconcileDice(state);
        BattleFixture.Put(state, "gondor", UnitKind.Regular, region);
        state.RandomSamples.Clear();
        return state;
    }

    [Theory]
    [InlineData("minas-tirith", true)]
    [InlineData("edoras", true)]
    [InlineData("osgiliath", false)]
    [InlineData("lorien", false)]
    public void ChallengeRequiresKingWithArmyInGondorOrRohan(string region, bool legal)
    {
        var state = ChallengeState(region);
        Assert.Equal(legal, new ChallengeOfTheKingCondition().CanPlay(state, Side.FreePeoples));
        state.Characters.Single().Garrison = true;
        Assert.False(new ChallengeOfTheKingCondition().CanPlay(state, Side.FreePeoples));
        state.Characters.Single().Garrison = false;
        state.Units.Single(u => u.Location == region).Kind = UnitKind.Leader;
        Assert.False(new ChallengeOfTheKingCondition().CanPlay(state, Side.FreePeoples));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ChallengeReturnsNonEyesAndRemovesEyesUnlessAllThreeAreEyes(int eyes)
    {
        var state = ChallengeState(aragorn: true);
        var chosen = state.HuntTiles.Where(t => HuntPool.Definitions[t.Id].Value == "eye").Take(eyes)
            .Concat(state.HuntTiles.Where(t => HuntPool.Definitions[t.Id].Pool == "standard" && HuntPool.Definitions[t.Id].Value != "eye").Take(3 - eyes)).Select(t => t.Id).ToArray();
        HuntTests.Pool(state, chosen);
        state.Player(Side.FreePeoples).Table.AddRange([FreePeoplesCharacterCards02.Staff, FreePeoplesCharacterCards01.Mithril, FreePeoplesCharacterCards01.Axe]);
        state = Play(state, Challenge, 0, 0, 0);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal(eyes == 3 ? "casualties" : "minas-tirith", state.Characters.Single().Location);
        Assert.Equal(eyes == 3 ? 0 : 1, state.Player(Side.FreePeoples).ExtraDiceNextRecovery);
        Assert.All(state.HuntTiles.Where(t => chosen.Contains(t.Id)), t => Assert.Equal(
            eyes != 3 && HuntPool.Definitions[t.Id].Value == "eye" ? "removed" : "pool", t.Location));
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Empty(state.Resolution.Frames);
        Assert.DoesNotContain(state.Events, e => e.Kind == "HuntTileDrawn");
    }

    [Fact]
    public void ChallengeRefillsOnlyBeforeTheNextDrawAndCountsSpecialEye()
    {
        var state = ChallengeState();
        var special = state.HuntTiles.First(t => HuntPool.Definitions[t.Id] is { Pool: "special-sh", Value: "eye" });
        HuntTests.Pool(state, special.Id);
        var discarded = state.HuntTiles.Where(t => HuntPool.Definitions[t.Id] is { Pool: "standard", Value: "3" }).Take(2).ToList();
        discarded.ForEach(t => t.Location = "discard");
        state = Play(state, Challenge, 0, 0, 0);
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == special.Id).Location);
        Assert.All(state.HuntTiles.Where(t => discarded.Any(d => d.Id == t.Id)), t => Assert.Equal("pool", t.Location));
        Assert.Equal(new[] { 1, 2, 1 }, state.RandomSamples.Select(s => s.ExclusiveMaximum));
    }
}
