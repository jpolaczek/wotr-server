namespace Wotr.Domain;

// RANK-02 (R35, docs/RANKED_PLAY.md): an account's single rating, whatever the expansions and faction.
public sealed record PlayerRating(double Rating, double Deviation, double Volatility)
{
    public static PlayerRating Initial { get; } =
        new(Glicko2.InitialRating, Glicko2.InitialDeviation, Glicko2.InitialVolatility);
}

// One game of a rating period: the opponent's rating before the game and the player's score (1 a win, 0 a loss).
public readonly record struct RatedGame(PlayerRating Opponent, double Score);
