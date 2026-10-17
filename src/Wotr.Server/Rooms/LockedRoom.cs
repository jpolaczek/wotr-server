using Npgsql;

namespace Wotr.Server;

// OBS-01: one room under its row lock — its state, its live audience and the open transaction. Every change to the
// players or the audience goes through it, so the limit of 2+16 is counted while no one else can join.
internal sealed class LockedRoom(RoomState state, List<RoomObserver> observers, ObserverOptions options,
    NpgsqlConnection connection, NpgsqlTransaction transaction)
{
    public RoomState State => state;
    public IReadOnlyList<RoomObserver> Observers => observers;
    public NpgsqlConnection Connection => connection;
    public NpgsqlTransaction Transaction => transaction;
    public bool AudienceFull => observers.Count >= options.Limit;

    public static async Task<LockedRoom> Load(RoomState state, ObserverOptions options, NpgsqlConnection connection,
        NpgsqlTransaction transaction, CancellationToken cancellation)
    {
        await RoomObservers.Prune(connection, transaction, state.Id, cancellation);
        var observers = await RoomObservers.Active(connection, transaction, state.Id, cancellation);
        return new LockedRoom(state, observers, options, connection, transaction);
    }

    public bool Observes(string account) => observers.Exists(o => o.Account == account);

    public bool Participates(string account) => state.Contains(account) || Observes(account);

    // Nobody but this account plays or watches.
    public bool OnlyParticipant(string account) =>
        EmptyOrOwn(state.FreePeoples, account) && EmptyOrOwn(state.Shadow, account) &&
        observers.TrueForAll(o => o.Account == account);

    private static bool EmptyOrOwn(string seat, string account) => seat.Length == 0 || seat == account;

    public RoomObserver? Observer(string account) => observers.Find(o => o.Account == account);

    // A new observer or the renewed lease of one already watching.
    public async Task AddObserver(string account, string name, CancellationToken cancellation)
    {
        await RoomObservers.Add(connection, transaction, state.Id, account, name, options.Lease, cancellation);
        if (!Observes(account))
            observers.Add(new RoomObserver(account, name));
    }

    public async Task RemoveObserver(string account, CancellationToken cancellation)
    {
        await RoomObservers.Remove(connection, transaction, state.Id, account, cancellation);
        observers.RemoveAll(o => o.Account == account);
    }
}
