using Microsoft.Extensions.Configuration;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;

// Seeder-only preparation. The running host later executes every CPU command through its ordinary scheduler.
internal static class PveScenarios
{
    public static async Task<Dictionary<string, string>> Create(Database database, SessionView fp, SessionView sh)
    {
        var result = new Dictionary<string, string>();
        if (Environment.GetEnvironmentVariable("WOTR_CLIENT_PVE_FIXTURE") != "1") return result;
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var host = BotHostOptions.Read(configuration);
        var access = BotAccessOptions.Read(configuration);
        var roomOptions = BotRoomOptions.Read(configuration);
        using var runtime = EmbeddedBotRuntime.LoadConfigured(host);
        var capabilities = new BotRuntimeCapabilities(host, roomOptions, access, runtime);
        if (capabilities.Unavailable(new()) is { } unavailable)
            throw new InvalidOperationException("Client CPU fixture: " + unavailable);
        var rooms = new RoomStore(database, new SeededRandom(13), bots: access, botRooms: roomOptions, runtime: capabilities, botHost: host);
        var jobs = new BotJobStore(database, host, EmptyRoomOptions.Read(configuration));
        foreach (var (key, scenarioId, mode) in new[]
        {
            ("shadow-move", "sh-army-move", "move"), ("free-move", "fp-army-move", "move"),
            ("recovery", "sh-army-move", "move"), ("reduced", "fp-army-move", "move"), ("skip", "sh-army-move", "move"),
            ("recruit-card", "sh-muster-event", "card"), ("attack", "sh-army-attack", "attack"),
            ("combat", "card-base.sh.strategy.01-combat", "combat"),
            ("hunt", "fp-character-move-fellowship", "hunt")
        })
        {
            var scenario = TestScenarioCatalog.Find(scenarioId) ?? throw new InvalidOperationException(scenarioId);
            var humanSide = TurnFlow.Other(scenario.StartingSide);
            var human = humanSide == Side.FreePeoples ? fp : sh;
            var room = await rooms.Create(human.AccountId, new CreateRoomRequest { Name = "CPU fixture " + key, Side = humanSide });
            var reserved = await rooms.AddBot(room.Id, human.AccountId, new()
            { RequestId = Guid.NewGuid().ToString("N"), ExpectedVersion = room.Version });
            room = reserved.Room ?? throw new InvalidOperationException(reserved.Error);
            var work = await jobs.Claim("client-fixture-seed") ?? throw new InvalidOperationException("No fixture job.");
            if (work.RoomId != room.Id || !await rooms.ReadyRuntime(work))
                throw new InvalidOperationException("Wrong fixture acknowledgement.");
            await rooms.SetReady(room.Id, human.AccountId, new() { ExpectedVersion = room.Version, Ready = true });
            var started = await rooms.Start(room.Id, human.AccountId, room.Version);
            var match = started.Room?.MatchId ?? throw new InvalidOperationException(started.Error);
            var state = scenario.Create(match);
            await using var connection = await database.Source.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using (var replace = Database.Command(connection, transaction,
                "UPDATE matches SET state=$2::jsonb,revision=$3,events_indexed=false,recorded=false WHERE id=$1",
                match, StateCodec.Write(state), state.Revision))
                await replace.ExecuteNonQueryAsync();
            await using (var hold = Database.Command(connection, transaction,
                "UPDATE bot_runtime_jobs SET available_at='infinity',memory=$2 WHERE assignment_id=$1", work.AssignmentId, mode))
                await hold.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            result.Add(key, match);
        }
        return result;
    }

    public static async Task Release(Database database, string match)
    {
        // Test CLI only, guarded by Program's isolated _test database check. No runtime or player endpoint.
        await using var connection = await database.Source.OpenConnectionAsync();
        await using var command = Database.Command(connection, null, """
            UPDATE bot_runtime_jobs j SET available_at=now() FROM bot_assignments a
            WHERE j.assignment_id=a.id AND a.match_id=$1 AND a.policy_version='client-fixture-13'
                AND j.available_at='infinity'
            """, match);
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException("Expected one held client fixture.");
    }
}
