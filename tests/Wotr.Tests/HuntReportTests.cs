using System.Text.Json;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.HuntTests;

namespace Wotr.Tests;

public class HuntReportTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AutomaticMissKeepsPublicRollAfterTheFrameHasGone(int level)
    {
        var random = level == 0 ? new SequenceRandom() : new SequenceRandom(0);
        var state = Move(State(level), random);
        Assert.Null(HuntOptions.View(state));
        var report = HuntReports.View(state)!;
        Assert.True(report.Completed);
        Assert.Equal(0, report.Initial.Progress);
        Assert.Equal(1, report.Outcome.Progress);
        var started = Assert.Single(report.Entries, e => e.Kind == HuntReportKind.Started).Hunt!;
        Assert.Null(started.MovementBonus);
        Assert.Null(started.EffectBonus);
        var roll = Assert.Single(report.Entries, e => e.Kind == HuntReportKind.Rolled);
        Assert.Equal(level, roll.Hunt!.Dice.Count);
        Assert.Equal(0, roll.Hunt.MovementBonus);
        Assert.Equal(0, roll.Hunt.EffectBonus);
        Assert.All(roll.Hunt.Dice, d => Assert.False(d.Success));
        Assert.DoesNotContain(report.Entries, e => e.Kind == HuntReportKind.TileDrawn);
        Assert.Equal(level, random.Calls);
        Assert.Equal(Json(report), Json(HuntReports.View(StateCodec.Copy(state))));
        report.Entries.Clear();
        Assert.NotEmpty(state.HuntReport!.Entries);
        var original = Json(state.HuntReport);
        state.Fellowship.Corruption++;
        HuntReports.Capture(state);
        Assert.Equal(original, Json(state.HuntReport));
        TurnFlow.SelectNext(state, Side.FreePeoples);
        state = Move(state, level == 0 ? new SequenceRandom() : new SequenceRandom(0));
        Assert.NotEqual(report.Id, state.HuntReport!.Id);
    }

    [Fact]
    public void OldRollWithoutBonusBreakdownKeepsOnlyTheKnownTotal()
    {
        var legacy = JsonSerializer.Deserialize<HuntView>("""{"Bonus":2}""")!;
        var restored = JsonSerializer.Deserialize<HuntView>(JsonSerializer.Serialize(legacy))!;
        Assert.Equal(2, restored.Bonus);
        Assert.Null(restored.MovementBonus);
        Assert.Null(restored.EffectBonus);
    }

    [Fact]
    public void SelectedRerollsAndAdditionalStrongholdTilesShareOneStableReport()
    {
        var state = State(1, "moria");
        Pool(state, Eye, "base.hunt.standard.eye.1.2");
        state = Move(state, new SequenceRandom(0));
        var id = state.HuntReport!.Id;
        Assert.False(state.HuntReport.Completed);
        state = Do(StateCodec.Copy(state), CommandKind.RerollHunt, new() { Ids = ["hunt-die-0"] }, new SequenceRandom(5, 0));
        var reroll = Assert.Single(state.HuntReport!.Entries, e => e.Kind == HuntReportKind.Rerolled).Hunt!;
        Assert.Equal(1, reroll.Dice[0].First);
        Assert.Equal(6, reroll.Dice[0].Result);
        Assert.True(reroll.Dice[0].Rerolled);
        Assert.True(reroll.Dice[0].Success);
        Assert.Contains("stronghold", reroll.RerollSources);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        ActionLogHuntFactsTests.AssertReport(state);
        var report = state.HuntReport!;
        Assert.Equal(id, report.Id);
        Assert.True(report.Completed);
        Assert.Equal(new[] { 1, 2 }, report.Entries.Where(e => e.Kind == HuntReportKind.Started).Select(e => e.HuntId));
        Assert.Equal(2, report.Entries.Count(e => e.Kind == HuntReportKind.TileDrawn));
        Assert.Equal("moria", report.Entries.Last(e => e.Kind == HuntReportKind.TileDrawn).SourceId);
        Assert.All(report.Entries.Where(e => e.HuntId == 2 && e.Hunt is not null), entry =>
        {
            Assert.Null(entry.Hunt!.MovementBonus);
            Assert.Null(entry.Hunt.EffectBonus);
        });
        Assert.False(report.Outcome.Hidden);
        Assert.Equal(1, report.Outcome.Corruption);
    }

    [Fact]
    public void ReplacementRetainsUnknownShelobAndOrderedProtectionWithoutInventingItsDie()
    {
        var state = HuntProtectionCardTests.ShelobReplacement();
        var drawn = Assert.Single(state.HuntReport!.Entries, e => e.Kind == HuntReportKind.TileDrawn);
        Assert.True(drawn.Hunt!.DamagePending);
        Assert.Null(drawn.Hunt.TileDie);
        state = Do(StateCodec.Copy(state), CommandKind.RespondEffect,
            new() { ReactionId = FreePeoplesCharacterCards01.ReplaceReaction }, new SequenceRandom(0));
        state = Do(state, CommandKind.RespondEffect, new() { ReactionId = FreePeoplesCharacterCards01.ProtectReaction });
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        ActionLogHuntFactsTests.AssertReport(state);
        var report = state.HuntReport!;
        Assert.True(report.Completed);
        Assert.All(report.Entries.Where(e => e.Hunt?.TileId == drawn.Hunt.TileId), e => Assert.Null(e.Hunt!.TileDie));
        Assert.Equal(Three, Assert.Single(report.Entries, e => e.Kind == HuntReportKind.TileReplaced).Hunt!.TileId);
        Assert.Equal(3, Assert.Single(report.Entries, e => e.Kind == HuntReportKind.DamageDetermined).Hunt!.Damage);
        Assert.Equal(2, Assert.Single(report.Entries, e => e.Kind == HuntReportKind.DamageChanged).Hunt!.Damage);
        Assert.Equal(2, report.Outcome.Corruption);
        Assert.Equal(1, report.Outcome.MordorStep);
        Assert.Equal(2, state.RandomSamples.Count);
    }

    [Fact]
    public void PublicGuideChangesPrecedeRemainingCorruptionAndPrivateHandsNeverEnterReport()
    {
        var state = State();
        Pool(state, Three);
        state = Move(state, new SequenceRandom(5, 0));
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.RandomCompanion }, new SequenceRandom(3));
        Assert.Equal("base.character.gimli", Assert.Single(state.HuntReport!.Entries, e => e.Kind == HuntReportKind.CompanionLeft).CharacterId);
        state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        ActionLogHuntFactsTests.AssertReport(state);
        var report = state.HuntReport!;
        var guide = report.Entries.FindIndex(e => e.Kind == HuntReportKind.GuideChanged);
        var corruption = report.Entries.FindIndex(e => e.Kind == HuntReportKind.Corruption);
        Assert.True(guide >= 0 && guide < corruption);
        Assert.Equal(1, report.Outcome.Corruption);
        var changed = StateCodec.Copy(state);
        changed.Player(Side.FreePeoples).Hand.Add("private-card-must-not-leak");
        changed.HuntTiles.Reverse();
        changed.RandomSamples.Clear();
        Assert.Equal(Json(Views().For(state, Side.Shadow).HuntReport), Json(Views().For(changed, Side.Shadow).HuntReport));
        Assert.Equal(Json(Views().For(state, Side.FreePeoples).HuntReport), Json(Views().For(state, Side.Shadow).HuntReport));
        Assert.DoesNotContain("private-card-must-not-leak", Json(HuntReports.View(changed)));
    }

    [Theory]
    [InlineData(8, Side.FreePeoples, 5)]
    [InlineData(9, Side.Shadow, 4)]
    public void MordorFinalReportRetainsTileAndCorruptionVictoryPriority(int corruption, Side winner, int step)
    {
        var state = MordorTests.OnTrack(4, corruption);
        Pool(state, Three);
        state = Move(state, new SequenceRandom(0));
        Assert.DoesNotContain(state.HuntReport!.Entries, e => e.Kind is HuntReportKind.Rolled or HuntReportKind.Rerolled);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        ActionLogHuntFactsTests.AssertReport(state);
        var report = state.HuntReport!;
        Assert.True(report.Completed);
        Assert.Equal(winner, report.Winner);
        Assert.Equal(step, report.Outcome.MordorStep);
        Assert.Equal(corruption + 3, report.Outcome.Corruption);
        Assert.Equal(Three, Assert.Single(report.Entries, e => e.Kind == HuntReportKind.TileDrawn).Hunt!.TileId);
        Assert.All(report.Entries.Where(e => e.Hunt is not null), entry =>
        {
            Assert.Null(entry.Hunt!.MovementBonus);
            Assert.Null(entry.Hunt.EffectBonus);
        });
        Assert.Equal(Json(report), Json(StateCodec.Copy(state).HuntReport));
    }

    [Fact]
    public void FireAndShadowRetainsBothPublicEliminationsAndTheNewGuide()
    {
        var state = LordsFireAndShadowTests.BeforeRoute(true, "moria");
        state.Fellowship.Companions.Remove(CharacterRules.Strider);
        Pool(state, Eye);
        state = LordsFixture.Do(state, CommandKind.DeclareFellowship, new() { Route = ["moria"] }, new SequenceRandom(0));
        Assert.Equal(LordsCharacters.Balrog, Assert.Single(state.HuntReport!.Entries,
            e => e.Kind == HuntReportKind.CharacterEliminated).CharacterId);
        Assert.Equal(CharacterRules.Grey, Assert.Single(state.HuntReport.Entries,
            e => e.Kind == HuntReportKind.CompanionLeft).CharacterId);
        state = LordsFixture.Do(state, CommandKind.SelectGuide, new() { GuideId = "base.character.gimli" });
        Assert.True(state.HuntReport!.Completed);
        Assert.Equal("base.character.gimli", state.HuntReport.Outcome.GuideId);
    }

    [Theory]
    [InlineData("base.hunt.special-sh.3.0.1", true, false, 3)]
    [InlineData("base.hunt.standard.1.1.1", false, true, 1)]
    [InlineData("base.hunt.special-fp.-2.0.1", false, false, -2)]
    public void StopRevealAndHealingRemainIndependent(string tile, bool stop, bool reveal, int damage)
    {
        var state = MordorTests.OnTrack(2, 4);
        Pool(state, tile);
        state = Move(state, new SequenceRandom(0));
        if (damage > 0)
            state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        ActionLogHuntFactsTests.AssertReport(state);
        var report = state.HuntReport!;
        Assert.True(report.Completed);
        var result = report.Entries.Last(e => e.Hunt is not null).Hunt!;
        Assert.Equal(stop, result.Stop);
        Assert.Equal(reveal, result.Reveal);
        Assert.Equal(Math.Max(0, -damage), result.Healing);
        Assert.Equal(stop ? 2 : 3, report.Outcome.MordorStep);
        Assert.Equal(!reveal, report.Outcome.Hidden);
        Assert.Equal(4 + damage, report.Outcome.Corruption);
    }

    [Fact]
    public void ReturningHobbitRecordsThePublicDestinationBeforeTheReportCloses()
    {
        var state = Move(CharacterHuntTests.Hobbits(), new SequenceRandom(5, 0));
        state = Do(state, CommandKind.DeclineReaction);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide });
        Assert.Equal("separating", Assert.Single(state.HuntReport!.Entries,
            e => e.Kind == HuntReportKind.CompanionLeft).Destination);
        state = Do(state, CommandKind.DeclineReaction);
        state = Do(StateCodec.Copy(state), CommandKind.SeparateCompanions,
            new() { Ids = [CharacterHuntTests.Merry], Route = ["hollin", "north-dunland"] });
        var placed = Assert.Single(state.HuntReport!.Entries, e => e.Kind == HuntReportKind.CompanionPlaced);
        Assert.Equal(CharacterHuntTests.Merry, placed.CharacterId);
        Assert.Equal("north-dunland", placed.Destination);
        Assert.True(state.HuntReport.Completed);
        Assert.Equal(2, state.HuntReport.Outcome.Corruption);
    }

    [Fact]
    public void StaffCancellationRetainsTheKnownReactionWithoutInventingATile()
    {
        var state = State();
        state.Player(Side.FreePeoples).Table.Add(FreePeoplesCharacterCards02.Staff);
        var samplesBeforeHunt = state.RandomSamples.Count;
        state = Move(state, new SequenceRandom(5));
        state = Do(state, CommandKind.RespondEffect,
            new() { ReactionId = FreePeoplesCharacterCards02.StaffReaction });
        ActionLogHuntFactsTests.AssertReport(state);
        var report = state.HuntReport!;
        Assert.True(report.Completed);
        Assert.Equal(FreePeoplesCharacterCards02.Staff,
            Assert.Single(report.Entries, e => e.Kind == HuntReportKind.Reaction).SourceId);
        Assert.DoesNotContain(report.Entries, e => e.Kind == HuntReportKind.TileDrawn);
        Assert.Equal(0, report.Outcome.Corruption);
        Assert.Equal(samplesBeforeHunt + 1, state.RandomSamples.Count);
    }

    [Fact]
    public void InvalidReportFrameReferenceFailsClosedWithoutReplayingTheHunt()
    {
        var state = Move(State(1, "moria"), new SequenceRandom(0));
        state.HuntReport!.Entries.Clear();
        Assert.Equal("CorruptSave", Assert.Throws<InvalidDataException>(() => StateCodec.Copy(state)).Message);
    }

    [Fact]
    public void OldActiveSaveHasNoInventedHistoryAfterResuming()
    {
        var state = State();
        Pool(state, Three);
        state = Move(state, new SequenceRandom(5, 0));
        var json = JsonNode.Parse(StateCodec.Write(state))!;
        RemoveReport(json);
        state = StateCodec.Read(json.ToJsonString());
        Assert.Null(state.HuntReport);
        Assert.Null(Views().For(state, Side.FreePeoples).HuntReport);
        state = Do(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring });
        Assert.Null(state.HuntReport);
        Assert.Equal(3, state.Fellowship.Corruption);
    }

    [PostgresFact]
    public async Task ReportSurvivesHostRestartAndEachReceiptWithoutDuplicateEntriesOrRandomness()
    {
        var state = State(1, "moria");
        Pool(state, Three);
        state = Move(state, new SequenceRandom(0));
        var id = state.HuntReport!.Id;
        state = await CardPackPersistenceTests.RestartDecision(state, CommandKind.RerollHunt,
            new() { Ids = ["hunt-die-0"] }, new SequenceRandom(5, 0));
        state = await CardPackPersistenceTests.RestartDecision(state, CommandKind.ResolveHuntDamage,
            new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom());
        Assert.Equal(id, state.HuntReport!.Id);
        Assert.True(state.HuntReport.Completed);
        Assert.Single(state.HuntReport.Entries, e => e.Kind == HuntReportKind.Rerolled);
        Assert.Single(state.HuntReport.Entries, e => e.Kind == HuntReportKind.Response);
    }

    private static string Json(object? value) => JsonSerializer.Serialize(value, BoardCatalog.Json);

    private static void RemoveReport(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToArray())
            {
                if (key.Equals("huntReport", StringComparison.OrdinalIgnoreCase) || key.Equals("huntReportSequence", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("reportIndex", StringComparison.OrdinalIgnoreCase))
                    obj.Remove(key);
                else if (obj[key] is { } child)
                    RemoveReport(child);
            }
        }
        else if (node is JsonArray array)
            foreach (var child in array.OfType<JsonNode>())
                RemoveReport(child);
    }
}
