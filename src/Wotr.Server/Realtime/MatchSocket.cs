using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;

namespace Wotr.Server;

public sealed class MatchSocket(MatchStore store, RevisionHub hub, ISessionResolver sessions, UpdateSocketBudget budget)
{
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
        if (!budget.TryTake(account))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = "5";
            return;
        }
        try
        {
            await Serve(id, account, observing, context);
        }
        finally
        {
            budget.Release(account);
        }
    }

    private async Task Serve(string id, string account, bool observing, HttpContext context)
    {
        var subscription = hub.Subscribe(id, account);
        try
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            var receive = ReceiveUntilClose(socket, lifetime);
            try
            {
                await SendUpdates(id, account, observing, context.Request.Headers.Authorization.ToString()[7..],
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
        finally { hub.Remove(id, subscription.Id); }
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

    private async Task SendUpdates(string id, string account, bool observing, string token, WebSocket socket,
        ChannelReader<long> updates, CancellationToken cancellation)
    {
        // Reading after subscribing closes the read/subscribe race; refresh recovers dropped notifications.
        while (!cancellation.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            if (await sessions.Resolve(token, cancellation) is null)
                return;
            var version = await Version(id, account, observing, cancellation);
            if (version is null)
                return;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                matchId = id,
                revision = version.Revision,
                privateVersion = version.PrivateVersion
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

    private static async Task ReceiveUntilClose(WebSocket socket, CancellationTokenSource lifetime)
    {
        var buffer = new byte[1024];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var read = await socket.ReceiveAsync(buffer, lifetime.Token);
                if (read.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        finally { await lifetime.CancelAsync(); }
    }
}
