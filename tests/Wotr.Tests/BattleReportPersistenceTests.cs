using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;
using static Wotr.Tests.BattleFixture;
using static Wotr.Tests.ActionLogFamilyTests;

namespace Wotr.Tests;

public class BattleReportPersistenceTests
{
    [PostgresFact]
    public async Task HttpReportSurvivesRestartCardsLossesAdvanceAndRetryThenAnotherBattleAtSameRegion()
    {
        var state = State();
        state.Id = Guid.NewGuid().ToString("N");
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        var defender = Put(state, "sauron", UnitKind.Regular, Target);
        var reserve = Put(state, "sauron", UnitKind.Regular, "osgiliath");
        state = NoCards(Begin(state, [attacker.Id]), 1, 1);
        var firstId = BattleRuntime.Active(state)!.Id;
        GameCommand continueCommand;
        await using (var host = new TestHost(new SequenceRandom()))
        {
            await host.Services.GetRequiredService<MatchStore>().Create(state, "alice", "bob");
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            continueCommand = await SendChoice(alice, state.Id, CommandKind.ContinueBattle);
            await SendChoice(bob, state.Id, CommandKind.HoldBattle);
            await SendChoice(alice, state.Id, CommandKind.CommitCombatCard);
        }
        GameCommand revealed;
        await using (var host = new TestHost(new SequenceRandom(5, 0)))
        {
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            await CorrespondenceFixture.Send(alice, continueCommand);
            var restored = await Report(alice, state.Id, firstId);
            Assert.Single(restored.Rounds);
            Assert.Equal(2, restored.Latest!.Round);
            revealed = await SendChoice(bob, state.Id, CommandKind.CommitCombatCard);
            Assert.Equal(2, (await Report(bob, state.Id, firstId)).Rounds.Count);
        }
        GameCommand losses;
        await using (var host = new TestHost(new SequenceRandom()))
        {
            using var bob = host.Client("bob");
            await CorrespondenceFixture.Send(bob, revealed);
            losses = await SendChoice(bob, state.Id, CommandKind.TakeBattleLosses, new() { Losses = [Loss(defender.Id)] });
            Assert.Equal(BattleReportStatus.InProgress, (await Report(bob, state.Id, firstId)).Status);
        }
        GameCommand stay;
        string firstReport;
        await using (var host = new TestHost(new SequenceRandom()))
        {
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            await CorrespondenceFixture.Send(bob, losses);
            stay = await SendChoice(alice, state.Id, CommandKind.AdvanceBattle);
            var report = await Report(alice, state.Id, firstId);
            Assert.Equal(BattleReportStatus.Completed, report.Status);
            Assert.Equal(new[] { 1, 2 }, report.Rounds.Select(r => r.Number));
            Assert.Single(report.Costs, c => c.Kind == ActionCostKind.Die);
            Assert.Equal(2, report.Steps.Count(s => s.Kind == "BattleRoundCompleted"));
            Assert.Contains(report.Steps, s => s.Facts.LossDetails.Any(l => l.Figure.Id == defender.Id));
            Assert.Equal(Json(report), Json(await Report(bob, state.Id, firstId)));
            firstReport = Json(report);
            if (Environment.GetEnvironmentVariable("WOTR_BATTLE_REPORT_WIRE_PATH") is { Length: > 0 } wirePath)
                await File.WriteAllTextAsync(wirePath, System.Text.Json.JsonSerializer.Serialize(report, BoardCatalog.Json));
            var view = await View(bob, state.Id);
            await SendChoice(bob, state.Id, CommandKind.MoveArmies, new()
            {
                DieId = view.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id,
                Moves = [new() { From = "osgiliath", To = Target, UnitIds = [reserve.Id] }]
            });
        }
        await using (var host = new TestHost(new SequenceRandom(0, 0)))
        {
            using var alice = host.Client("alice");
            using var bob = host.Client("bob");
            await CorrespondenceFixture.Send(alice, stay);
            var view = await View(alice, state.Id);
            await SendChoice(alice, state.Id, CommandKind.AttackArmy, new()
            {
                DieId = view.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id,
                MovementCost = DieFace.Army, Moves = [new() { From = Origin, To = Target, UnitIds = [attacker.Id] }]
            });
            var secondId = (await View(alice, state.Id)).Battle!.Id;
            Assert.NotEqual(firstId, secondId);
            await SendChoice(alice, state.Id, CommandKind.CommitCombatCard);
            await SendChoice(bob, state.Id, CommandKind.CommitCombatCard);
            await SendChoice(alice, state.Id, CommandKind.StopBattle);
            Assert.Single((await Report(alice, state.Id, secondId)).Rounds);
            Assert.Equal(firstReport, Json(await Report(alice, state.Id, firstId)));
        }
    }

