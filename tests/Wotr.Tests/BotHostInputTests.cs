using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotHostInputTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, BoardCatalog.Json);

    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public async Task AllSurfacesMatchOrdinaryPlayerServicesAndHiddenPermutationsDoNotChangeInput(Side side)
    {
        await using var fixture = await BotHostFixture.Create();
        var state = BattleFixture.State(side);
        var attacker = BattleFixture.Put(state, side == Side.FreePeoples ? "gondor" : "sauron", UnitKind.Regular, BattleFixture.Origin);
        BattleFixture.Put(state, side == Side.FreePeoples ? "sauron" : "gondor", UnitKind.Regular, BattleFixture.Target);
        state = BattleFixture.Begin(state, [attacker.Id]);
        state.Events.Add(new GameEvent { HistoryId = "fixture-private-event", Kind = "Fixture", Revision = state.Revision,
            OnlyFor = TurnFlow.Other(side), Text = "original-opponent-secret" });
        var (_, _, work) = await fixture.Game(side, state);
        var first = (await fixture.Inputs.Read(work))!;
        var ordinary = (await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State;
        Assert.Equal(Json(fixture.Host.Services.GetRequiredService<Projection>().For(ordinary, side)), Json(first.Request.Player));
        var log = (await fixture.Host.Services.GetRequiredService<ActionLogStore>().Page(work.MatchId, work.ParticipantId, ActionLogFilter.All, 50))!;
        Assert.NotEmpty(first.Request.History);
        Assert.Equal(Json(log.Steps.OrderBy(s => s.Id)), Json(first.Request.History.OrderBy(s => s.Id)));
        Assert.NotEmpty(first.Request.Reports);
        foreach (var report in first.Request.Reports)
            Assert.Equal(Json(await fixture.Host.Services.GetRequiredService<BattleReportStore>().Read(work.MatchId, report.Id, work.ParticipantId)), Json(report));
        var query = KernelTests.Command(state, CommandKind.CommitCombatCard);
        var response = new BotRuntimeResponse(2, first.Request.InvocationId, "NeedPreviews", "", ["base"], PreviewRequests: [query]);
        var withPreview = (await fixture.Inputs.AddPreviews(work, first.Request, response))!;
        Assert.Equal(Json(fixture.Host.Services.GetRequiredService<PreviewService>().Preview(state, side, query)), Json(withPreview.Previews.Single()));
        Assert.Equal(Json(query), Json(withPreview.PreviewCommands!.Single()));
        // Opponent order and own-side-invisible event content are not observable facts.
        var hidden = StateCodec.Copy(state);
        hidden.Player(TurnFlow.Other(side)).Hand.Reverse();
        foreach (var deck in hidden.Player(TurnFlow.Other(side)).Decks)
            deck.Value.Reverse();
        foreach (var step in hidden.Events.Where(e => e.OnlyFor == TurnFlow.Other(side)))
            step.Text = "hidden-permutation-secret";
        Assert.NotEqual(StateCodec.Write(state), StateCodec.Write(hidden));
        await fixture.Replace(work.MatchId, hidden);
        var second = (await fixture.Inputs.Read(work))!;
        Assert.Equal(Json(first.Request with { InvocationId = "", SampleSeed = 0 }),
            Json(second.Request with { InvocationId = "", SampleSeed = 0 }));
        Assert.DoesNotContain("hidden-permutation-secret", Json(second.Request));
    }

    [PostgresFact]
    public async Task HistoryOverflowFailsExplicitlyAndPreviewRevisionCannotDrift()
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, _, work) = await fixture.Game();
        var first = (await fixture.Inputs.Read(work))!;
        var proposal = fixture.Runtime.Decide(first.Request);
        await fixture.Jobs.Stage(work, first.Request, proposal, first.HistoryPosition);
        Assert.True((await fixture.Matches.Execute(proposal.Command!, work.ParticipantId, runtimeWork: work))!.Accepted);
        var request = new BotRuntimeResponse(2, first.Request.InvocationId, "NeedPreviews", "", ["base"], PreviewRequests: [proposal.Command!]);
        Assert.Null(await fixture.Inputs.AddPreviews(work, first.Request, request));
        var state = (await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State;
        state.Open(DecisionKind.FellowshipPhase, work.Side, [CommandKind.ContinueFellowship]);
        state.Events.AddRange(Enumerable.Range(0, 4097).Select(i => new GameEvent { Kind = "Fixture", Revision = state.Revision, Text = i.ToString() }));
        await fixture.Replace(work.MatchId, state);
        await fixture.Expire(work);
        var fresh = (await fixture.Jobs.Claim("overflow"))!;
        await Assert.ThrowsAsync<BotContextTooLargeException>(() => fixture.Inputs.Read(fresh));
        Assert.Equal(1L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
    }
}
