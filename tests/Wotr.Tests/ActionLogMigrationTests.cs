using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class ActionLogMigrationTests
{
    [PostgresFact]
    public async Task OldDatabaseKeepsItsSnapshotAndCompleteLegacyHistoryDuringReadOnlyMigrationFallback()
    {
        var schema = "action_migration_" + Guid.NewGuid().ToString("N");
        await using var control = new NpgsqlConnection(PostgresTests.Connection);
        await control.OpenAsync();
        await Sql(control, $"CREATE SCHEMA {schema}");
        try
        {
            var settings = new NpgsqlConnectionStringBuilder(PostgresTests.Connection) { SearchPath = schema };
            await using var source = NpgsqlDataSource.Create(settings.ConnectionString);
            await using var connection = await source.OpenConnectionAsync();
            await InstallPreviousSchema(connection);
            var state = ActionLogPagingTests.LongHistory();
            state.Resolution.ActionLog = null;
            foreach (var item in state.Events)
                item.Activity = null;
            await using (var insert = Database.Command(connection, null,
                "INSERT INTO matches(id,fp_account,sh_account,revision,state) VALUES ($1,'alice','bob',$2,$3::jsonb)",
                state.Id, state.Revision, StateCodec.Write(state)))
                await insert.ExecuteNonQueryAsync();
            // The old table could omit initial snapshot events; preserve it until the next accepted write.
            await using (var insert = Database.Command(connection, null,
                "INSERT INTO match_events(match_id,revision,ordinal,event) VALUES ($1,$2,0,$3::jsonb)",
                state.Id, state.Revision, "{\"revision\":122,\"kind\":\"Legacy\",\"text\":\"Uninterpreted historical text.\"}"))
                await insert.ExecuteNonQueryAsync();
            var database = new Database(source);
            await database.Migrate();
            await database.Migrate();
            var store = new MatchStore(database, LordsFixture.Engine(), new SequenceRandom());
            var journal = new ActionLogStore(database);
            var page = (await journal.Page(state.Id, "alice", ActionLogFilter.All, 50))!;
            Assert.Equal(50, page.Steps.Count);
            Assert.All(page.Actions, a => { Assert.True(a.Legacy); Assert.Null(a.Initiator); Assert.Empty(a.Costs); });
            CorrespondenceFixture.SameState(state, (await store.Read(state.Id, "alice"))!.State);
            await using var count = new NpgsqlCommand("SELECT count(*) FROM match_events", connection);
            Assert.Equal(1L, await count.ExecuteScalarAsync());
            var finish = BattleFixture.Command(state, CommandKind.FinishActionSequence);
            Assert.True((await store.Execute(finish, "bob"))!.Accepted);
            Assert.Equal((long)state.Events.Count + 1, await count.ExecuteScalarAsync());
            var older = (await journal.Page(state.Id, "alice", ActionLogFilter.All, 50, before: page.LatestCursor))!;
            Assert.Equal(page.Steps.Skip(1).Select(s => s.Id), older.Steps.Take(49).Select(s => s.Id));
        }
        finally
        {
            await Sql(control, $"DROP SCHEMA {schema} CASCADE");
        }
    }

    private static async Task InstallPreviousSchema(NpgsqlConnection connection)
    {
        var assembly = typeof(Database).Assembly;
        var names = assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql", StringComparison.Ordinal)).Order().Take(3);
        foreach (var name in names)
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            await Sql(connection, await new StreamReader(stream).ReadToEndAsync());
        }
        await Sql(connection, "INSERT INTO schema_migrations(version) VALUES (1),(2),(3)");
    }

    private static async Task Sql(NpgsqlConnection connection, string text)
    {
        await using var command = new NpgsqlCommand(text, connection);
        await command.ExecuteNonQueryAsync();
    }
}
