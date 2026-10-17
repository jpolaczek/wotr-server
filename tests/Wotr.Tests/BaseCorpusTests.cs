using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.BaseCorpusFixture;

namespace Wotr.Tests;

public class BaseCorpusTests
{
    public static IEnumerable<object[]> AllCases() => Cases.Select(c => new object[] { c.Name });
    public static IEnumerable<object[]> ResumableCases() => Cases.Where(c => c.Command is not null).Select(c => new object[] { c.Name });

    [Fact]
    public void CorpusCoversEveryBaseDecisionAndAllFourFinalResults()
    {
        var baseDecisions = Enum.GetValues<DecisionKind>().Except([DecisionKind.CouncilVersions, DecisionKind.CorruptionTile,
            DecisionKind.SpecialDieChoice, DecisionKind.KeepActionDie, DecisionKind.CouncilHomelands, DecisionKind.CouncilToken,
            DecisionKind.RulerDiscard, DecisionKind.Dismay]).Order();
        Assert.Equal(baseDecisions, Cases.Where(c => c.Decision.HasValue).Select(c => c.Decision!.Value).Distinct().Order());
        Assert.Equal(Enum.GetValues<VictoryCondition>().Order(), Cases.Where(c => c.Victory.HasValue).Select(c => c.Victory!.Value).Order());
        Assert.Equal(29, Cases.Count);
        Assert.Equal(Cases.Count, Cases.Select(c => c.File).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void FrozenSaveReadsWithoutLosingDecisionCostOrContinuation(string name)
    {
        var entry = Case(name);
        var raw = Raw(entry);
        Assert.Equal(entry.Sha256, Hash(raw));
        var original = JsonSerializer.Deserialize<MatchState>(raw, BoardCatalog.Json)!;
        var state = StateCodec.Read(raw);
        Assert.Equal(entry.SchemaVersion, original.SchemaVersion);
        original.SchemaVersion = StateCodec.CurrentVersion;
        Assert.Equal(StateCodec.Write(original), StateCodec.Write(state));
        Assert.Equal(StateCodec.CurrentVersion, state.SchemaVersion);
        Assert.Equal(entry.Decision, state.Pending?.Kind);
        Assert.Equal(entry.Owner, state.Pending?.Owner);
        Assert.Equal(entry.Victory, state.Victory);
        Assert.Equal(state.Units.Count, state.Units.Select(u => u.Id).Distinct().Count());
        Assert.All(state.Resolution.PaidDieIds, id => Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == id).Location));
        var restored = StateCodec.Copy(state);
        foreach (var side in Enum.GetValues<Side>())
            Assert.Equal(Json(Views.For(state, side)), Json(Views.For(restored, side)));
        if (state.Winner.HasValue)
        {
            Assert.Equal(GamePhase.Finished, state.Phase);
            Assert.Null(state.Pending);
            Assert.Empty(state.Resolution.Frames);
            Assert.Empty(state.Resolution.PaidDieIds);
        }
    }

    [Theory]
    [MemberData(nameof(ResumableCases))]
    public void SavedDecisionResumesToTheReviewedResultWithoutMutatingItsInput(string name)
    {
        var entry = Case(name);
        var state = Load(entry);
        var before = StateCodec.Write(state);
        var random = new ZeroRandom();
        var result = CharacterFixture.Engine().Execute(state, state.Pending!.Owner, entry.Command!, random);
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        Assert.Equal(state.Revision + 1, result.State.Revision);
        Assert.Equal(before, StateCodec.Write(state));
        // Keep the reviewed historical bytes/hash, excluding independently checked additive metadata.
        var historical = StateCodec.Copy(result.State);
        if (name == "reveal")
            CheckAndRemoveNewHuntReport(state, historical);
        if (name is "strategy-march" or "paid-army-step")
            CheckAndRemoveNewActionMetadata(state, historical, name);
        CheckAndRemoveJournalAdditions(state, historical, entry.Command!);
        CheckAndRestoreNamedTexts(state, historical);
        historical.SchemaVersion = entry.SchemaVersion;
        Assert.Equal(entry.ExpectedStateSha256, Hash(StateCodec.Write(historical)));
        Assert.Equal(entry.ExpectedDecision, result.State.Pending?.Kind);
        Assert.Equal(entry.ExpectedOwner, result.State.Pending?.Owner);
        Assert.Equal(entry.ExpectedVictory, result.State.Victory);
        Assert.Equal(entry.ExpectedRandomSamples, result.State.RandomSamples.Count);
        Assert.Equal(state.Units.Select(u => u.Id), result.State.Units.Select(u => u.Id));
        Assert.Equal(random.Calls, result.State.RandomSamples.Count - state.RandomSamples.Count);
    }

