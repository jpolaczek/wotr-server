using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsRulerTokenTests
{
    internal static (MatchState State, CommandPayload Payload) Recruitment(string sovereign = SovereignCatalog.Brand)
    {
        var state = KingsRulerRecruitmentTests.Ready(sovereign);
        var choices = sovereign switch
        {
            SovereignCatalog.Dain => new[] { (UnitKind.Elite, "erebor"), (UnitKind.Regular, "iron-hills") },
            SovereignCatalog.Theoden => [(UnitKind.Regular, "edoras"), (UnitKind.Regular, "helms-deep"), (UnitKind.Regular, "edoras")],
            _ => [(UnitKind.Regular, "dale"), (UnitKind.Regular, "bree"), (UnitKind.Regular, "dale")]
        };
        var payload = KingsRulerRecruitmentTests.Recruit(state, sovereign, choices);
        payload.RulerTokenId = KingsRulerTokens.Id(sovereign);
        Ruler(state, DieFace.Character);
        return (state, payload);
    }

    [Theory]
    [InlineData(SovereignCatalog.Brand)]
    [InlineData(SovereignCatalog.Dain)]
    [InlineData(SovereignCatalog.Theoden)]
    public void TokenConvertsTheRulerAndPaysItsExactRecruitmentInOneCommand(string sovereign)
    {
        var (state, payload) = Recruitment(sovereign);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload));
        Assert.True(preview.Legal);
        Assert.Equal(new[] { payload.DieId, payload.RulerTokenId }, preview.Costs);
        Assert.Equal(payload.Placements.Count, preview.Placements.Count);
        Assert.Equal(before, StateCodec.Write(state));
        var option = KingsRulerTokenOptions.Actions(state, Side.FreePeoples).Single();
        Assert.Equal(payload.RulerTokenId, option.TokenId);
        Assert.Equal(payload.AbilityId, option.AbilityId);
        Assert.Equal(payload.DieId, Assert.Single(option.Action.EligibleDice));
        Assert.Empty(KingsRulerTokenOptions.Actions(state, Side.Shadow));
        state = Do(state, CommandKind.UseCharacterAbility, payload);
        foreach (var placement in payload.Placements)
            Assert.Equal(placement.RegionId, state.Units.Single(u => u.Id == placement.UnitId).Location);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Equal("removed", state.Kings!.Tokens.Single(t => t.SovereignId == sovereign).Location);
        Assert.True(state.Kings.RulerTokenUsedThisTurn);
        Assert.Empty(state.Kings.TokenActionSovereign);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        Ruler(state, DieFace.Character);
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
    }

    [Theory]
    [InlineData("wrong-token")]
    [InlineData("unknown-token")]
    [InlineData("wrong-ability")]
    [InlineData("ordinary-die")]
    [InlineData("ring-in-payload")]
    [InlineData("ring-used")]
    [InlineData("removed-token")]
    [InlineData("removed-sovereign")]
    [InlineData("unavailable-ruler")]
    [InlineData("wrong-region")]
    [InlineData("peace")]
    [InlineData("disabled-variant")]
    public void InvalidTokenCommandsSpendNothingAndDoNotRoll(string reason)
    {
        var (state, payload) = Recruitment();
        switch (reason)
        {
            case "wrong-token": payload.RulerTokenId = KingsRulerTokens.Id(SovereignCatalog.Dain); break;
            case "unknown-token": payload.RulerTokenId = "unknown"; break;
            case "wrong-ability": payload.AbilityId = "kings.iron-guard"; break;
            case "ordinary-die": payload.DieId = Die(state).Id; break;
            case "ring-in-payload": payload.RingId = state.Rings[0].Id; break;
            case "ring-used": state.Player(Side.FreePeoples).RingUsedThisTurn = true; break;
            case "removed-token": state.Kings!.Tokens[0].Location = "removed"; break;
            case "removed-sovereign": SovereignLifecycle.Remove(state, Sovereign(state, SovereignCatalog.Brand)); break;
            case "unavailable-ruler": KingsDice.Ruler(state, Side.FreePeoples)!.Location = DieLocation.Discarded; break;
            case "wrong-region": payload.Placements[0].RegionId = "barad-dur"; break;
            case "peace": state.Nations.Single(n => n.Id == "north").StepsToWar = 1; break;
            case "disabled-variant": state.Configuration.RemovingRulerTokens = true; break;
        }
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
    }

    [Fact]
    public void TokenCannotConvertTheRulerForAnUnrelatedOrdinaryAction()
    {
        var (state, payload) = Recruitment();
        payload.Deck = "character";
        LordsFixture.Reject(state, CommandKind.DrawCard, payload);
        payload.Face = DieFace.Event;
        payload.RingId = state.Rings[0].Id;
        LordsFixture.Reject(state, CommandKind.UseRing, payload);
    }

    internal static (MatchState State, CommandPayload Payload) Anarion()
    {
        var state = KingsAnarionTests.Ready();
        var die = Ruler(state, DieFace.Army);
        return (state, new() { DieId = die.Id, Deck = "strategy", RulerTokenId = KingsRulerTokens.Id(SovereignCatalog.Denethor) });
    }

    [Fact]
    public void DenethorsTokenRequiresTheExtraDrawBeforeCompletingItsCost()
    {
        var (state, payload) = Anarion();
        var before = state.Player(Side.FreePeoples).Hand.Count;
        Assert.Contains(KingsRulerTokenOptions.Actions(state, Side.FreePeoples), o => o.Action.Kind == CommandKind.DrawCard);
        state = Do(state, CommandKind.DrawCard, payload);
        Assert.Equal(SovereignCatalog.Denethor, state.Pending!.SourceId);
        Assert.DoesNotContain("decline", state.Pending.PrivateChoices);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == payload.DieId).Location);
        LordsFixture.Reject(state, CommandKind.ChooseEffect, new() { Ids = ["decline"] });
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = ["character"] });
        Assert.Equal(before + 2, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Empty(state.Kings!.TokenActionSovereign);
    }

    [Fact]
    public void ThranduilsTokenImmediatelyAppliesFarSightedToTheHunt()
    {
        var state = KingsFarSightedTests.Ready();
        Eyes(state, 2);
        var die = Ruler(state, DieFace.Muster);
        var payload = new CommandPayload { DieId = die.Id, RulerTokenId = KingsRulerTokens.Id(SovereignCatalog.Thranduil),
            AbilityId = KingsFarSighted.Ability };
        var option = KingsRulerTokenOptions.Actions(state, Side.FreePeoples).Single(o => o.Action.Kind == CommandKind.MoveFellowship);
        Assert.Equal(1, option.Action.RulerTravel!.HuntDiceReduction);
        state = Do(state, CommandKind.MoveFellowship, payload, 0);
        Assert.Equal(KingsFixture.SetupSamples + 1, state.RandomSamples.Count);
        Assert.Equal(DieLocation.Hunt, state.Dice.Single(d => d.Id == die.Id).Location);
        Assert.Equal("removed", state.Kings!.Tokens.Single().Location);
    }

    [Fact]
    public void DenethorsTokenCanPayAnEventAndKeepsItsRequiredDrawThroughNestedChoices()
    {
        var (state, payload) = Anarion();
        state.Player(Side.FreePeoples).Hand = ["base.fp.strategy.15"];
        payload.CardId = "base.fp.strategy.15";
        payload.CardCost = DieFace.Event;
        var option = KingsRulerTokenOptions.Actions(state, Side.FreePeoples).Single(o => o.Action.Kind == CommandKind.PlayEvent);
        Assert.All(option.Action.Events.SelectMany(e => e.Costs), c =>
        {
            Assert.Equal(payload.DieId, c.DieId);
            Assert.Equal(DieFace.Event, c.Face);
        });
        Assert.DoesNotContain(payload.CardId, Json(state, Side.Shadow));
        state = Do(state, CommandKind.PlayEvent, payload);
        Assert.NotEqual(SovereignCatalog.Denethor, state.Pending!.SourceId);
        state = Do(StateCodec.Copy(state), CommandKind.ChooseEffect, new() { Ids = [state.Pending.PrivateChoices[0]] });
        Assert.Equal(SovereignCatalog.Denethor, state.Pending!.SourceId);
        Assert.DoesNotContain("decline", state.Pending.PrivateChoices);
        state = Do(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
        Assert.Equal(2, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }

    [Fact]
    public void ThranduilsTokenAlsoSupportsTheLongerSeparationRoute()
    {
        var state = KingsFarSightedTests.Ready();
        var die = Ruler(state, DieFace.Event);
        const string merry = "base.character.merry";
        var option = KingsRulerTokenOptions.Actions(state, Side.FreePeoples).Single(o => o.Action.Kind == CommandKind.SeparateCompanions);
        Assert.Equal(2, option.Action.Travel.Single(t => t.Id == merry).Range);
        state = Do(state, CommandKind.SeparateCompanions, new()
        {
            DieId = die.Id, RulerTokenId = KingsRulerTokens.Id(SovereignCatalog.Thranduil),
            AbilityId = KingsFarSighted.Ability, Ids = [merry], Route = ["rivendell", "trollshaws", "hollin"]
        });
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == merry).Location);
        Assert.Equal("removed", state.Kings!.Tokens.Single().Location);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == die.Id).Location);
        state = Do(StateCodec.Copy(state), CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == die.Id).Location);
        Assert.Equal(KingsFixture.SetupSamples, state.RandomSamples.Count);
    }

    [Theory]
    [InlineData(SovereignCatalog.Denethor)]
    [InlineData(SovereignCatalog.Thranduil)]
    public void TokenDoesNotBypassTheAbilitysLocationRestrictions(string sovereign)
    {
        var (state, payload) = sovereign == SovereignCatalog.Denethor ? Anarion() : (KingsFarSightedTests.Ready(), new CommandPayload());
        if (sovereign == SovereignCatalog.Denethor)
            state.Characters.Single(c => c.Id == sovereign).Location = "osgiliath";
        else
        {
            state.Fellowship.MordorStep = 1;
            payload.DieId = KingsDice.Ruler(state, Side.FreePeoples)!.Id;
            payload.RulerTokenId = KingsRulerTokens.Id(sovereign);
            payload.AbilityId = KingsFarSighted.Ability;
        }
        LordsFixture.Reject(state, sovereign == SovereignCatalog.Denethor ? CommandKind.DrawCard : CommandKind.MoveFellowship, payload);
        Assert.Empty(KingsRulerTokenOptions.Actions(state, Side.FreePeoples));
    }

    [Theory]
    [InlineData("ordinary")]
    [InlineData("keeper")]
    [InlineData("table")]
    public void AnyElvenRingUseBlocksTokensUntilTheNextTurn(string source)
    {
        var (state, payload) = Recruitment();
        var ring = state.Rings[0];
        var cost = new CommandPayload { DieId = Die(state).Id, RingId = ring.Id, Face = DieFace.Event };
        if (source == "keeper")
        {
            state.Configuration.Lords = true;
            state.Lords = new();
            KeeperRings.Transfer(state, ring.Id);
            Assert.False(state.Player(Side.FreePeoples).RingUsedThisTurn);
        }
        else if (source == "table")
            TableRingCost.Pay(state, cost);
        else
            state = Do(state, CommandKind.UseRing, cost);
        Assert.True(state.Kings!.ElvenRingUsedThisTurn);
        Assert.Empty(KingsRulerTokenOptions.Actions(state, Side.FreePeoples));
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
        TurnFlow.BeginTurn(state);
        Assert.False(state.Kings.ElvenRingUsedThisTurn);
        Assert.False(state.Kings.RulerTokenUsedThisTurn);
    }

    [Fact]
    public void TokenBlocksAllElvenRingCostsButDoesNotBlockShadowsOwnRing()
    {
        var (state, payload) = Recruitment();
        state = Do(state, CommandKind.UseCharacterAbility, payload);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state.Configuration.Lords = true;
        state.Lords = new();
        var ring = state.Rings[0];
        LordsFixture.Reject(state, CommandKind.UseRing,
            new() { DieId = Die(state).Id, RingId = ring.Id, Face = DieFace.Event });
        Assert.False(KeeperRings.Available(state, ring.Id));
        Assert.Empty(TableRingCost.Rings(state));
        Assert.False(new TurnOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == CommandKind.UseRing).Enabled);
        ring.Owner = Side.Shadow;
        TurnFlow.SelectNext(state, Side.Shadow);
        state = Do(state, CommandKind.UseRing, new() { DieId = Die(state, Side.Shadow).Id, RingId = ring.Id, Face = DieFace.Event });
        Assert.Null(state.Rings.Single(r => r.Id == ring.Id).Owner);
    }

    [PostgresFact]
    public Task TokenAndAllRecruitsSurviveRestartAndRetryWithoutDuplicateCost()
    {
        var (state, payload) = Recruitment();
        return LordsPersistenceTests.Restart(state, CommandKind.UseCharacterAbility, payload);
    }

    [PostgresFact]
    public Task DenethorsTokenSurvivesRestartBeforeItsRequiredExtraDraw()
    {
        var (state, payload) = Anarion();
        state = Do(state, CommandKind.DrawCard, payload);
        return LordsPersistenceTests.Restart(state, CommandKind.ChooseEffect, new() { Ids = ["strategy"] });
    }
}
