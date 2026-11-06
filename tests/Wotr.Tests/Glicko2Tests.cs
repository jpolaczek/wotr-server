using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// RANK-02 (R35, docs/RANKED_PLAY.md): the expected numbers come from Glickman's worked example and from an independent
// reference script of 2026-11-05 that follows the same published steps.
public class Glicko2Tests
{
    private static readonly PlayerRating Established = new(1500, 80, 0.06);

    [Fact]
    public void GlickmansExampleReproduces()
    {
        var player = new PlayerRating(1500, 200, 0.06);
        var after = Glicko2.Period(player,
        [
            new RatedGame(new PlayerRating(1400, 30, 0.06), 1),
            new RatedGame(new PlayerRating(1550, 100, 0.06), 0),
            new RatedGame(new PlayerRating(1700, 300, 0.06), 0)
        ]);
        Assert.Equal(1464.05, after.Rating, 0.01);
        Assert.Equal(151.52, after.Deviation, 0.01);
        Assert.InRange(after.Volatility, 0.05999, 0.06);
    }

    [Fact]
    public void AFirstWinBetweenTwoNewPlayers()
    {
        var after = Glicko2.Update(PlayerRating.Initial, PlayerRating.Initial, 1);
        Assert.Equal(1662.3, after.Rating, 0.1);
        Assert.Equal(290.3, after.Deviation, 0.1);
    }

    [Fact]
    public void AGameUpdatesBothPlayersFromTheirRatingsBeforeIt()
    {
        var (winner, loser) = Glicko2.Game(PlayerRating.Initial, Established);
        Assert.Equal(1675.0, winner.Rating, 0.1);
        Assert.Equal(1487.8, loser.Rating, 0.1);
        Assert.Equal(Glicko2.Update(Established, PlayerRating.Initial, 0), loser);
    }

    // An established favourite gains little by winning and loses more by losing.
    [Fact]
    public void TheExpectedResultWeighsTheChange()
    {
        var favourite = new PlayerRating(1800, 60, 0.06);
        var outsider = new PlayerRating(1500, 60, 0.06);
        Assert.Equal(3.2, Glicko2.Update(favourite, outsider, 1).Rating - 1800, 0.1);
        Assert.Equal(-17.5, Glicko2.Update(favourite, outsider, 0).Rating - 1800, 0.1);
    }

    // D12: a new player alternating wins and losses against an established 1500 settles within a few dozen games.
    [Fact]
    public void ANewPlayersSwingsShrinkAsGamesAccumulate()
    {
        var expected = new Dictionary<int, double> { [1] = 175.0, [2] = -183.4, [10] = -37.6, [20] = -20.1, [40] = -12.8 };
        var player = PlayerRating.Initial;
        for (var game = 1; game <= 40; game++)
        {
            var after = Glicko2.Update(player, Established, game % 2);
            if (expected.TryGetValue(game, out var change))
                Assert.Equal(change, after.Rating - player.Rating, 0.1);
            player = after;
        }
        Assert.Equal(66.76, player.Deviation, 0.01);
    }

    [Fact]
    public void AWinNeverLowersAndALossNeverRaisesARatingAndEveryResultIsFinite()
    {
        var ratings = new[] { 0.0, 800, 1500, 2200, 3000, 6000 };
        var deviations = new[] { 30.0, 45, 80, 150, 350 };
        var grid = ratings.SelectMany(r => deviations.Select(d => new PlayerRating(r, d, 0.06))).ToList();
        foreach (var player in grid)
            foreach (var opponent in grid)
            {
                var win = Glicko2.Update(player, opponent, 1);
                var loss = Glicko2.Update(player, opponent, 0);
                Assert.True(win.Rating >= player.Rating, $"{player} beating {opponent} fell to {win.Rating}");
                Assert.True(loss.Rating <= player.Rating, $"{player} losing to {opponent} rose to {loss.Rating}");
                foreach (var result in new[] { win, loss })
                {
                    Assert.True(double.IsFinite(result.Rating) && double.IsFinite(result.Volatility));
                    Assert.InRange(result.Deviation, Glicko2.MinimumDeviation, 351);
                }
            }
    }

    [Fact]
    public void ADeviationNeverFallsBelowTheFloor()
    {
        var certain = new PlayerRating(1500, 30, 0.06);
        Assert.Equal(Glicko2.MinimumDeviation, Glicko2.Update(certain, certain, 1).Deviation);
    }

    [Fact]
    public void InvalidInputIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Glicko2.Update(new PlayerRating(double.NaN, 350, 0.06), Established, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Glicko2.Update(PlayerRating.Initial, new PlayerRating(1500, 0, 0.06), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Glicko2.Update(PlayerRating.Initial, new PlayerRating(1500, 80, -1), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Glicko2.Update(PlayerRating.Initial, Established, 1.5));
        Assert.Throws<ArgumentException>(() => Glicko2.Period(PlayerRating.Initial, []));
    }
}
