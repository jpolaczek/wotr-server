namespace Wotr.Server;

// One open update socket's place in its account's budget. Eviction closes the socket; the place is already gone.
public sealed class UpdateSocketSlot
{
    private readonly CancellationTokenSource eviction = new();

    public CancellationToken Evicted => eviction.Token;

    public void Evict() => eviction.Cancel();
}

// QUAL-02: open update sockets per account. A slot is taken before the socket is accepted and always given back
// when it ends (close, disconnect, cancellation or failure); an account without sockets leaves no entry behind.
// RANK-10: a seated ranked player's socket always gets one, in place of that account's oldest, which may be dead.
public sealed class UpdateSocketBudget(GameRateLimitOptions limits)
{
    private readonly Dictionary<string, List<UpdateSocketSlot>> open = new(StringComparer.Ordinal);

    public int Open(string account)
    {
        lock (open)
            return open.TryGetValue(account, out var slots) ? slots.Count : 0;
    }

    public int Accounts
    {
        get
        {
            lock (open)
                return open.Count;
        }
    }

    public UpdateSocketSlot? TryTake(string account, bool evictOldest = false)
    {
        UpdateSocketSlot? evicted = null;
        var slot = new UpdateSocketSlot();
        lock (open)
        {
            var slots = open.GetValueOrDefault(account) ?? [];
            if (slots.Count >= limits.UpdateSockets)
            {
                if (!evictOldest)
                    return null;
                evicted = slots[0];
                slots.RemoveAt(0);
            }
            slots.Add(slot);
            open[account] = slots;
        }
        evicted?.Evict();
        return slot;
    }

    public void Release(string account, UpdateSocketSlot slot)
    {
        lock (open)
            if (open.TryGetValue(account, out var slots) && slots.Remove(slot) && slots.Count == 0)
                open.Remove(account);
    }
}