    // UI-198: new event texts name cards and characters by title. Each title is checked against the card or guide the
    // event is about, then the id the reviewed bytes hold is put back.
    private static void CheckAndRestoreNamedTexts(MatchState before, MatchState historical)
    {
        foreach (var added in historical.Events.Skip(before.Events.Count))
        {
            if (added.Kind == "GuideChanged")
            {
                var guide = historical.Fellowship.Guide;
                Assert.Equal($"The Fellowship is guided by {CharacterCatalog.Title(guide)}.", added.Text);
                added.Text = $"The Fellowship is guided by {guide}.";
                continue;
            }
            var card = added.Activity?.Facts.Cards.FirstOrDefault(c => c.Id.Length > 0)?.Id;
            if (card != null && added.Kind is "CardDrawn" or "OwnExcessDiscard" or "EventPlayed")
            {
                Assert.Contains(CardCatalog.Title(card), added.Text, StringComparison.Ordinal);
                added.Text = added.Text.Replace(CardCatalog.Title(card), card, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void ChangingOnlyHiddenHandDeckOrderAndEntropyKeepsTheOtherRawProjectionIdentical(string name)
    {
        var state = Load(Case(name));
        foreach (var viewer in Enum.GetValues<Side>())
        {
            var changed = StateCodec.Copy(state);
            SwapHiddenCard(changed, TurnFlow.Other(viewer));
            foreach (var player in changed.Players)
                foreach (var deck in player.Decks.Values)
                    deck.Reverse();
            foreach (var sample in changed.RandomSamples)
                sample.Value = (sample.Value + 1) % sample.ExclusiveMaximum;
            var raw = Json(Views.For(state, viewer));
            Assert.Equal(raw, Json(Views.For(changed, viewer)));
            using var document = JsonDocument.Parse(raw);
            foreach (var forbidden in new[] { "randomSamples", "resolution", "pending", "huntTiles" })
                Assert.False(document.RootElement.TryGetProperty(forbidden, out _));
        }
    }

    [Fact]
    public void SecretCommitAndRingIntermediateStateHaveTheirSourceDefinedMeaning()
    {
        var battle = Load(Case("secret-combat"));
        var fp = Views.For(battle, Side.FreePeoples);
        var sh = Views.For(battle, Side.Shadow);
        Assert.Equal("base.fp.strategy.02", fp.Battle!.Attacker.CardId);
        Assert.Null(sh.Battle!.Attacker.CardId);
        Assert.Equal("strategy", sh.Battle.Attacker.CardBack);
        Assert.Single(battle.Resolution.PaidDieIds);
        Assert.Equal(DecisionKind.CombatCard, sh.Decision!.Kind);
        var ring = Load(Case("ring-paid"));
        Assert.True(ring.Player(Side.FreePeoples).RingActionPending);
        Assert.True(ring.Player(Side.FreePeoples).RingUsedThisTurn);
        Assert.Equal(Side.Shadow, ring.Rings[0].Owner);
        Assert.Equal(DieFace.Muster, ring.Dice[0].Face);
        Assert.Equal(DieLocation.Available, ring.Dice[0].Location);
        Assert.Equal(Side.FreePeoples, ring.Pending!.Owner);
    }

    private static void SwapHiddenCard(MatchState state, Side owner)
    {
        var player = state.Player(owner);
        var index = player.Hand.FindIndex(id => id != player.CommittedCombatCard);
        if (index < 0)
            return;
        var deck = player.Decks[CardCatalog.Base[player.Hand[index]].Deck];
        (player.Hand[index], deck[0]) = (deck[0], player.Hand[index]);
    }

    private static void CheckAndRemoveNewHuntReport(MatchState original, MatchState continued)
    {
        Assert.Null(original.HuntReport);
        Assert.Equal(0, original.HuntReportSequence);
        Assert.All(original.Resolution.Frames.Where(f => f.Hunt is not null), f => Assert.Equal(0, f.Hunt!.ReportIndex));
        var report = Assert.IsType<HuntReportView>(continued.HuntReport);
        Assert.Equal(1, continued.HuntReportSequence);
        Assert.Equal(continued.Revision, report.StartedRevision);
        var started = Assert.Single(report.Entries, e => e.Kind == HuntReportKind.Started);
        Assert.Equal("moria", started.SourceId);
        Assert.Equal(1, started.HuntId);
        Assert.DoesNotContain(report.Entries, e => e.Kind is HuntReportKind.Rolled or HuntReportKind.Rerolled);
        Assert.Single(report.Entries, e => e.Kind == HuntReportKind.TileDrawn);
        Assert.Equal(Json(Assert.IsType<HuntReportView>(Views.For(continued, Side.FreePeoples).HuntReport)),
            Json(Assert.IsType<HuntReportView>(Views.For(continued, Side.Shadow).HuntReport)));
        continued.HuntReport = null;
        continued.HuntReportSequence = 0;
        foreach (var frame in continued.Resolution.Frames.Where(f => f.Hunt is not null))
            frame.Hunt!.ReportIndex = 0;
    }

    private static void CheckAndRemoveNewActionMetadata(MatchState original, MatchState continued, string name)
    {
        Assert.Null(original.Resolution.ActionLog);
        Assert.Null(continued.Resolution.ActionLog);
        Assert.All(original.Events, e => Assert.Null(e.Activity));
        var added = continued.Events.Skip(original.Events.Count).ToArray();
        var direct = Assert.Single(added, e => e.Activity?.Actor == Side.FreePeoples);
        var metadata = Assert.IsType<ActionEvent>(direct.Activity);
        Assert.True(metadata.Action.PartialHistory);
        Assert.Empty(metadata.Action.Costs);
        Assert.Equal(ActionLogStatus.Completed, metadata.Status);
        if (name == "strategy-march")
        {
            Assert.Equal(CommandKind.MoveArmies, metadata.Command);
            var movement = Assert.Single(metadata.Movements);
            Assert.Equal("north-ithilien", movement.From);
            Assert.Equal("dagorlad", movement.To);
            Assert.Equal(2, movement.Participants.Count);
            Assert.Single(movement.Participants, p => p.UnitKind == UnitKind.Regular && p.NationId == "gondor");
            Assert.Single(movement.Participants, p => p.DefinitionId == "base.character.gimli");
        }
        else
        {
            Assert.Equal(CommandKind.FinishActionSequence, metadata.Command);
            Assert.Equal("ActionFinished", direct.Kind);
            Assert.Empty(metadata.Movements);
            // This explicit Finish fact did not exist in the reviewed pre-journal result.
            continued.Events.Remove(direct);
        }
        foreach (var item in added)
        {
            Assert.NotNull(item.Activity);
            item.Activity = null;
        }
    }

    internal static void CheckAndRemoveJournalAdditions(MatchState original, MatchState continued, GameCommand command)
    {
        var added = continued.Events.Skip(original.Events.Count).ToArray();
        if (command.Kind is not (CommandKind.MoveArmies or CommandKind.FinishActionSequence))
        {
            Assert.All(added, e => Assert.NotNull(e.Activity));
            Assert.Contains(added, e => e.Activity?.Command == command.Kind);
        }
        Assert.All(added, e => Assert.Matches("^[a-f0-9]{64}$", e.HistoryId!));
        Assert.Equal(added.Length, added.Select(e => e.HistoryId).Distinct().Count());
        foreach (var legacy in added.Where(e => e.Kind == "LegacyBattleRound"))
        {
            Assert.True(legacy.Activity!.Action.PartialHistory);
            Assert.Equal(ActionLogStatus.Unknown, legacy.Activity.Status);
            Assert.Equal(original.LastBattleRound!.Id, legacy.Activity.Facts.Battle!.Id);
            Assert.Equal(original.LastBattleRound.Round, legacy.Activity.Facts.Battle.Round);
            Assert.Empty(legacy.Activity.Costs);
        }
        continued.Events.RemoveAll(e => e.Revision > original.Revision && e.Kind is "ActionDecision" or
            "BattleRoundCompleted" or "HuntFact" or "HuntChainCompleted" or "CardsDrawn" or "OwnExcessDiscard" or
            "OwnEffectChoice" or "OwnCorruptionTile" or "OwnProtectionCost" or "LegacyBattleRound");
        foreach (var item in added)
        {
            item.Activity = null;
            item.HistoryId = null;
        }
        continued.Resolution.ActionLog = null;
    }
}
