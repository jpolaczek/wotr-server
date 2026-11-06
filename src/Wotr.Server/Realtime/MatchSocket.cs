using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Server;

public sealed class MatchSocket(MatchStore store, RevisionHub hub, ISessionResolver sessions, UpdateSocketBudget budget,
    RankedMatches ranked, MatchPresence presence)
{
    private sealed record UpdateStream(string Match, string Account, bool Observing, Side? Seat);

    // OBS-02: observing opens the same update stream for an observer of the match's room.
    public async Task Run(string id, HttpContext context, bool observing = false)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }
        var account = MatchEndpoints.Account(context);
        if (await Version(id, account, observing, context.RequestAborted) is null)
        {
            context.Response.StatusCode = 404;
            return;
        }
        // QUAL-02: an account keeps a bounded number of update sockets; the slot returns however the socket ends.
        // RANK-10: a seated ranked player always gets one, and that socket must keep talking.
        var seat = observing ? null : await ranked.Seat(id, account, context.RequestAborted);
        var slot = budget.TryTake(account, evictOldest: seat is not null);
        if (slot is null)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = "5";
            return;
        }
        try
        {
            await Serve(new(id, account, observing, seat), slot, context);
        }
        finally
        {
            budget.Release(account, slot);
        }
    }

    private async Task Serve(UpdateStream stream, UpdateSocketSlot slot, HttpContext context)
    {
        var subscription = hub.Subscribe(stream.Match, stream.Account);
        try
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            using var seat = stream.Seat is { } side ? presence.Join(stream.Match, side) : null;
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, slot.Evicted);
            var receive = ReceiveUntilClose(socket, lifetime, seat);
            try
            {
                await SendUpdates(stream, context.Request.Headers.Authorization.ToString()[7..],
                    socket, subscription.Channel.Reader, lifetime.Token);
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            catch (Exception error) when (DatabaseOverloadHandling.IsTransient(error))
            {
                await CloseBusy(socket);
            }
            finally
            {
                await lifetime.CancelAsync();
                try
                {
                    await receive;
                }
                catch (OperationCanceledException) { }
                catch (WebSocketException) { }
                socket.Abort();
            }
        }
        finally { hub.Remove(stream.Match, subscription.Id); }
    }

    // QUAL-05: a busy database closes the socket with 1013 (try again later), so the client reconnects and
    // refreshes instead of seeing a silent drop.
    public const int TryAgainLater = 1013;

    private static async Task CloseBusy(WebSocket socket)
    {
        DatabaseOverloadHandling.Count();
        if (socket.State != WebSocketState.Open)
            return;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await socket.CloseOutputAsync((WebSocketCloseStatus)TryAgainLater, DatabaseOverloadHandling.ServerBusy, timeout.Token);
        }
        catch (Exception error) when (error is WebSocketException or OperationCanceledException) { }
    }

    private async Task SendUpdates(UpdateStream stream, string token, WebSocket socket,
        ChannelReader<long> updates, CancellationToken cancellation)
    {
        // Reading after subscribing closes the read/subscribe race; refresh recovers dropped notifications.
        while (!cancellation.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            if (await sessions.Resolve(token, cancellation) is null)
                return;
            var version = await Version(stream.Match, stream.Account, stream.Observing, cancellation);
            if (version is null)
                return;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                matchId = stream.Match,
                revision = version.Revision,
                privateVersion = version.PrivateVersion,
                clockVersion = version.ClockVersion,
                opponentOnline = OpponentOnline(stream)
            });
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellation);
            using var tick = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            tick.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await updates.ReadAsync(tick.Token);
            }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { }
        }
    }

    // An observer has no private version. A lapsed or removed observer gets nothing, so the loop ends and the socket
    // closes within one refresh, as for a deleted match.
    private async Task<MatchVersion?> Version(string id, string account, bool observing, CancellationToken cancellation)
    {
        if (!observing)
            return await store.ReadVersion(id, account, cancellation);
        return await store.ReadObservedRevision(id, account, cancellation) is { } revision ? new(revision, 0) : null;
    }

    // RANK-10: known for a seat of a ranked match, and read afresh for every notice: at most 5 s old.
    private bool? OpponentOnline(UpdateStream stream) =>
        stream.Seat is { } side ? presence.Online(stream.Match, TurnFlow.Other(side)) : null;

    // RANK-10: a seated player's client sends a frame every 2 s, of any kind and content. A socket silent for the limit
    // is closed, which gives its slot back. A dead network shows no close, and normal matches need no frames.
    private static async Task ReceiveUntilClose(WebSocket socket, CancellationTokenSource lifetime, MatchPresence.SeatSocket? seat)
    {
        var buffer = new byte[1024];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                using var silence = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                if (seat is not null)
                    silence.CancelAfter(seat.Silence);
                var read = await socket.ReceiveAsync(buffer, silence.Token);
                if (read.MessageType == WebSocketMessageType.Close)
                    break;
                seat?.Heard();
            }
        }
        finally { await lifetime.CancelAsync(); }
    }
}