    [PostgresFact]
    public async Task AuthorizedReportAndLegacyReadFallbackSurviveLazyIndexAndNeverCrossMatches()
    {
        await using var fixture = await CorrespondenceFixture.Create();
        var state = State();
        state.Id = Guid.NewGuid().ToString("N");
        var attacker = Put(state, "gondor", UnitKind.Regular, Origin);
        Put(state, "sauron", UnitKind.Regular, Target);
        state = NoCards(Begin(state, [attacker.Id]), 1, 1);
        state = Execute(state, CommandKind.StopBattle);
        var id = state.LastBattleRound!.Id;
        foreach (var item in state.Events)
            item.Activity = null;
        await fixture.Save(state);
        await using (var connection = await fixture.Database.Source.OpenConnectionAsync())
        {
            await using var legacy = Database.Command(connection, null,
                "UPDATE matches SET events_indexed=false WHERE id=$1", state.Id);
            await legacy.ExecuteNonQueryAsync();
        }
        var report = await Report(fixture.Fp, state.Id, id);
        Assert.True(report.Legacy);
        Assert.True(report.PartialHistory);
        Assert.Null(report.Initiator);
        Assert.Null(report.Turn);
        Assert.Empty(report.Costs);
        Assert.Equal(BattleReportStatus.Unknown, report.Status);
        using var anonymous = fixture.Anonymous();
        using var forbidden = await fixture.Other.GetAsync(Path(state.Id, id));
        using var unauthenticated = await anonymous.GetAsync(Path(state.Id, id));
        using var absent = await fixture.Fp.GetAsync(Path(state.Id, "unknown"));
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
        var other = State();
        other.Id = Guid.NewGuid().ToString("N");
        await fixture.Save(other);
        using var wrongMatch = await fixture.Fp.GetAsync(Path(other.Id, id));
        Assert.Equal(HttpStatusCode.NotFound, wrongMatch.StatusCode);
        var view = await View(fixture.Sh, state.Id);
        var command = await SendChoice(fixture.Sh, state.Id, CommandKind.DiscardDie,
            new() { DieId = view.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id });
        await fixture.Restart();
        var preserved = await Report(fixture.Sh, state.Id, id);
        Assert.True(preserved.Legacy);
        Assert.Equal(Json(report.Latest), Json(preserved.Latest));
        await CorrespondenceFixture.Send(fixture.Sh, command);
        Assert.Equal(Json(preserved), Json(await Report(fixture.Fp, state.Id, id)));
        var restored = (await fixture.Matches.Read(state.Id, fixture.FreePeoples.AccountId))!.State;
        Assert.Equal(state.RandomSamples.Count, restored.RandomSamples.Count);
        Assert.Single(restored.Events, e => e.Kind == "LegacyBattleRound");
    }

    internal static string Path(string match, string battle) => $"/api/matches/{match}/battles/{Uri.EscapeDataString(battle)}";
    internal static Task<BattleReportView> Report(HttpClient client, string match, string battle) =>
        CorrespondenceFixture.Read<BattleReportView>(client, Path(match, battle));
    private static Task<PlayerView> View(HttpClient client, string match) =>
        CorrespondenceFixture.Read<PlayerView>(client, $"/api/matches/{match}");

    private static async Task<GameCommand> SendChoice(HttpClient client, string match, CommandKind kind, CommandPayload? payload = null)
    {
        var view = await View(client, match);
        var command = new GameCommand
        {
            MatchId = match, ExpectedRevision = view.Revision, DecisionId = view.Decision!.Id,
            CommandId = Guid.NewGuid().ToString("N"), Kind = kind, Payload = payload ?? new()
        };
        await CorrespondenceFixture.Send(client, command);
        return command;
    }
}
