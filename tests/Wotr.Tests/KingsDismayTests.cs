using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.KingsIntegrationFixture;

namespace Wotr.Tests;

public class KingsDismayTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void OmittedDismayMatchesItsPublishedDefaultForSavedCombinedGames(bool? setting, bool enabled)
    {
        var state = DismayWindow();
        state.Configuration.Dismay = setting;
        state = StateCodec.Copy(state);
        TurnFlow.BeginActions(state);
        Assert.Equal(enabled, LordsFixture.Views().For(state, Side.Shadow).Configuration.Dismay);
        Assert.Equal(enabled ? DecisionKind.Dismay : DecisionKind.Action, state.Pending!.Kind);
        if (enabled)
            state = KingsFixture.Do(state, CommandKind.UseDismay, Dismay(state), 0);
        var sovereign = KingsFixture.Sovereign(state, SovereignCatalog.Dain);
        SovereignLifecycle.Awaken(state, sovereign, "erebor");
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == sovereign.Id));
        SovereignLifecycle.Reconcile(state);
        Assert.Equal(enabled, KingsDismay.Tokens(state).Any(t => t.SovereignId == SovereignCatalog.Dain));
    }

    [Theory]
    [MemberData(nameof(KingsDiceTests.Faces), MemberType = typeof(KingsDiceTests))]
    public void RerollsEitherRetainedRulerUsingPrintedFacesAndOneToken(Side side, int roll, DieFace face)
    {
        var state = DismayWindow();
        var payload = Dismay(state, side);
        var command = BattleFixture.Command(state, CommandKind.UseDismay, payload);
        var original = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(payload.Ids, preview.Costs);
        Assert.Equal(original, StateCodec.Write(state));
        Assert.Null(LordsFixture.Views().For(state, Side.FreePeoples).Decision);
        Assert.Empty(LordsFixture.Views().For(state, Side.FreePeoples).Options);
        var next = KingsFixture.Do(StateCodec.Copy(state), command.Kind, payload, roll);
        var die = KingsDice.Ruler(next, side)!;
        Assert.Equal(face, die.Face);
        Assert.Equal(face == DieFace.Eye ? DieLocation.Hunt : DieLocation.Available, die.Location);
        Assert.Equal(face == DieFace.Eye ? "rolled-eye" : "", die.HuntOrigin);
        Assert.Equal(state.RandomSamples.Count + 1, next.RandomSamples.Count);
        Assert.Equal("removed", next.Kings!.Tokens.Single(t => KingsRulerTokens.Id(t.SovereignId) == payload.Ids.Single()).Location);
        Assert.Equal(DecisionKind.Dismay, next.Pending!.Kind);
        Assert.Empty(next.Kings.RulerDiscardOrder);
        Assert.Equal(2, LordsFixture.Views().For(next, Side.FreePeoples).RulerTokens.Count);
    }

    [Fact]
    public void RulerEyeCanLeaveHuntAndFurtherTokensRequireSeparateDecisions()
    {
        var state = DismayWindow();
        var die = KingsDice.Ruler(state, Side.Shadow)!;
        die.Face = DieFace.Eye;
        die.Location = DieLocation.Hunt;
        die.HuntOrigin = "rolled-eye";
        state = KingsFixture.Do(state, CommandKind.UseDismay, Dismay(state), 0);
        Assert.Equal(DieLocation.Available, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.Empty(KingsDice.Ruler(state, Side.Shadow)!.HuntOrigin);
        state = KingsFixture.Do(StateCodec.Copy(state), CommandKind.UseDismay, Dismay(state), 5);
        Assert.Equal(DieLocation.Hunt, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Empty(KingsDismay.Tokens(state));
        Assert.Equal(2, state.Events.Count(e => e.Kind == "DismayUsed"));
    }

    [Theory]
    [InlineData(DieLocation.Discarded)]
    [InlineData(DieLocation.Removed)]
    [InlineData(DieLocation.Spent)]
    [InlineData(DieLocation.Unrolled)]
    [InlineData(DieLocation.Resolving)]
    public void OnlyRetainedRulersMayBeRerolled(DieLocation location)
    {
        var state = DismayWindow();
        var payload = Dismay(state);
        KingsDice.Ruler(state, Side.Shadow)!.Location = location;
        LordsFixture.Reject(state, CommandKind.UseDismay, payload);
        Assert.DoesNotContain(payload.DieId, new DismayCommands().GetOptions(state, Side.Shadow).First().EligibleDice);
        KingsDice.Ruler(state, Side.FreePeoples)!.Location = location;
        TurnFlow.BeginActions(state);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(2, KingsDismay.Tokens(state).Count());
    }

    [Theory]
    [InlineData("ordinary-die")]
    [InlineData("keeper")]
    [InlineData("unknown-token")]
    [InlineData("duplicate-token")]
    [InlineData("disabled")]
    [InlineData("wrong-phase")]
    [InlineData("ring")]
    [InlineData("ruler-token")]
    public void ForgedRerollsNeverSpendOrRoll(string error)
    {
        var state = DismayWindow();
        var p = Dismay(state);
        switch (error)
        {
            case "ordinary-die": p.DieId = KingsFixture.Die(state, Side.Shadow).Id; break;
            case "keeper": p.DieId = LordsDiceCatalog.Nenya; break;
            case "unknown-token": p.Ids = ["not-a-token"]; break;
            case "duplicate-token": p.Ids.Add(p.Ids[0]); break;
            case "disabled": state.Configuration.Dismay = false; break;
            case "wrong-phase": state.Phase = GamePhase.ActionResolution; break;
            case "ring": p.RingId = LordsDiceSetup.NenyaRing; break;
            case "ruler-token": p.RulerTokenId = p.Ids[0]; break;
        }
        LordsFixture.Reject(state, CommandKind.UseDismay, p);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShadowChoosesLoreAndDismayOrderAndNeitherRepeatsAfterCompletion(bool loreFirst)
    {
        var state = DismayWindow(lore: true);
        state.Kings!.Tokens.RemoveAt(1);
        Assert.Contains(LordsFixture.Views().For(state, Side.Shadow).Options, o => o.Kind == CommandKind.UseLoreBeforeActions);
        if (loreFirst)
            state = KingsFixture.Do(state, CommandKind.UseLoreBeforeActions);
        else
            state = KingsFixture.Do(state, CommandKind.UseDismay, Dismay(state), 5);
        Assert.Equal("DieAndResult", state.Pending!.Presentation);
        state = LordsCardsFixture.Choose(StateCodec.Copy(state), KingsDice.Shadow, "lore.Muster.keep");
        if (loreFirst)
        {
            Assert.Equal(DecisionKind.Dismay, state.Pending!.Kind);
            Assert.DoesNotContain(LordsFixture.Views().For(state, Side.Shadow).Options, o => o.Kind == CommandKind.UseLoreBeforeActions);
            state = KingsFixture.Do(state, CommandKind.UseDismay, Dismay(state), 5);
        }
        Assert.Equal(loreFirst ? DieFace.Eye : DieFace.Muster, KingsDice.Ruler(state, Side.Shadow)!.Face);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(state.Turn, state.Lords!.LoreOfferedTurn);
    }

    [Fact]
    public void DecliningDismayKeepsTokensAndStillOffersLoreButNotDismayAgain()
    {
        var state = KingsFixture.Do(DismayWindow(lore: true), CommandKind.DeclineDismay);
        Assert.Equal("DieAndResult", state.Pending!.Presentation);
        state = LordsCardsFixture.Choose(state, LoreOfTheRings.Decline);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(2, KingsDismay.Tokens(state).Count());
        Assert.Equal(state.Turn, state.Kings!.DismayDeclinedTurn);
        state.Turn++;
        state.Phase = GamePhase.ActionRoll;
        TurnFlow.BeginActions(state);
        Assert.Equal(DecisionKind.Dismay, state.Pending!.Kind);
    }

    [Fact]
    public void UsingRulerTokenGrantsTheSamePhysicalDismayOnlyOnceDespiteLaterDeath()
    {
        var (state, payload) = KingsRulerTokenTests.Recruitment();
        state.Configuration.Dismay = true;
        state.Configuration.Lords = true;
        state = KingsFixture.Do(state, CommandKind.UseCharacterAbility, payload);
        Assert.Equal(KingsDismay.Location, Assert.Single(state.Kings!.Tokens).Location);
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == SovereignCatalog.Brand));
        SovereignLifecycle.Reconcile(state);
        Assert.Single(state.Events, e => e.Kind == "DismayGranted");
        Assert.Single(KingsDismay.Tokens(state));
        state.Kings.Tokens[0].Location = "removed";
        SovereignLifecycle.Reconcile(state);
        Assert.Empty(KingsDismay.Tokens(state));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DeathOfAwakenedGrantsTokenAccordingToVariantWithoutCreatingUnusedRuler(bool dismay, bool remove)
    {
        var state = Actions(new() { Lords = true, Kings = true, Dismay = dismay, RemovingRulerTokens = remove });
        var sovereign = KingsFixture.Sovereign(state, SovereignCatalog.Brand);
        SovereignLifecycle.Awaken(state, sovereign, "dale");
        Assert.Equal(remove ? 0 : 1, state.Kings!.Tokens.Count);
        if (remove)
        {
            KingsFixture.Ruler(state, DieFace.Muster);
            TurnFlow.SelectNext(state, Side.FreePeoples);
            Assert.Empty(KingsRulerTokenOptions.Actions(state, Side.FreePeoples));
            Assert.Empty(LordsFixture.Views().For(state, Side.FreePeoples).RulerTokens);
        }
        CharacterLifecycle.Eliminate(state, state.Characters.Single(c => c.Id == sovereign.Id));
        SovereignLifecycle.Reconcile(state);
        Assert.Equal(dismay ? 1 : 0, KingsDismay.Tokens(state).Count());
        Assert.Equal(SovereignStatus.Removed, sovereign.Status);
        SovereignLifecycle.Reconcile(state);
        Assert.Equal(dismay ? 1 : 0, state.Events.Count(e => e.Kind == "DismayGranted"));
    }

    [Theory]
    [InlineData(SovereignStatus.Initial)]
    [InlineData(SovereignStatus.Corrupted)]
    public void RemovalOfUnawakenedSovereignNeverGrantsDismay(SovereignStatus status)
    {
        var state = Actions(new() { Kings = true, Lords = true, RemovingRulerTokens = true });
        var sovereign = KingsFixture.Sovereign(state, SovereignCatalog.Brand);
        sovereign.Status = status;
        SovereignLifecycle.Remove(state, sovereign);
        Assert.Empty(state.Kings!.Tokens);
    }
}
