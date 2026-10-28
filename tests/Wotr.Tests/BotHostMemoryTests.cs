using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotHostMemoryTests
{
    [PostgresTheory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public async Task OrdinaryAcceptedMoveAndMemoryCommitOnceWithExactReceipt(Side side)
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, room, work) = await fixture.Game(side);
        var (input, proposal) = await fixture.Stage(work, "preview");
        Assert.Single(input.Request.Previews);
        Assert.True(input.Request.Previews[0].Legal);
        var receipt = await fixture.Matches.Execute(proposal.Command!, work.ParticipantId, runtimeWork: work);
        Assert.True(receipt!.Accepted, receipt.Error);
        Assert.Equal(input.Request.Player.Revision + 1, receipt.Revision);
        Assert.Equal(1L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.Equal(proposal.ProposedMemory, await fixture.Scalar("SELECT memory FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.Equal("accepted", await fixture.Scalar("SELECT status FROM bot_runtime_proposals WHERE assignment_id=$1", work.AssignmentId));
        Assert.Null(await fixture.Jobs.Pending(work));
        Assert.True((await fixture.Matches.Receipt(room.MatchId, proposal.Command!.CommandId, work.ParticipantId))!.Accepted);
        await Assert.ThrowsAsync<BotWorkExpiredException>(() => fixture.Matches.Execute(proposal.Command, work.ParticipantId, runtimeWork: work));
        Assert.Equal(1L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        var saved = (await fixture.Matches.Read(room.MatchId, work.ParticipantId))!;
        Assert.DoesNotContain("fixture-private-memory", StateCodec.Write(saved.State));
    }

    [PostgresFact]
    public async Task FailureAfterGameReceiptAndMemoryWritesRollsBackAllThenRecoversExactCommand()
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, _, work) = await fixture.Game();
        var (input, proposal) = await fixture.Stage(work);
        await Assert.ThrowsAsync<IOException>(() => fixture.Matches.Execute(proposal.Command!, work.ParticipantId,
            beforeCommit: () => throw new IOException("injected crash"), runtimeWork: work));
        Assert.Equal(input.Request.Player.Revision, await fixture.Matches.ReadRevision(work.MatchId, work.ParticipantId, default));
        Assert.Null(await fixture.Matches.Receipt(work.MatchId, proposal.Command!.CommandId, work.ParticipantId));
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        await fixture.Expire(work);
        var recovered = (await fixture.Jobs.Claim("restarted"))!;
        var pending = (await fixture.Jobs.Pending(recovered))!;
        Assert.Equal(CommandFingerprint.Create(proposal.Command), CommandFingerprint.Create(pending));
        Assert.Equal(proposal.Command.CommandId, pending.CommandId);
        Assert.True((await fixture.Matches.Execute(pending, recovered.ParticipantId, runtimeWork: recovered))!.Accepted);
        Assert.Equal(1L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
    }

    [PostgresFact]
    public async Task FullRulesRejectIllegalPayloadWithoutAdvancingMemoryOrGame()
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, _, work) = await fixture.Game(Side.Shadow);
        var before = StateCodec.Write((await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State);
        var (_, proposal) = await fixture.Stage(work, "illegal");
        var receipt = (await fixture.Matches.Execute(proposal.Command!, work.ParticipantId, runtimeWork: work))!;
        Assert.False(receipt.Accepted);
        Assert.Equal(before, StateCodec.Write((await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State));
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.Equal("illegal", await fixture.Scalar("SELECT memory FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        Assert.Equal("rejected", await fixture.Scalar("SELECT status FROM bot_runtime_proposals WHERE assignment_id=$1", work.AssignmentId));
    }

    [PostgresFact]
    public async Task ExpiredGenerationCannotStageSubmitOrReleaseNewOwnersWork()
    {
        await using var fixture = await BotHostFixture.Create();
        var (_, _, old) = await fixture.Game();
        var input = (await fixture.Inputs.Read(old))!;
        var proposal = fixture.Runtime.Decide(input.Request);
        Assert.Null(await fixture.Jobs.Claim("contender"));
        await fixture.Expire(old);
        var current = (await fixture.Jobs.Claim("contender"))!;
        await Assert.ThrowsAsync<BotWorkExpiredException>(() => fixture.Jobs.Stage(old, input.Request, proposal, input.HistoryPosition));
        await fixture.Jobs.Stage(current, input.Request, proposal, input.HistoryPosition);
        await Assert.ThrowsAsync<BotWorkExpiredException>(() => fixture.Matches.Execute(proposal.Command!, old.ParticipantId, runtimeWork: old));
        await fixture.Jobs.Release(old, "RuntimeTimeout");
        Assert.Equal(current.OwnerId, await fixture.Scalar("SELECT owner_id FROM bot_runtime_jobs WHERE assignment_id=$1", current.AssignmentId));
        Assert.True((await fixture.Matches.Execute(proposal.Command!, current.ParticipantId, runtimeWork: current))!.Accepted);
    }

    [PostgresFact]
    public async Task StaleJournalIsRejectedAndAcceptedHistoryCursorDoesNotSkipTheBotsOwnMove()
    {
        await using var fixture = await BotHostFixture.Create();
        var (human, _, work) = await fixture.Game();
        var (input, proposal) = await fixture.Stage(work);
        // An ordinary already-committed move makes the captured proposal stale.
        var concurrent = KernelTests.Command((await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State, CommandKind.ContinueFellowship);
        Assert.True((await fixture.Matches.Execute(concurrent, work.ParticipantId))!.Accepted);
        var rejected = (await fixture.Matches.Execute(proposal.Command!, work.ParticipantId, runtimeWork: work))!;
        Assert.False(rejected.Accepted);
        Assert.Equal("StaleRevision", rejected.Error);
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
        var humanState = (await fixture.Matches.Read(work.MatchId, human.AccountId))!.State;
        Assert.True((await fixture.Matches.Execute(KernelTests.Command(humanState, CommandKind.AllocateHunt), human.AccountId))!.Accepted);
        await fixture.Expire(work);
        var fresh = (await fixture.Jobs.Claim("fresh-observation"))!;
        var observation = (await fixture.Inputs.Read(fresh))!.Request;
        Assert.Null(fresh.Memory);
        Assert.Contains(observation.History, step => step.Revision == input.Request.Player.Revision + 1);
        Assert.Contains(observation.History, step => step.Revision == input.Request.Player.Revision + 2);
    }

    [PostgresFact]
    public async Task NestedReactionRestoresTheHumanDecisionAndKeepsMemoryPrivate()
    {
        await using var fixture = await BotHostFixture.Create();
        var state = KernelTests.NewGame();
        state.Phase = GamePhase.ActionResolution;
        state.Open(DecisionKind.HuntDamage, Side.Shadow, []);
        var humanDecision = state.Pending!.Id;
        state.Resolution.Suspend(state, "hunt-damage", 2);
        state.Open(DecisionKind.Reaction, Side.FreePeoples, [CommandKind.DeclineReaction]);
        var (human, _, work) = await fixture.Game(seed: state);
        var (_, proposal) = await fixture.Stage(work);
        Assert.Equal(CommandKind.DeclineReaction, proposal.Command!.Kind);
        Assert.True((await fixture.Matches.Execute(proposal.Command, work.ParticipantId, runtimeWork: work))!.Accepted);
        var after = (await fixture.Matches.Read(work.MatchId, human.AccountId))!.State;
        Assert.Equal(humanDecision, after.Pending!.Id);
        Assert.Equal(Side.Shadow, after.Pending.Owner);
        Assert.Equal(1L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs WHERE assignment_id=$1", work.AssignmentId));
    }
}
