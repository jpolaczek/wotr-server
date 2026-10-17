using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.KingsFreePeoplesCards01;
using static Wotr.Tests.KingsCardsFixture;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsCards01CombatTests
{
    internal static void AddCompanion(MatchState state, string id)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        Companion(state, id, force.Region);
        force.Participants.Add(id);
    }

    internal static void AddSovereign(MatchState state, string id)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        SovereignLifecycle.Awaken(state, Sovereign(state, id), force.Region);
        force.Participants.Add(id);
    }

    [Theory]
    [MemberData(nameof(KingsCards01EventTests.Cards), MemberType = typeof(KingsCards01EventTests))]
    public void EveryCombatHalfValidatesModuleOwnerHandAndWindow(string card)
    {
        var state = Battle();
        AddCompanion(state, Merry);
        state.Player(Side.FreePeoples).Hand = [card];
        var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, new() { CardId = card });
        Assert.Null(LordsFixture.Engine().Validate(state, Side.FreePeoples, command));
        Assert.NotNull(LordsFixture.Engine().Validate(state, Side.Shadow, command));
        state.Configuration.Kings = false;
        LordsFixture.Reject(state, command.Kind, command.Payload);
        state.Configuration.Kings = true;
        state.Player(Side.FreePeoples).Hand.Clear();
        LordsFixture.Reject(state, command.Kind, command.Payload);
        state.Player(Side.FreePeoples).Hand = [card];
        state.Pending!.Kind = DecisionKind.Action;
        LordsFixture.Reject(state, command.Kind, command.Payload);
    }

    [Theory]
    [InlineData(Stewards)]
    [InlineData(Aid)]
    [InlineData(Dark)]
    public void RequiredCompanionOrSovereignMustActuallyParticipate(string card)
    {
        var state = Battle();
        Companion(state, Merry, "lossarnach");
        BattleRuntime.Active(state)!.Force(Side.FreePeoples).Participants.Remove(SovereignCatalog.Dain);
        state.Player(Side.FreePeoples).Hand = [card];
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = card });
    }

    [Fact]
    public void BannerForfeitsTheodensEntireFirstRoundLeadershipAndCancelsShadowReroll()
    {
        var state = Battle(SovereignCatalog.Theoden);
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        shadow.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, shadow.Region).Id);
        state = Cards(state, Aid, 0, 0, 0, 0);
        var battle = BattleRuntime.Active(state)!;
        Assert.Equal(3, battle.Force(Side.FreePeoples).Round.ForfeitedLeadership[SovereignCatalog.Theoden]);
        Assert.Equal(0, BattleLeadership.Total(state, battle.Force(Side.FreePeoples)));
        Assert.Equal(0, battle.Force(Side.Shadow).Round.RerollLimit);
        Assert.NotEqual(DecisionKind.BattleReroll, state.Pending!.Kind);
        Assert.Equal(KingsFixture.SetupSamples + 4, state.RandomSamples.Count);
        BattleRuntime.NextRound(battle);
        Assert.Empty(battle.Force(Side.FreePeoples).Round.ForfeitedLeadership);
        Assert.Equal(5, battle.Force(Side.Shadow).Round.RerollLimit);
    }

    [Fact]
    public void BannerChoosesOneCharacterAndCannotSpendAlreadyForfeitedLeadership()
    {
        var state = Battle();
        AddSovereign(state, SovereignCatalog.Theoden);
        state = Cards(state, Aid, 0, 0, 0, 0);
        Assert.Equal("CharacterLeadership", state.Pending!.Presentation);
        Assert.Equal(2, state.Pending.PrivateChoices.Count);
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        force.Round.ForfeitedLeadership[SovereignCatalog.Theoden] = 1;
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Theoden] });
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.ChooseEffect, new() { Ids = [SovereignCatalog.Dain] }));
        Assert.Contains(preview.Costs, c => c.Contains("forfeit 1 Leadership"));
        state = Choose(StateCodec.Copy(state), SovereignCatalog.Dain);
        Assert.Equal(1, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.ForfeitedLeadership[SovereignCatalog.Dain]);
        Assert.Equal(2, BattleLeadership.Total(state, BattleRuntime.Active(state)!.Force(Side.FreePeoples)));
    }

    [Fact]
    public void BannerAcceptsAragornButGodOfOldRequiresASovereign()
    {
        var state = Battle();
        var force = BattleRuntime.Active(state)!.Force(Side.FreePeoples);
        force.Participants.Remove(SovereignCatalog.Dain);
        var aragorn = new CharacterState { Id = CharacterRules.Strider, Location = force.Region };
        CharacterLifecycle.ApplyVersion(aragorn, CharacterCatalog.All[CharacterRules.Aragorn]);
        state.Characters.Add(aragorn);
        force.Participants.Add(aragorn.Id);
        state.Player(Side.FreePeoples).Hand = [Aid, Dark];
        LordsFixture.Reject(state, CommandKind.CommitCombatCard, new() { CardId = Dark });
        state = Cards(state, Aid, 0, 0, 0, 0, 0, 0);
        Assert.Equal(2, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.ForfeitedLeadership[aragorn.Id]);
    }

    [Theory]
    [InlineData(Aid)]
    [InlineData(Dark)]
    public void CancelledSovereignLeadershipCannotBeSpentOrUsedAsSpecialDice(string card)
    {
        var state = Battle();
        BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelledLeadership.Add(SovereignCatalog.Dain);
        state = Cards(state, card, 0, 0, 0, 0);
        var battle = BattleRuntime.Active(state)!;
        Assert.Empty(battle.Force(Side.FreePeoples).Round.ForfeitedLeadership);
        Assert.Equal(5, battle.Force(Side.Shadow).Round.RerollLimit);
        Assert.All(battle.SpecialAttacks, a => Assert.Empty(a.Dice));
        Assert.Equal(KingsFixture.SetupSamples + 4, state.RandomSamples.Count);
    }

    [Fact]
    public void GodOfOldUsesLeadershipAndThreePlusBeforeOrdinaryCombatWithSeparateLosses()
    {
        var state = Battle(SovereignCatalog.Theoden);
        BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CombatModifier = 3;
        state = Cards(state, Dark, 1, 2, 0);
        var battle = BattleRuntime.Active(state)!;
        var attack = Assert.Single(battle.SpecialAttacks);
        Assert.Equal(new[] { 2, 3, 1 }, attack.Dice.Select(d => d.Result));
        Assert.All(attack.Dice, d => { Assert.Equal(3, d.HitTarget); Assert.Equal(0, d.Modifier); });
        Assert.Single(attack.Dice, d => d.Hit);
        Assert.Empty(battle.Force(Side.FreePeoples).Round.Dice);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        var loss = BattleRules.Units(state, battle.Force(Side.Shadow)).First(BoardRules.IsArmyUnit).Id;
        state = Do(StateCodec.Copy(state), CommandKind.TakeBattleLosses, new() { Losses = [BattleFixture.Loss(loss)] }, 0, 0, 0);
        Assert.Equal("reserve", state.Units.Single(u => u.Id == loss).Location);
        Assert.Equal(2, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.Dice.Count);
        Assert.Single(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice);
    }

    [Theory]
    [InlineData(Aid)]
    [InlineData(Dark)]
    public void EarlierCancellationPreventsBothNewCombatEffects(string card)
    {
        var state = Battle();
        state.Player(Side.FreePeoples).Hand = [card];
        state.Player(Side.Shadow).Hand = [ShadowStrategyCards02.Moving];
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = card });
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = ShadowStrategyCards02.Moving }, 0, 0, 0, 0);
        var battle = BattleRuntime.Active(state)!;
        Assert.True(battle.Force(Side.FreePeoples).Round.CardCancelled);
        Assert.Empty(battle.SpecialAttacks);
        Assert.Empty(battle.Force(Side.FreePeoples).Round.ForfeitedLeadership);
        Assert.Equal(5, battle.Force(Side.Shadow).Round.RerollLimit);
    }

    [Fact]
    public void BraveStandCountsOnlyParticipatingCompanionsAndKeepsAtLeastOneShadowDie()
    {
        var state = Battle();
        AddCompanion(state, Gimli);
        AddCompanion(state, Merry);
        Companion(state, Boromir, "lossarnach");
        state = Cards(state, Stewards, 0, 0, 0, 0);
        var shadow = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.Equal(3, shadow.Round.CombatDicePenalty); // The awakened Dáin is also a Companion.
        Assert.Single(shadow.Round.Dice);
        Assert.Equal(KingsFixture.SetupSamples + 4, state.RandomSamples.Count);
    }

    [Fact]
    public void DaylightCapsShadowAtThreeDespiteFiveArmyUnits()
    {
        var state = Battle();
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        for (var i = 0; i < 3; i++)
            force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Regular, force.Region).Id);
        state = Cards(state, Riders, 0, 0, 0, 0, 0);
        Assert.Equal(3, BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice.Count);
    }

    [Fact]
    public void ConfusionUsesNaturalOnesDespiteModifiersAndExcludesThemFromRerolls()
    {
        var state = Battle();
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        force.Participants.Add(BattleFixture.Put(state, "sauron", UnitKind.Nazgul, force.Region).Id);
        force.Round.CombatModifier = 2;
        state = Cards(state, Wisdom, 0, 0, 0, 1);
        force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        Assert.True(force.Round.Dice[0].CannotReroll);
        Assert.False(force.Round.Dice[1].CannotReroll);
        Assert.DoesNotContain(force.Round.Dice[0].Id, BattleRoll.Failed(force));
        state = KingsChieftainBattleFixture.SkipRerolls(state);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Equal(1, state.Pending.RequiredCount);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public void ShieldWallRequiresTwoOrdinaryHitsAndCancelsExactlyOne(int hits, int cancelled)
    {
        var samples = new[] { 0, 0, 5, hits == 2 ? 5 : 0 };
        var state = KingsChieftainBattleFixture.SkipRerolls(Cards(Battle(), Brand, samples));
        Assert.Equal(cancelled, BattleRuntime.Active(state)!.Force(Side.FreePeoples).Round.CancelHits);
        Assert.Equal(DecisionKind.Losses, state.Pending!.Kind);
        Assert.Equal(hits - cancelled, state.Pending.RequiredCount);
    }
}
