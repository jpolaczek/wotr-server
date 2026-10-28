using System.Diagnostics;
using Npgsql;

namespace Wotr.Server;

// Durable polling also recovers work without any player opening the match.
public sealed class BotScheduler(BotHostOptions options, BotRuntimeCapabilities capabilities,
    BotJobStore jobs, BotInputStore inputs, RoomStore rooms, MatchStore matches, RevisionHub hub,
    ILogger<BotScheduler> logger, BotHostMetrics metrics) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stopping) => !options.Enabled
        ? Task.CompletedTask : Task.WhenAll(Enumerable.Range(0, options.Workers).Select(_ => Worker(stopping)));

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
                        metrics.Outcome(error);
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

    private async Task Release(BotWork work, string error, CancellationToken cancellation)
    {
        try { await jobs.Release(work, error, cancellation); }
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
        var request = input.Request with { MaximumWork = options.MaximumWork / 3 };
        if (work.Compatibility is null || work.Compatibility != BotRuntimeIdentity.Of(runtime.Description))
            return "MemoryIncompatible";
        if (capabilities.Unavailable(request.Player.Configuration, work.PolicyVersion, work.RuntimeVersion) is { } unavailable)
            return unavailable;
        // Re-submit an exact journal entry before asking for any new decision, even after a turn changed.
        if (pending is not null)
            return await Apply(work, pending, stopping);
        if (request.Player.Winner is not null || request.Player.DecisionOwner != work.Side || request.Player.Decision is null)
            return "";
        var compute = TimeSpan.Zero;
        metrics.QueueWait(work.QueueSeconds);
        for (var round = 0; round < 3; round++)
        {
            if (!metrics.CanEvaluate())
                return "MemoryPressure";
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(stopping);
            var remaining = options.DecisionTimeout - compute;
            if (remaining <= TimeSpan.Zero)
                return "RuntimeTimeout";
            cancel.CancelAfter(remaining);
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
            catch (OperationCanceledException) when (!stopping.IsCancellationRequested) { return "RuntimeTimeout"; }
            compute += Stopwatch.GetElapsedTime(started);
            if (compute > options.DecisionTimeout || result.Reason == "Cancelled")
                return "RuntimeTimeout";
            if (result.Status == "NeedPreviews")
            {
                if (round == 2)
                    return "PreviewLimit";
                request = await inputs.AddPreviews(work, request, result, stopping) ?? throw new BotWorkExpiredException();
                continue;
            }
            if (result.Status != "Proposal")
                return "RuntimeRefused";
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
