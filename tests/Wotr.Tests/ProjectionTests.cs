using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class ProjectionTests
{
    private readonly Projection projection = new(new CoreOptions());
    private static string Json(object value) => JsonSerializer.Serialize(value, BoardCatalog.Json);
    [Fact]
    public void OpponentSecretsNeverEnterRawProjectionOptionsPreviewOrHistory()
    {
        var first = KernelTests.NewGame();
        first.Configuration.Lords = true;
        first.Configuration.Council = true;
        first.Configuration.Kings = true;
        first.Player(Side.Shadow).Hand.Add("secret-a");
        first.Player(Side.Shadow).CommittedCombatCard = "combat-a";
        first.Player(Side.Shadow).CombatCardCommitted = true;
        first.Player(Side.Shadow).HiddenCorruptionTiles.Add(1);
        first.Events.Add(new()
        {
            Kind = "PrivateDraw",
            OnlyFor = Side.Shadow,
            Text = "secret-a"
        });
        first.Open(DecisionKind.CorruptionTile, Side.Shadow, []);
        first.Pending!.PrivateChoices.Add("tile-a");
        var second = StateCodec.Copy(first);
        second.Player(Side.Shadow).Hand[0] = "secret-b";
        second.Player(Side.Shadow).CommittedCombatCard = "combat-b";
        second.Player(Side.Shadow).HiddenCorruptionTiles[0] = 3;
        second.Player(Side.Shadow).Decks["character"].Reverse();
        second.Events[0].Text = "secret-b";
        second.Pending!.PrivateChoices[0] = "tile-b";
        Assert.Equal(Json(projection.For(first, Side.FreePeoples)), Json(projection.For(second, Side.FreePeoples)));
        Assert.NotEqual(Json(projection.For(first, Side.Shadow)), Json(projection.For(second, Side.Shadow)));
        var preview = new PreviewService(new GameEngine(new DeclineReactionHandler()));
        var command = KernelTests.Command(first, CommandKind.DeclineReaction);
        Assert.Equal(Json(preview.Preview(first, Side.FreePeoples, command)), Json(preview.Preview(second, Side.FreePeoples, command)));
        Assert.DoesNotContain("secret-a", Json(projection.For(first, Side.FreePeoples)));
    }
    [Fact]
    public void CouncilVersionsArePrivateAndVisibleCorruptionIsExplicit()
    {
        var s = KernelTests.NewGame();
        s.Configuration.Council = true;
        s.Configuration.Kings = true;
        s.Player(Side.FreePeoples).PrivateCompanionVersions.Add("alternate-gimli");
        var before = Json(projection.For(s, Side.Shadow));
        s.Player(Side.FreePeoples).PrivateCompanionVersions[0] = "standard-gimli";
        Assert.Equal(before, Json(projection.For(s, Side.Shadow)));
        s.Player(Side.Shadow).HiddenCorruptionTiles = [1, 2];
        Assert.Empty(projection.For(s, Side.FreePeoples).Players.Single(p => p.Side == Side.Shadow).VisibleCorruptionTiles);
        s.Configuration.VisibleCorruption = true;
        Assert.Equal(new[] { 1, 2 }, projection.For(s, Side.FreePeoples).Players.Single(p => p.Side == Side.Shadow).VisibleCorruptionTiles);
    }
    [Fact]
    public void PreviewDoesNotExecuteReactionOrMutateState()
    {
        var s = KernelTests.NewGame();
        s.Open(DecisionKind.Action, Side.Shadow, []);
        s.Resolution.Suspend(s, "test", 0);
        s.Open(DecisionKind.Reaction, Side.FreePeoples, [CommandKind.DeclineReaction]);
        var before = StateCodec.Write(s);
        var preview = new PreviewService(new GameEngine(new DeclineReactionHandler()));
        Assert.True(preview.Preview(s, Side.FreePeoples, KernelTests.Command(s, CommandKind.DeclineReaction)).Legal);
        Assert.Equal(before, StateCodec.Write(s));
    }
    [Fact]
    public void SubsetAndCostConstraintsStayLinearAndRejectDuplicatesOrForeignIds()
    {
        var ids = Enumerable.Range(1, 64).Select(n => $"unit.{n}").ToList();
        var option = new ActionOption { EligibleIds = ids, EligibleDice = ["ordinary", "will-west"], Minimum = 1, Maximum = 64 };
        Assert.True(DraftSelection.IsSubset(ids.Take(32).ToList(), option.EligibleIds, 1, 64));
        Assert.False(DraftSelection.IsSubset(["unit.1", "unit.1"], option.EligibleIds, 1, 64));
        Assert.False(DraftSelection.IsSubset(["foreign"], option.EligibleIds, 1, 64));
        Assert.False(DraftSelection.IsSubset([], option.EligibleIds, 1, 64));
        Assert.True(DraftSelection.CostMatches("will-west", option.EligibleDice));
        Assert.False(DraftSelection.CostMatches("enemy-die", option.EligibleDice));
        Assert.True(Json(option).Length < 1500, "Response contains candidates, never all subsets.");
    }
    [Fact]
    public void ErrorAllowlistDoesNotEchoPrivateText()
        => Assert.Equal("IllegalChoice", Visibility.SafeError("Enemy holds a secret card"));
}
