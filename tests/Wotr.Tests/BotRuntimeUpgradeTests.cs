using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotRuntimeUpgradeTests
{
    [PostgresTheory]
    [InlineData(Side.FreePeoples, false)]
    [InlineData(Side.Shadow, true)]
    public async Task InstalledUpgradeAndRollbackPreserveAcceptedMemoryAndExactPendingDecision(Side side, bool rollback)
    {
        using var previous = new BotPackageFixture();
        using var next = new BotPackageFixture(upgrade: true);
        await using var fixture = await BotHostFixture.Create(previous.Settings());
        var (_, _, work) = await fixture.Game(side, NestedReactions(side));
        var (_, first) = await fixture.Stage(work);
        Assert.True((await fixture.Matches.Execute(first.Command!, work.ParticipantId, runtimeWork: work))!.Accepted);
        await fixture.Expire(work);
        work = (await fixture.Jobs.Claim("pending-upgrade"))!;
        var (_, pending) = await fixture.Stage(work);
        var before = await Saved(fixture);
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        var drained = await previous.Operator(fixture.Connection, "drain");
        Assert.Equal(0, drained.ExitCode);
        var epoch = (await control.Status()).Epoch;
        Assert.Equal(0, (await next.Operator(fixture.Connection, "inspect")).ExitCode);
        var upgraded = await next.Operator(fixture.Connection, "upgrade", epoch.ToString(), "0.2.0");
        Assert.Equal(0, upgraded.ExitCode);
        Assert.Equal("0.2.1", await fixture.Scalar("SELECT runtime_version FROM bot_assignments"));
        Assert.Equal(before, await Saved(fixture));
        // Restart with the installed target while the durable drain is still closed.
        await using (var target = await KestrelProcess.Start(fixture.Connection, next.Settings(fixture.Settings)))
        {
            Assert.True((await control.Status()).Draining);
            Assert.Equal(before, await Saved(fixture));
            Assert.Equal(1, (await previous.Operator(fixture.Connection, "resume", epoch.ToString())).ExitCode);
            if (!rollback)
            {
                Assert.Equal(0, (await next.Operator(fixture.Connection, "resume", epoch.ToString())).ExitCode);
                await fixture.Until("SELECT memory_version FROM bot_runtime_jobs", 2L);
            }
            await target.StopGracefully();
        }
        if (rollback)
        {
            epoch = await control.Drain();
            Assert.Equal(0, (await previous.Operator(fixture.Connection, "upgrade", epoch.ToString(), "0.2.1")).ExitCode);
            Assert.Equal(before, await Saved(fixture));
            await using var restored = await KestrelProcess.Start(fixture.Connection, previous.Settings(fixture.Settings));
            Assert.Equal(0, (await previous.Operator(fixture.Connection, "resume", epoch.ToString())).ExitCode);
            await fixture.Until("SELECT memory_version FROM bot_runtime_jobs", 2L);
        }
        Assert.True((await fixture.Matches.Receipt(work.MatchId, pending.Command!.CommandId, work.ParticipantId))!.Accepted);
        Assert.Equal(pending.ProposedMemory, await fixture.Scalar("SELECT memory FROM bot_runtime_jobs"));
        Assert.Equal(2L, await fixture.Scalar("SELECT count(*) FROM command_receipts WHERE accepted"));
        Assert.Equal("accepted", await fixture.Scalar("SELECT status FROM bot_runtime_proposals"));
        Assert.Equal(TurnFlow.Other(side), (await fixture.Matches.Read(work.MatchId, work.ParticipantId))!.State.Pending!.Owner);
        // Old work cannot submit again, and an old host cannot claim the upgraded assignment.
        var stale = await Record.ExceptionAsync(() => fixture.Matches.Execute(pending.Command, work.ParticipantId, runtimeWork: work));
        Assert.True(stale is BotWorkExpiredException or BotRuntimeDrainingException);
        if (!rollback)
            Assert.Null(await fixture.Jobs.Claim("obsolete-server"));
    }

    [PostgresTheory]
    [InlineData("abi")]
    [InlineData("protocol")]
    [InlineData("rules")]
    [InlineData("content")]
    [InlineData("memory")]
    [InlineData("policy")]
    [InlineData("unready")]
    [InlineData("legacy")]
    [InlineData("mixed")]
    [InlineData("modules")]
    public async Task IncompatibleTargetPreservesEverySavedByteAndLeavesHostDrained(string change)
    {
        using var previous = new BotPackageFixture();
        using var next = new BotPackageFixture(upgrade: true);
        await using var fixture = await BotHostFixture.Create(previous.Settings());
        var (_, _, work) = await fixture.Game();
        await fixture.Stage(work);
        var target = next.Package.Description;
        switch (change)
        {
            case "abi": target = target with { AbiVersion = 3 }; break;
            case "protocol": target = target with { ProtocolVersion = 3 }; break;
            case "rules": target = target with { RulesVersion = "future" }; break;
            case "content": target = target with { ContentVersion = "future" }; break;
            case "memory": target = target with { MemoryFormat = "fixture.memory/2" }; break;
            case "policy": target = target with { Policies = target.Policies.Select(p => p with { PolicyVersion = "fixture-2" }).ToArray() }; break;
            case "unready": target = target with { GameplayReady = false, Policies = [] }; break;
            case "legacy": await fixture.Execute("UPDATE bot_runtime_jobs SET compatibility=NULL"); break;
            case "mixed": await fixture.Execute("UPDATE bot_assignments SET runtime_version='0.1.0'"); break;
            case "modules": await fixture.Execute("UPDATE rooms SET state=jsonb_set(state,'{configuration,lords}','true'::jsonb)"); break;
        }
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        var epoch = await control.Drain();
        var before = await Saved(fixture);
        var pins = await fixture.Scalar("SELECT jsonb_agg(runtime_version)::text FROM bot_assignments");
        Assert.False((await control.Upgrade(epoch, "0.2.0", target, next.Hash)).Applied);
        Assert.Equal(before, await Saved(fixture));
        Assert.Equal(pins, await fixture.Scalar("SELECT jsonb_agg(runtime_version)::text FROM bot_assignments"));
        Assert.True((await control.Status()).Draining);
        Assert.Null(await fixture.Jobs.Claim("incompatible"));
        Assert.Equal(0L, await fixture.Scalar("SELECT count(*) FROM command_receipts"));
    }

    [PostgresFact]
    public async Task ReservedRoomUsesTheUpgradedAssignmentPinForReadinessAndStart()
    {
        using var previous = new BotPackageFixture();
        using var next = new BotPackageFixture(upgrade: true);
        await using var fixture = await BotHostFixture.Create(previous.Settings());
        var (human, room, old) = await fixture.Reserve();
        Assert.True(await fixture.Rooms.ReadyRuntime(old));
        await fixture.Rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        var epoch = await control.Drain();
        Assert.True((await control.Upgrade(epoch, "0.2.0", next.Package.Description, next.Hash)).Applied);
        Assert.True(await control.Resume(epoch, "0.2.1", next.Hash));
        Assert.False(await fixture.Rooms.ReadyRuntime(old));
        Assert.Equal("BotVersionUnsupported", (await fixture.Rooms.Start(room.Id, human.AccountId, room.Version)).Error);
        var options = next.Options(fixture.Settings);
        using var runtime = EmbeddedBotRuntime.LoadConfigured(options);
        var capabilities = new BotRuntimeCapabilities(options, fixture.Host.Services.GetRequiredService<BotRoomOptions>(),
            fixture.Host.Services.GetRequiredService<BotAccessOptions>(), runtime);
        var jobs = new BotJobStore(fixture.Database, options, fixture.Host.Services.GetRequiredService<EmptyRoomOptions>());
        var rooms = ActivatorUtilities.CreateInstance<RoomStore>(fixture.Host.Services, capabilities, options);
        var current = (await jobs.Claim("upgraded-reserved-seat"))!;
        Assert.True(await rooms.ReadyRuntime(current));
        Assert.Equal("", (await rooms.Start(room.Id, human.AccountId, room.Version)).Error);
        Assert.Equal("0.2.1", await fixture.Scalar("SELECT state->'bot'->>'runtimeVersion' FROM rooms WHERE id=$1", room.Id));
        Assert.Equal(1L, await fixture.Scalar("SELECT count(*) FROM matches"));
    }

    [PostgresFact]
    public async Task OneIncompatibleAssignmentRefusesTheWholeUpgradeWithoutPartialChanges()
    {
        using var previous = new BotPackageFixture();
        using var next = new BotPackageFixture(upgrade: true);
        await using var fixture = await BotHostFixture.Create(previous.Settings());
        var (_, _, first) = await fixture.Game();
        await fixture.Stage(first);
        await fixture.Execute("UPDATE bot_runtime_jobs SET lease_until='infinity' WHERE assignment_id=$1", first.AssignmentId);
        var (_, _, second) = await fixture.Reserve();
        await fixture.Execute("UPDATE bot_runtime_jobs SET compatibility=NULL WHERE assignment_id=$1", second.AssignmentId);
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        var epoch = await control.Drain();
        var before = await Saved(fixture);
        Assert.False((await control.Upgrade(epoch, "0.2.0", next.Package.Description, next.Hash)).Applied);
        Assert.Equal(before, await Saved(fixture));
        Assert.Equal(2L, await fixture.Scalar("SELECT count(*) FROM bot_assignments WHERE runtime_version='0.2.0'"));
        Assert.Equal(1L, await fixture.Scalar("SELECT count(*) FROM bot_runtime_proposals WHERE status='pending'"));
    }

    [PostgresFact]
    public async Task EpochAndPackagePinPreventSkippingUpgradeOrResumingAnUnreviewedBuild()
    {
        using var previous = new BotPackageFixture();
        using var next = new BotPackageFixture(upgrade: true);
        await using var fixture = await BotHostFixture.Create(previous.Settings());
        var (_, _, work) = await fixture.Game();
        await fixture.Stage(work);
        var control = fixture.Host.Services.GetRequiredService<BotRuntimeControl>();
        Assert.False((await control.Upgrade(0, "0.2.0", next.Package.Description, next.Hash)).Applied);
        var epoch = await control.Drain();
        Assert.False(await control.Resume(epoch, "0.2.1", next.Hash));
        Assert.False((await control.Upgrade(epoch - 1, "0.2.0", next.Package.Description, next.Hash)).Applied);
        Assert.True((await control.Upgrade(epoch, "0.2.0", next.Package.Description, next.Hash)).Applied);
        Assert.False(await control.Resume(epoch, "0.2.1", new string('0', 64)));
        Assert.True(await control.Resume(epoch, "0.2.1", next.Hash));
        var wrongOptions = next.Options(fixture.Settings) with { ManifestSha256 = new string('0', 64) };
        var wrongHost = new BotJobStore(fixture.Database, wrongOptions, fixture.Host.Services.GetRequiredService<EmptyRoomOptions>());
        Assert.Null(await wrongHost.Claim("wrong-package"));
        Assert.Equal(0L, await fixture.Scalar("SELECT memory_version FROM bot_runtime_jobs"));
    }

    private static MatchState NestedReactions(Side side)
    {
        var state = KernelTests.NewGame();
        state.Phase = GamePhase.ActionResolution;
        state.Open(DecisionKind.HuntDamage, TurnFlow.Other(side), []);
        for (var i = 0; i < 2; i++)
        {
            state.Resolution.Suspend(state, "upgrade-fixture", i);
            state.Open(DecisionKind.Reaction, side, [CommandKind.DeclineReaction]);
        }
        return state;
    }

    private static async Task<string> Saved(BotHostFixture fixture) => (string)(await fixture.Scalar("""
        SELECT jsonb_build_object('matches',(SELECT jsonb_agg(state ORDER BY id) FROM matches),
            'rooms',(SELECT jsonb_agg(state ORDER BY id) FROM rooms),
            'memory',(SELECT jsonb_agg(jsonb_build_array(assignment_id,memory,memory_version,history_position,accepted_revision) ORDER BY assignment_id) FROM bot_runtime_jobs),
            'proposals',(SELECT jsonb_agg(to_jsonb(p) ORDER BY assignment_id) FROM bot_runtime_proposals p))::text
        """))!;
}
