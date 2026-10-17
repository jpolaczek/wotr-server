using System.Threading.Channels;

namespace Wotr.Server;

public sealed class RevisionHub
{
    private readonly object gate = new();
    private sealed record Watcher(string Account, Channel<long> Channel);
    private readonly Dictionary<string, Dictionary<Guid, Watcher>> watchers = [];

    public (Guid Id, Channel<long> Channel) Subscribe(string match, string account = "")
    {
        var channel = Channel.CreateBounded<long>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });
        var id = Guid.NewGuid();
        lock (gate)
        {
            if (!watchers.TryGetValue(match, out var group))
                watchers[match] = group = [];
            group[id] = new(account, channel);
        }
        return (id, channel);
    }

    // OBS-06: the open update streams of one match (players' and observers'), for resource checks.
    public int Watchers(string match)
    {
        lock (gate)
            return watchers.TryGetValue(match, out var group) ? group.Count : 0;
    }

    public void Remove(string match, Guid id)
    {
        lock (gate)
        {
            if (!watchers.TryGetValue(match, out var group))
                return;
            if (group.Remove(id, out var watcher))
                watcher.Channel.Writer.TryComplete();
            if (group.Count == 0)
                watchers.Remove(match);
        }
    }

    public void Publish(string match, long revision, string? onlyAccount = null)
    {
        lock (gate)
        {
            if (watchers.TryGetValue(match, out var group))
                foreach (var watcher in group.Values)
                    if (onlyAccount is null || watcher.Account == onlyAccount)
                        watcher.Channel.Writer.TryWrite(revision);
        }
    }
}
