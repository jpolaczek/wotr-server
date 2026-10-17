using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class FellowshipTests
{
    internal static readonly GameEngine Engine = new(new TurnCommands(), new FellowshipCommands(), new EffectCommands(), new HuntCommands());
    internal static readonly Projection Views = new(new TurnOptions(), new FellowshipOptions());
    internal static MatchState Phase(string region = "rivendell", int progress = 0)
    {
        var state = KernelTests.NewGame();
        TurnFlow.Start(state);
        state.Fellowship.RegionId = region;
        state.Fellowship.Progress = progress;
        return state;
    }

    internal static MatchState Do(MatchState state, CommandKind kind, CommandPayload payload) => EventTests.Execute(state, Command(state, kind, payload), Engine);
    internal static GameCommand Command(MatchState state, CommandKind kind, CommandPayload payload)
    {
        var command = KernelTests.Command(state, kind);
        command.Payload = payload;
        return command;
    }

    internal static void Reject(MatchState state, CommandKind kind, CommandPayload payload)
    {
        var before = StateCodec.Write(state);
        var random = new SequenceRandom();
        Assert.False(Engine.Execute(state, state.Pending!.Owner, Command(state, kind, payload), random).Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(state));
        Assert.Equal(0, random.Calls);
    }

    [Fact]
    public void V09DeclarationHealsOnceWithoutRevealingAndCanStayInPlace()
    {
        var state = Phase("minas-tirith");
        state.Fellowship.Corruption = 4;
        var before = StateCodec.Write(state);
        var payload = new CommandPayload { Route = ["minas-tirith"] };
        var preview = new PreviewService(Engine).Preview(state, Side.FreePeoples, Command(state, CommandKind.DeclareFellowship, payload));
        Assert.True(preview.Legal);
        Assert.Equal(1, preview.Fellowship!.Healed);
        var activation = Assert.Single(preview.PoliticsChanges);
        Assert.Equal("gondor", activation.Id);
        Assert.True(activation.Active);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(state, CommandKind.DeclareFellowship, payload);
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(0, state.Fellowship.Progress);
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.True(state.Nations.Single(n => n.Id == "gondor").Active);
        Reject(state, CommandKind.DeclareFellowship, payload);
        Assert.DoesNotContain(Views.For(state, Side.FreePeoples).Options, o => o.Kind == CommandKind.DeclareFellowship);
        TurnFlow.BeginTurn(state);
        state = Do(state, CommandKind.DeclareFellowship, payload);
        Assert.Equal(2, state.Fellowship.Corruption);
    }

    [Theory]
    [InlineData(false, Side.FreePeoples, true)]
    [InlineData(false, Side.Shadow, false)]
    [InlineData(true, Side.FreePeoples, false)]
    public void HealingDistinguishesOriginalOwnershipFromCurrentControl(bool shadowStronghold, Side controller, bool heals)
    {
        var region = shadowStronghold ? "moria" : "minas-tirith";
        var state = Phase(region);
        state.Fellowship.Corruption = 1;
        state.Control[region] = controller;
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine).Preview(state, Side.FreePeoples,
            Command(state, CommandKind.DeclareFellowship, new() { Route = [region] }));
        Assert.Equal(heals ? 1 : 0, preview.Fellowship!.Healed);
        Assert.Equal(!shadowStronghold && controller == Side.FreePeoples ? 1 : 0, preview.PoliticsChanges.Count);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = [region] });
        Assert.Equal(heals ? 0 : 1, state.Fellowship.Corruption);
        if (!shadowStronghold)
            Assert.Equal(controller == Side.FreePeoples, state.Nations.Single(n => n.Id == "gondor").Active);
    }

    [Fact]
    public void DeclarationUsesChosenLegalEdgesAndPassesShadowStrongholdsWithoutExtraHunts()
    {
        var state = Phase("rivendell", 5);
        Reject(state, CommandKind.DeclareFellowship, new() { Route = ["rivendell", "high-pass"] });
        Reject(state, CommandKind.DeclareFellowship, new() { Route = ["hollin"] });
        Reject(state, CommandKind.DeclareFellowship, new() { Route = ["rivendell", "fords-of-bruinen", "hollin", "dimrill-dale", "lorien"] });
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = ["rivendell", "fords-of-bruinen", "hollin", "moria", "dimrill-dale", "lorien"] });
        Assert.Equal("lorien", state.Fellowship.RegionId);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
    }

    [Fact]
    public void RevealingUsesTheFaqSanctuaryRestrictionWithoutHealingOrActivation()
    {
        var state = TurnTests.Actions();
        state.Fellowship.RegionId = "osgiliath";
        state.Fellowship.Progress = 1;
        state.Fellowship.Corruption = 3;
        FellowshipFlow.BeginReveal(state, "test-reveal", FellowshipReturn.Decision);
        Assert.False(state.Fellowship.Hidden);
        Reject(state, CommandKind.RevealFellowship, new() { Route = ["osgiliath", "minas-tirith"] });
        state.Control["minas-tirith"] = Side.Shadow;
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["osgiliath", "minas-tirith"] });
        Assert.Equal("minas-tirith", state.Fellowship.RegionId);
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.False(state.Nations.Single(n => n.Id == "gondor").Active);
        // A captured FP Stronghold is not an original Shadow Stronghold.
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
    }

    [Fact]
    public void RevealingInAConqueredShadowStrongholdIsAllowedAndAddsNoHunt()
    {
        var state = TurnTests.Actions();
        state.Fellowship.RegionId = "moria";
        state.Control["moria"] = Side.FreePeoples;
        FellowshipFlow.BeginReveal(state, "test-reveal", FellowshipReturn.Decision);
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["moria"] });
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
    }

    [Fact]
    public void EachDifferentShadowStrongholdQueuesAHuntAndKeepsThePaidContinuationOpen()
    {
        var state = TurnTests.Actions();
        state.Fellowship.RegionId = "moria";
        state.Fellowship.Progress = 5;
        DiceRules.Pay(state, Side.FreePeoples, state.Dice[0].Id, DieFace.Character);
        FellowshipFlow.BeginReveal(state, "test-hunt", FellowshipReturn.Decision);
        var payload = new CommandPayload { Route = ["moria", "hollin", "moria", "dimrill-dale", "north-anduin-vale", "dol-guldur"] };
        var preview = new PreviewService(Engine).Preview(state, Side.FreePeoples, Command(state, CommandKind.RevealFellowship, payload));
        Assert.True(preview.HasUnknownResult);
        Assert.Equal(new[] { "moria", "dol-guldur" }, preview.Fellowship!.AdditionalHuntRegions);
        // Inspect the T15 hand-off before T16's automatic tile execution.
        FellowshipFlow.Reveal(state, payload.Route);
        var restored = StateCodec.Copy(state);
        var procedure = FellowshipFlow.Current(restored)!.Fellowship!;
        Assert.True(procedure.AwaitingHunts);
        Assert.Equal(new[] { "moria", "dol-guldur" }, procedure.AdditionalHuntRegions);
        Assert.Equal(payload.Route, procedure.Route);
        Assert.Null(restored.Pending);
        Assert.Equal(DieLocation.Resolving, restored.Dice[0].Location);
        Assert.Equal(0, restored.Fellowship.Progress);
        // T15 preserves obligations. Drawing/damage/cost completion are tested with T16, not simulated here.
    }

    [Fact]
    public void HiddenStateAndPhaseAreRequiredForDeclarationAndHideSpendsWithoutHuntMovement()
    {
        var state = Phase();
        state.Fellowship.Hidden = false;
        Reject(state, CommandKind.DeclareFellowship, new() { Route = ["rivendell"] });
        state = TurnTests.Actions();
        state.Fellowship.Hidden = false;
        state.Fellowship.Progress = 3;
        state = Do(state, CommandKind.HideFellowship, new() { DieId = state.Dice[0].Id });
        Assert.True(state.Fellowship.Hidden);
        Assert.Equal(3, state.Fellowship.Progress);
        Assert.Equal("rivendell", state.Fellowship.RegionId);
        Assert.Equal(DieLocation.Spent, state.Dice[0].Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void PhaseGuideMustShareHighestLevelAndItsChoiceEndsTheFellowshipPhase()
    {
        var state = Phase();
        var candidates = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.SelectGuide).EligibleIds;
        Assert.Equal(new[] { "base.character.gandalf-grey", "base.character.strider" }, candidates);
        Reject(state, CommandKind.SelectGuide, new() { GuideId = "base.character.gimli" });
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = "base.character.strider" });
        Assert.Equal("base.character.strider", state.Fellowship.Guide);
        Assert.Equal(GamePhase.HuntAllocation, state.Phase);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void NoPlannedRouteOrPrivateOptionsAreSentToShadow()
    {
        var state = Phase("rivendell", 5);
        var first = JsonSerializer.Serialize(Views.For(state, Side.Shadow), BoardCatalog.Json);
        var command = Command(state, CommandKind.DeclareFellowship, new() { Route = ["rivendell", "fords-of-bruinen", "hollin"] });
        Assert.True(new PreviewService(Engine).Preview(state, Side.FreePeoples, command).Legal);
        Assert.Equal(first, JsonSerializer.Serialize(Views.For(state, Side.Shadow), BoardCatalog.Json));
        Assert.Empty(Views.For(state, Side.Shadow).OwnCompanions);
        Assert.Equal(7, Views.For(state, Side.FreePeoples).OwnCompanions.Count);
        Assert.Equal(new[] { "gandalf", "aragorn", "boromir", "gimli", "legolas", "merry", "pippin" }, Views.For(state, Side.Shadow).Fellowship.CompanionIdentities);
    }
}
