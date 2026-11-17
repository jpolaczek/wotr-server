using System.Diagnostics;
using Npgsql;
using Wotr.Contracts;

namespace Wotr.Server;

// Durable polling also recovers work without any player opening the match.
public sealed class BotScheduler(BotHostOptions options, BotRuntimeCapabilities capabilities,
    BotJobStore jobs, BotInputStore inputs, RoomStore rooms, MatchStore matches, RevisionHub hub,
    ILogger<BotScheduler> logger, BotHostMetrics metrics, ProtocolSeatHost protocol, Database database) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stopping) => !options.Enabled
        ? Task.CompletedTask : Task.WhenAll(Enumerable.Range(0, options.Workers).Select(_ => Worker(stopping)).Append(Sweep(stopping)));

    // BOT-225: a protocol game that ends on the other side's move gets no further CPU work (a finished match is not
    // claimed), so its sessions are closed here: the bot hears game_over and the sidecar's slot is free again.
    private async Task Sweep(CancellationToken stopping)
    {
        var interval = TimeSpan.FromMilliseconds(Math.Max(1000, options.Poll.TotalMilliseconds * 4));
        try
        {
            while (!stopping.IsCancellationRequested)
            {
                await Task.Delay(interval, stopping);
                var open = protocol.OpenMatches();
                if (open.Count == 0)
                    continue;
                try
                {
                    var ended = new List<(string Match, Side? Winner, VictoryCondition? Victory, int Turn)>();
                    await using (var connection = await database.Source.OpenConnectionAsync(stopping))
                    await using (var query = Database.Command(connection, null,
                        "SELECT id, state->>'winner', state->>'victory', COALESCE((state->>'turn')::int, 0) " +
                        "FROM matches WHERE id = ANY($1) AND state->>'winner' IS NOT NULL", (object)open.ToArray()))
                    await using (var row = await query.ExecuteReaderAsync(stopping))
                        while (await row.ReadAsync(stopping))
                            ended.Add((row.GetString(0),
                                Enum.TryParse<Side>(row.IsDBNull(1) ? "" : row.GetString(1), out var side) ? side : null,
                                Enum.TryParse<VictoryCondition>(row.IsDBNull(2) ? "" : row.GetString(2), out var how) ? how : null,
                                row.GetInt32(3)));
                    foreach (var game in ended)
                        await protocol.Ended(game.Match, game.Winner, game.Victory, game.Turn, stopping);
                    protocol.ExpireIdle();
                }
                catch (NpgsqlException)
                {
                    logger.LogWarning("The protocol seat's sweep of ended games will retry.");
                }
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }

    private async Task Worker(CancellationToken stopping)
    {
        var runtime = EmbeddedBotRuntime.LoadConfigured(options);
        var owner = Guid.NewGuid().ToString("N");
        Task<BotRuntimeResponse>? evaluation = null;
        try
        {
            if (!runtime.GameplayReady)
            {
                metrics.Outcome(runtime.Status == "Loaded" ? "PolicyUnavailable" : runtime.Status);
                return;
            }
            while (!stopping.IsCancellationRequested)
            {
                BotWork? work = null;
                try
                {
                    if (!metrics.CanEvaluate())
                    {
                        metrics.Outcome("MemoryPressure");
                        logger.LogInformation("Embedded bot work waits for memory.");
                        await Task.Delay(options.Retry, stopping);
                        continue;
                    }
                    work = await jobs.Claim(owner, stopping);
                    if (work is null)
                    {
                        await Task.Delay(options.Poll, stopping);
                        continue;
                    }
                    var error = await Run(work, runtime, task => evaluation = task, stopping);
                    if (error.Length > 0)
                    {
                        metrics.Outcome(error);
                        logger.LogInformation("Embedded bot work deferred: {Outcome}.", error);
                    }
                    await Release(work, error, stopping);
                }
                catch (BotWorkExpiredException) { metrics.Outcome("StaleWork"); }
                catch (BotRuntimeDrainingException) { metrics.Outcome("Drained"); }
                catch (BotContextTooLargeException)
                {
                    metrics.Outcome("ContextTooLarge");
                    if (work is not null)
                        await Release(work, "ContextTooLarge", stopping);
                }
                catch (Exception error) when (error is NpgsqlException or InvalidOperationException or ArgumentException)
                {
                    // Never log raw exceptions/requests from a private evaluation or SQL parameters.
                    logger.LogWarning("Embedded bot work deferred after a host failure.");
                    metrics.Outcome("HostFailure");
                    if (work is not null)
                        await Release(work, "HostFailure", stopping);
                    await Task.Delay(options.Retry, stopping);
                }
                // Also retained when journal/release failed. No unbounded replacement computations.
                if (evaluation is { IsCompleted: false })
                    await evaluation.WaitAsync(stopping);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
        finally
        {
            if (evaluation is { IsCompleted: false })
                _ = evaluation.ContinueWith(_ => runtime.Dispose(), CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            else
                runtime.Dispose();
        }
    }

    private async Task KeepLease(BotWork work, CancellationToken cancellation)
    {
        var every = TimeSpan.FromSeconds(Math.Max(1, options.Lease.TotalSeconds / 3));
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                await Task.Delay(every, cancellation);
                if (!await jobs.Renew(work, cancellation))
                    return;
            }
        }
        catch (OperationCanceledException) { }
        catch (NpgsqlException)
        {
            logger.LogWarning("The protocol seat's lease renewal failed; the work may be taken again after its lease.");
        }
    }

    private async Task Release(BotWork work, string error, CancellationToken cancellation)
    {
        // BOT-224: a protocol seat's failed decision is asked again after a growing interval while the failure lasts.
        var delay = error.StartsWith("Protocol", StringComparison.Ordinal) ? protocol.RetryDelay(work.MatchId, work.Side) : (TimeSpan?)null;
        try { await jobs.Release(work, error, cancellation, delay); }
        catch (NpgsqlException)
        {
            logger.LogWarning("Embedded bot lease will recover after expiry.");
        }
    }

    private async Task<string> Run(BotWork work, EmbeddedBotRuntime runtime,
        Action<Task<BotRuntimeResponse>> observe, CancellationToken stopping)
    {
        if (work.MatchId.Length == 0)
            return await rooms.ReadyRuntime(work, stopping) ? "" : "NotReady";
        var pending = await jobs.Pending(work, stopping);
        var input = await inputs.Read(work, stopping, includeContext: pending is null);
        if (input is null)
            throw new BotWorkExpiredException();
        var timeout = options.Timeout(work.ThinkingMode);
        var request = input.Request with { MaximumWork = options.Work(work.ThinkingMode), ThinkingMode = work.ThinkingMode };
        if (work.Compatibility is null || work.Compatibility != BotRuntimeIdentity.Of(runtime.Description))
            return "MemoryIncompatible";
        if (capabilities.Unavailable(request.Player.Configuration, work.PolicyVersion, work.RuntimeVersion) is { } unavailable)
            return unavailable;
        // Re-submit an exact journal entry before asking for any new decision, even after a turn changed.
        if (pending is not null)
            return await Apply(work, pending, stopping);
        if (request.Player.Winner is not null)
            await protocol.Finished(request.Player, stopping);
        if (request.Player.Winner is not null || request.Player.DecisionOwner != work.Side || request.Player.Decision is null)
            return "";
        // BOT-225: a base game's CPU seat plays through the protocol sidecar (AlphaWOTR). Owner, 2026-11-17: the scripted
        // runtime never plays in its place. When the seat cannot answer, the decision is released with its error (the
        // seat shows as unavailable, the operator's log has the error) and the same decision is asked again until AlphaWOTR
        // answers it: the game waits, it is never played by another engine.
        if (work.Engine == ProtocolSeatHost.AlphaWotr)
        {
            // BOT-226: AlphaWOTR's seat waits while the operator's sidecar is switched off or the game is not a base game.
            if (!protocol.Enabled || !protocol.Handles(request.Player))
            {
                logger.LogError("AlphaWOTR's seat in match {Match} cannot play: {Reason}; the game waits.", work.MatchId,
                    protocol.Enabled ? "not a base game" : "the protocol seat is off");
                var outcome = protocol.Enabled ? "ProtocolConfiguration" : "ProtocolDisabled";
                protocol.Unavailable(work.MatchId, work.Side, outcome);
                return outcome;
            }
            var state = await inputs.ReadState(work, stopping) ?? throw new BotWorkExpiredException();
            // The lease is kept while AlphaWOTR thinks (a decision of several questions outlasts it): another worker must
            // not take the same game meanwhile.
            ProtocolSeatResult result;
            using (var thinking = CancellationTokenSource.CreateLinkedTokenSource(stopping))
            {
                var renewal = KeepLease(work, thinking.Token);
                try { result = await protocol.Decide(request.Player, state, request.InvocationId, stopping); }
                finally
                {
                    thinking.Cancel();
                    await renewal;
                }
            }
            if (result.Outcome == "Stale")
                throw new BotWorkExpiredException();
            if (result.Command is not { } chosen)
            {
                logger.LogError("AlphaWOTR could not answer {Kind} at revision {Revision} of match {Match} ({Outcome}); the game waits.",
                    request.Player.Decision.Kind, request.Player.Revision, work.MatchId, result.Outcome);
                return "Protocol" + result.Outcome;
            }
            metrics.Outcome("Protocol" + result.Outcome);
            await jobs.StageProtocol(work, request, chosen, input.HistoryPosition, stopping);
            var applied = await Apply(work, chosen, stopping);
            if (applied.Length == 0)
            {
                protocol.Recovered(work.MatchId, work.Side);
                await protocol.Applied(request.Player, chosen, stopping);
            }
            return applied;
        }
        var compute = TimeSpan.Zero;
        metrics.QueueWait(work.QueueSeconds);
        for (var round = 0; round < 3; round++)
        {
            if (!metrics.CanEvaluate())
                return "MemoryPressure";
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(stopping);
            var remaining = timeout - compute;
            if (remaining <= TimeSpan.Zero)
                return "RuntimeTimeout";
            cancel.CancelAfter(remaining);
            // The runtime's deadline is what the host still grants (BOT-37, ABI 3).
            request = request with { DeadlineMilliseconds = (int)Math.Clamp(remaining.TotalMilliseconds, 50, 15_000) };
            var token = cancel.Token;
            var started = Stopwatch.GetTimestamp();
            var evaluation = Task.Run(() =>
            {
                using var measurement = metrics.Evaluate();
                return runtime.Decide(request, token);
            }, CancellationToken.None);
            observe(evaluation);
            BotRuntimeResponse result;
            try { result = await evaluation.WaitAsync(cancel.Token); }
            catch (OperationCanceledException) when (!stopping.IsCancellationRequested)
            {
                logger.LogInformation("Embedded bot decision at revision {Revision} timed out.", request.Player.Revision);
                return "RuntimeTimeout";
            }
            compute += Stopwatch.GetElapsedTime(started);
            if (compute > timeout || result.Reason == "Cancelled")
            {
                logger.LogInformation("Embedded bot decision at revision {Revision} timed out.", request.Player.Revision);
                return "RuntimeTimeout";
            }
            if (result.Status == "NeedPreviews")
            {
                if (round == 2)
                    return "PreviewLimit";
                request = await inputs.AddPreviews(work, request, result, stopping) ?? throw new BotWorkExpiredException();
                continue;
            }
            if (result.Status != "Proposal")
            {
                // BOT-20: the runtime's own status and reason name a refusal without exposing its evaluation; the host
                // retries the decision, and without them a CPU that pauses for minutes leaves no trace.
                logger.LogInformation("Embedded bot refused a decision at revision {Revision}: {Status} {Reason}.",
                    request.Player.Revision, result.Status, result.Reason);
                return "RuntimeRefused";
            }
            await jobs.Stage(work, request, result, input.HistoryPosition, stopping);
            return await Apply(work, result.Command!, stopping);
        }
        return "PreviewLimit";
    }

    private async Task<string> Apply(BotWork work, Wotr.Contracts.GameCommand command, CancellationToken cancellation)
    {
        var receipt = await matches.Execute(command, work.ParticipantId, cancellation, runtimeWork: work);
        if (receipt?.Accepted is not true)
            return "CommandRejected";
        metrics.Outcome("Accepted");
        if (receipt.PublicStateChanged)
            hub.Publish(work.MatchId, receipt.Revision);
        else if (receipt.AutoDecision is not null)
            hub.Publish(work.MatchId, receipt.Revision, work.ParticipantId);
        return "";
    }
}
