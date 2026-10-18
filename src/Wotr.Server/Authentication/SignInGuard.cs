namespace Wotr.Server;

// SEC-02: how many failed sign-ins an account name may collect, within which window, and how long it is then locked.
public sealed record SignInLimits(int Failures, TimeSpan Window, TimeSpan Lock)
{
    public const string FailuresKey = "Authentication:SignInFailures";
    public const string WindowKey = "Authentication:SignInWindowSeconds";
    public const string LockKey = "Authentication:SignInLockSeconds";
    public static readonly SignInLimits Default = new(10, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));

    public static SignInLimits Read(IConfiguration configuration) => new(
        Positive(configuration, FailuresKey, Default.Failures),
        TimeSpan.FromSeconds(Positive(configuration, WindowKey, (int)Default.Window.TotalSeconds)),
        TimeSpan.FromSeconds(Positive(configuration, LockKey, (int)Default.Lock.TotalSeconds)));

    private static int Positive(IConfiguration configuration, string key, int fallback)
    {
        var value = configuration.GetValue(key, fallback);
        return value > 0 ? value : throw new InvalidOperationException($"{key} must be a positive whole number.");
    }
}

// SEC-02: a temporary lock per account name on top of the per-address limit (AuthenticationRateLimit). A name that
// collects SignInLimits.Failures failed sign-ins within the window is refused for SignInLimits.Lock from every address,
// even with the right password, and its password is not checked meanwhile. Names that do not exist count the same, so
// a lock tells nothing about accounts. The counts live in memory (one server process; a restart clears them) and are
// bounded by Capacity; the log never names the account.
public sealed class SignInGuard(SignInLimits limits, TimeProvider clock, ILogger<SignInGuard> log)
{
    public const int Capacity = 10_000;
    private readonly Lock gate = new();
    private readonly Dictionary<string, Entry> names = new(StringComparer.Ordinal);

    private sealed class Entry(DateTimeOffset windowStart)
    {
        public DateTimeOffset WindowStart { get; set; } = windowStart;
        public int Failures { get; set; }
        public DateTimeOffset LockedUntil { get; set; } = DateTimeOffset.MinValue;
    }

    public SignInLimits Limits => limits;

    public int Tracked
    {
        get
        {
            lock (gate)
                return names.Count;
        }
    }

    // The time left on the name's lock, or null when it may sign in.
    public TimeSpan? LockedFor(string name)
    {
        var now = clock.GetUtcNow();
        lock (gate)
            return names.TryGetValue(Key(name), out var entry) && entry.LockedUntil > now ? entry.LockedUntil - now : null;
    }

    public void Failed(string name)
    {
        var now = clock.GetUtcNow();
        lock (gate)
        {
            var entry = Track(Key(name), now);
            if (now - entry.WindowStart >= limits.Window)
                Restart(entry, now);
            entry.Failures++;
            if (entry.Failures < limits.Failures)
                return;
            entry.LockedUntil = now + limits.Lock;
            Restart(entry, entry.LockedUntil);
        }
        log.LogWarning("An account name was locked for {Seconds} s after {Failures} failed sign-ins.",
            (int)limits.Lock.TotalSeconds, limits.Failures);
    }

    // A successful sign-in clears the name's failures.
    public void Succeeded(string name)
    {
        lock (gate)
            names.Remove(Key(name));
    }

    private static string Key(string name) => name.Trim().ToUpperInvariant();

    private static void Restart(Entry entry, DateTimeOffset windowStart)
    {
        entry.WindowStart = windowStart;
        entry.Failures = 0;
    }

    private Entry Track(string key, DateTimeOffset now)
    {
        if (names.TryGetValue(key, out var entry))
            return entry;
        if (names.Count >= Capacity)
            MakeRoom(now);
        entry = new Entry(now);
        names[key] = entry;
        return entry;
    }

    // Forgets names whose window and lock are over; when every name is still active, the one active longest ago goes.
    private void MakeRoom(DateTimeOffset now)
    {
        foreach (var (key, entry) in names.ToList())
        {
            if (entry.LockedUntil <= now && now - entry.WindowStart >= limits.Window)
                names.Remove(key);
        }
        if (names.Count < Capacity)
            return;
        var oldest = names.MinBy(pair => Max(pair.Value.WindowStart, pair.Value.LockedUntil)).Key;
        names.Remove(oldest);
    }

    private static DateTimeOffset Max(DateTimeOffset first, DateTimeOffset second) => first > second ? first : second;
}
