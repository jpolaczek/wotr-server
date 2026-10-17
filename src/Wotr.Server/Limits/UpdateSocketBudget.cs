namespace Wotr.Server;

// QUAL-02: open update sockets per account. A slot is taken before the socket is accepted and always given back
// when it ends (close, disconnect, cancellation or failure); an account without sockets leaves no entry behind.
public sealed class UpdateSocketBudget(GameRateLimitOptions limits)
{
    private readonly Dictionary<string, int> open = new(StringComparer.Ordinal);

    public int Open(string account)
    {
        lock (open)
            return open.GetValueOrDefault(account);
    }

    public int Accounts
    {
        get
        {
            lock (open)
                return open.Count;
        }
    }

    public bool TryTake(string account)
    {
        lock (open)
        {
            var count = open.GetValueOrDefault(account);
            if (count >= limits.UpdateSockets)
                return false;
            open[account] = count + 1;
            return true;
        }
    }

    public void Release(string account)
    {
        lock (open)
        {
            var count = open.GetValueOrDefault(account) - 1;
            if (count > 0)
                open[account] = count;
            else
                open.Remove(account);
        }
    }
}
