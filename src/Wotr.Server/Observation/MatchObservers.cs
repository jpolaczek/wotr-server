namespace Wotr.Server;

// OBS-02: the one rule for reading a match as an observer, shared by every read (board, history, battle reports,
// updates): the account watches the room of this match with a lease that has not lapsed and plays in it on no
// side. Used in queries over `matches m` with the match id as $1 and the account as $2.
internal static class MatchObservers
{
    public const string Watching = """
        m.fp_account<>$2 AND m.sh_account<>$2 AND EXISTS (SELECT 1 FROM rooms r
            JOIN room_observers o ON o.room_id=r.id
            WHERE r.match_id=m.id AND NOT r.closed AND o.account_id=$2 AND o.lease_until > now())
        """;
}
