using System.Collections.Concurrent;
using System.Threading.Channels;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

// DEMO-03: plays the CPU's moves in demo matches. Anything that may hand a decision to the CPU wakes it: an accepted
// command, a new demo, or a read of the match (which resumes the CPU after a server restart). One loop runs per
// match; a wake during it makes it look once more. Each move is an ordinary command of the technical CPU account,
// with its receipt, history and action log, published to the player like a human opponent's move.
public sealed class DemoBotRunner(DemoSessionStore demos, MatchStore store, DemoBot bot, RevisionHub hub, DemoOptions options,
    ILogger<DemoBotRunner> logger) : BackgroundService
{
    private readonly Channel<string> wakes = Channel.CreateUnbounded<string>();
    private readonly ConcurrentDictionary<string, Side?> sides = new();
    private readonly Dictionary<string, bool> running = [];
    private readonly object gate = new();

    public void Wake(string matchId) => wakes.Writer.TryWrite(matchId);

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        await foreach (var match in wakes.Reader.ReadAllAsync(stopping))
            if (Begin(match))
                _ = Play(match, stopping);
    }

    // True when no loop runs for the match yet; otherwise the running loop is asked to look again.
    private bool Begin(string match)
    {
        lock (gate)
        {
            if (running.ContainsKey(match))
            {
                running[match] = true;
                return false;
            }
            running[match] = false;
            return true;
        }
    }

    private bool Again(string match)
    {
        lock (gate)
        {
            if (running[match])
            {
                running[match] = false;
                return true;
            }
            running.Remove(match);
            return false;
        }
    }

    private async Task Play(string match, CancellationToken stopping)
    {
        try
        {
            do
                await Moves(match, stopping);
            while (Again(match));
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            logger.LogError(error, "The demo CPU stopped in match {Match}.", match);
            lock (gate)
                running.Remove(match);
        }
    }

    private async Task Moves(string match, CancellationToken stopping)
    {
        if (await CpuSide(match, stopping) is not { } side)
            return;
        for (var step = 0; step < options.StepLimit; step++)
        {
            var current = await store.Read(match, DemoSessionStore.CpuAccount, stopping);
            if (current is null)
                return;
            var command = bot.Next(current.State, side);
            if (command is null)
            {
                WarnIfStuck(match, current.State, side);
                return;
            }
            await Task.Delay(options.StepDelay, stopping);
            command.CommandId = "demo-cpu-" + Guid.NewGuid().ToString("N");
            var receipt = await store.Execute(command, DemoSessionStore.CpuAccount, stopping);
            if (receipt is null)
                return;
            if (receipt.Accepted && receipt.PublicStateChanged)
                hub.Publish(match, receipt.Revision);
        }
        logger.LogWarning("The demo CPU made {Steps} moves in match {Match} without handing over.", options.StepLimit, match);
    }

    // Whether a match is a demo never changes; a removed demo simply has nothing left to read.
    private async Task<Side?> CpuSide(string match, CancellationToken stopping)
    {
        if (sides.TryGetValue(match, out var known))
            return known;
        var side = await demos.CpuSide(match, stopping);
        sides[match] = side;
        return side;
    }

    private void WarnIfStuck(string match, MatchState state, Side side)
    {
        if (state.Winner is null && state.Pending?.Owner == side)
            logger.LogWarning("The demo CPU has no move for {Decision} in match {Match}.", state.Pending.Kind, match);
    }
}
