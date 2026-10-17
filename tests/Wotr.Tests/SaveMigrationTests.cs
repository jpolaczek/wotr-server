using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class SaveMigrationTests
{
    [Theory]
    [InlineData("action.json", false)]
    [InlineData("suspended-action.json", true)]
    public void HistoricalActionAndItsReturnGainNewCommandsWithoutAdvancing(string file, bool suspended)
    {
        var raw = JsonNode.Parse(LegacyContractFixture.Read(file))!;
        var original = suspended ? raw["resolution"]!["frames"]![0]!["returnDecision"]! : raw["pending"]!;
        Assert.DoesNotContain("UseNarya", original["commands"]!.ToJsonString());
        var migrated = LegacyContractFixture.State(file);
        var decision = suspended ? migrated.Resolution.Frames[0].ReturnDecision! : migrated.Pending!;
        Assert.Equal(StateCodec.CurrentVersion, migrated.SchemaVersion);
        Assert.Equal(original["id"]!.GetValue<string>(), decision.Id);
        Assert.Contains(CommandKind.UseNarya, decision.Commands);
        Assert.Contains(CommandKind.DiscardLore, decision.Commands);
        Assert.Contains(CommandKind.DiscardGates, decision.Commands);
        Assert.DoesNotContain(CommandKind.AwakenSovereign, decision.Commands);
        Assert.Equal(raw["revision"]!.GetValue<long>(), migrated.Revision);
        Assert.Equal(raw["randomSamples"]!.AsArray().Count, migrated.RandomSamples.Count);
        Assert.Equal(StateCodec.Write(migrated), StateCodec.Write(StateCodec.Read(StateCodec.Write(migrated))));
        if (suspended)
        {
            var id = decision.Id;
            var result = new GameEngine(new DeclineReactionHandler()).Execute(migrated, Side.Shadow,
                KernelTests.Command(migrated, CommandKind.DeclineReaction), new SequenceRandom());
            Assert.True(result.Receipt.Accepted, result.Receipt.Error);
            Assert.Equal(id, result.State.Pending!.Id);
            Assert.Contains(CommandKind.UseNarya, result.State.Pending.Commands);
        }
    }

    [Fact]
    public void VersionTwoSnapshotDoesNotInventAnActionSequenceOrReplayItsCost()
    {
        var state = ActionSequenceFixture.Board();
        var army = ActionSequenceFixture.Put(state, "gondor", "minas-tirith").Id;
        var payload = ActionSequenceFixture.Move(state, "minas-tirith", "osgiliath", army);
        payload.Stepwise = false;
        state = ActionSequenceFixture.Do(state, CommandKind.MoveArmies, payload);
        var expected = StateCodec.Write(state);
        state.SchemaVersion = 2;
        var restored = StateCodec.Read(StateCodec.Write(state));
        Assert.Null(restored.Resolution.ActionSequence);
        Assert.Equal(expected, StateCodec.Write(restored));
        Assert.Equal(DieLocation.Spent, restored.Dice[0].Location);
    }

    [Fact]
    public void CurrentRestrictedDecisionIsNotBroadenedByEveryCopy()
    {
        var state = KernelTests.NewGame();
        state.Open(DecisionKind.Action, Side.FreePeoples, [CommandKind.DiscardDie]);
        Assert.Equal(state.Pending!.Commands, StateCodec.Copy(state).Pending!.Commands);
    }

    [Fact]
    public void HistoricalMoveKeepsTheSameCostAndRecordedRandomResult()
    {
        var state = LegacyContractFixture.State("before.json");
        var result = CharacterFixture.Engine().Execute(state, Side.FreePeoples,
            LegacyContractFixture.Command(), new SequenceRandom(0));
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        var expected = LegacyContractFixture.State("after.json");
        Assert.Null(expected.HuntReport);
        Assert.True(result.State.HuntReport!.Completed);
        var historical = StateCodec.Copy(result.State);
        BaseCorpusTests.CheckAndRemoveJournalAdditions(state, historical, LegacyContractFixture.Command());
        var actual = JsonNode.Parse(StateCodec.Write(historical))!.AsObject();
        // Additive public reporting is new; all historical rule state and RNG must
        // still match the untouched fixture exactly.
        actual.Remove("huntReport");
        actual.Remove("huntReportSequence");
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(StateCodec.Write(expected)), actual));
        Assert.Equal(state.RandomSamples.Count + 1, result.State.RandomSamples.Count);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("{}")]
    public void MalformedOrUnversionedSaveFailsClosed(string json) =>
        Assert.Equal("CorruptSave", Assert.Throws<InvalidDataException>(() => StateCodec.Read(json)).Message);
}
