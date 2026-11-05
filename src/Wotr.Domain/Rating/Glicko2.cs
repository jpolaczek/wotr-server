namespace Wotr.Domain;

// RANK-02 (R35, docs/RANKED_PLAY.md): published Glicko-2, steps 1-8 of Glickman's "Example of the Glicko-2 system".
// W3Champions bases its rating on Glicko-2 and keeps its improvements private, so this follows the published method,
// with one game as one rating period. The engine and the database never meet here: callers pass ratings in and store
// what comes back.
public static class Glicko2
{
    public const double InitialRating = 1500;
    public const double InitialDeviation = 350;
    public const double InitialVolatility = 0.06;
    public const double MinimumDeviation = 45;

    private const double Tau = 0.5;
    private const double Scale = 173.7178;
    private const double Convergence = 0.000001;
    // The iteration converges in a handful of rounds; the bound only stops bad input from holding a transaction.
    private const int IterationLimit = 100;
    // A rating gap of thousands of points would make the expected score exactly 0 or 1 in floating point, leaving the
    // game no variance. Keeping it this far from both ends changes no realistic result and keeps every result finite.
    private const double ExpectationMargin = 0.000000001;

    // A finished game updates both players from each other's ratings before the game.
    public static (PlayerRating Winner, PlayerRating Loser) Game(PlayerRating winner, PlayerRating loser) =>
        (Update(winner, loser, 1), Update(loser, winner, 0));

    public static PlayerRating Update(PlayerRating player, PlayerRating opponent, double score) =>
        Period(player, [new RatedGame(opponent, score)]);

    public static PlayerRating Period(PlayerRating player, IReadOnlyList<RatedGame> games)
    {
        Validate(player, nameof(player));
        if (games.Count == 0)
            throw new ArgumentException("A rating period needs at least one game.", nameof(games));
        var mu = ToMu(player.Rating);
        var phi = player.Deviation / Scale;
        var (variance, improvement) = Outcomes(mu, games);
        var volatility = NewVolatility(phi, player.Volatility, variance * improvement, variance);
        var phiStar = Math.Sqrt(phi * phi + volatility * volatility);
        var newPhi = 1 / Math.Sqrt(1 / (phiStar * phiStar) + 1 / variance);
        var newMu = mu + newPhi * newPhi * improvement;
        return new PlayerRating(newMu * Scale + InitialRating, Math.Max(MinimumDeviation, newPhi * Scale), volatility);
    }

    // Steps 3 and 4: the estimated variance v of the rating from the games alone, and the sum behind the improvement.
    private static (double Variance, double Improvement) Outcomes(double mu, IReadOnlyList<RatedGame> games)
    {
        var information = 0.0;
        var improvement = 0.0;
        foreach (var game in games)
        {
            Validate(game.Opponent, nameof(games));
            if (!double.IsFinite(game.Score) || game.Score < 0 || game.Score > 1)
                throw new ArgumentOutOfRangeException(nameof(games), game.Score, "A score lies between 0 and 1.");
            var weight = G(game.Opponent.Deviation / Scale);
            var expected = Expected(mu, ToMu(game.Opponent.Rating), weight);
            information += weight * weight * expected * (1 - expected);
            improvement += weight * (game.Score - expected);
        }
        return (1 / information, improvement);
    }

    private static double ToMu(double rating) => (rating - InitialRating) / Scale;

    private static double G(double phi) => 1 / Math.Sqrt(1 + 3 * phi * phi / (Math.PI * Math.PI));

    private static double Expected(double mu, double opponentMu, double weight)
    {
        var expected = 1 / (1 + Math.Exp(-weight * (mu - opponentMu)));
        return Math.Clamp(expected, ExpectationMargin, 1 - ExpectationMargin);
    }

    // Step 5: the new volatility, by the Illinois iteration on f, bounded.
    private static double NewVolatility(double phi, double volatility, double delta, double variance)
    {
        var equation = new VolatilityEquation(phi, delta, variance, Math.Log(volatility * volatility));
        var a = equation.A;
        var b = LowerBound(equation);
        var fa = equation.Value(a);
        var fb = equation.Value(b);
        for (var round = 0; round < IterationLimit && Math.Abs(b - a) > Convergence; round++)
        {
            var c = a + (a - b) * fa / (fb - fa);
            var fc = equation.Value(c);
            if (fc * fb <= 0)
            {
                a = b;
                fa = fb;
            }
            else
                fa /= 2;
            b = c;
            fb = fc;
        }
        return Math.Exp(a / 2);
    }

    private static double LowerBound(VolatilityEquation equation)
    {
        var excess = equation.Delta * equation.Delta - equation.Phi * equation.Phi - equation.Variance;
        if (excess > 0)
            return Math.Log(excess);
        var k = 1;
        while (k < IterationLimit && equation.Value(equation.A - k * Tau) < 0)
            k++;
        return equation.A - k * Tau;
    }

    private static void Validate(PlayerRating rating, string parameter)
    {
        if (!double.IsFinite(rating.Rating))
            throw new ArgumentOutOfRangeException(parameter, rating.Rating, "A rating is a finite number.");
        if (!double.IsFinite(rating.Deviation) || rating.Deviation <= 0)
            throw new ArgumentOutOfRangeException(parameter, rating.Deviation, "A rating deviation is positive.");
        if (!double.IsFinite(rating.Volatility) || rating.Volatility <= 0)
            throw new ArgumentOutOfRangeException(parameter, rating.Volatility, "A rating volatility is positive.");
    }

    // f(x) of step 5, around a = ln(sigma^2).
    private readonly record struct VolatilityEquation(double Phi, double Delta, double Variance, double A)
    {
        public double Value(double x)
        {
            var ex = Math.Exp(x);
            var spread = Phi * Phi + Variance + ex;
            return ex * (Delta * Delta - Phi * Phi - Variance - ex) / (2 * spread * spread) - (x - A) / (Tau * Tau);
        }
    }
}
