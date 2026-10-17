using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain.Content;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class MenuMigrationTests
{
    [PostgresFact]
    public async Task ExistingRoomsBecomePrivateAndRetainSeatsCodesAndVersion()
    {
        await using var admin = NpgsqlDataSource.Create(PostgresTests.Connection);
        var schema = "menu_" + Guid.NewGuid().ToString("N");
        await using (var create = admin.CreateCommand("CREATE SCHEMA " + schema))
            await create.ExecuteNonQueryAsync();
        try
        {
            var settings = new NpgsqlConnectionStringBuilder(PostgresTests.Connection) { SearchPath = schema };
            await using var data = NpgsqlDataSource.Create(settings.ConnectionString);
            await using var connection = await data.OpenConnectionAsync();
            var assembly = typeof(Database).Assembly;
            foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql", StringComparison.Ordinal)).Order())
            {
                var version = int.Parse(name.Split('.')[^2].Split('_')[0], System.Globalization.CultureInfo.InvariantCulture);
                if (version >= 6)
                    continue;
                using var stream = assembly.GetManifestResourceStream(name)!;
                await using var ddl = new NpgsqlCommand(await new StreamReader(stream).ReadToEndAsync(), connection);
                await ddl.ExecuteNonQueryAsync();
                await using var record = Database.Command(connection, null, "INSERT INTO schema_migrations(version) VALUES ($1)", version);
                await record.ExecuteNonQueryAsync();
            }
            var database = new Database(data);
            var fp = await LegacyAccount(connection, "free_player");
            var sh = await LegacyAccount(connection, "shadow_player");
            var rooms = new[]
            {
                new RoomState { Id = "legacy_shadow", JoinCode = SessionStore.NewToken(), Shadow = sh.AccountId, ShadowName = sh.Username, Version = 7 },
                new RoomState { Id = "legacy_full", JoinCode = SessionStore.NewToken(), FreePeoples = fp.AccountId, FreePeoplesName = fp.Username, Shadow = sh.AccountId, ShadowName = sh.Username, Version = 9 }
            };
            foreach (var room in rooms)
            {
                await using var insert = Database.Command(connection, null,
                    "INSERT INTO rooms(id,join_code,fp_account,sh_account,state) VALUES ($1,$2,NULLIF($3,''),NULLIF($4,''),$5::jsonb)",
                    room.Id, room.JoinCode, room.FreePeoples, room.Shadow, JsonSerializer.Serialize(room, BoardCatalog.Json));
                await insert.ExecuteNonQueryAsync();
            }
            await database.Migrate();
            await database.Migrate();
            var store = new RoomStore(database, new ServerRandom());
            var lone = (await store.Read("legacy_shadow", sh.AccountId))!;
            Assert.True(lone.IsHost);
            Assert.Equal(RoomVisibility.Private, lone.Visibility);
            Assert.Equal(7, lone.Version);
            Assert.Equal(rooms[0].JoinCode, lone.JoinCode);
            var full = (await store.Read("legacy_full", fp.AccountId))!;
            Assert.True(full.IsHost);
            Assert.Equal(sh.Username, full.Shadow);
            Assert.Equal(9, full.Version);
            Assert.Empty((await store.ListPublic(sh.AccountId, "", 20)).Rooms);
        }
        finally
        {
            await using var drop = admin.CreateCommand("DROP SCHEMA " + schema + " CASCADE");
            await drop.ExecuteNonQueryAsync();
        }
    }

    // An account in the schema before 006 (and before AUTH-02's addresses), as the old server wrote it.
    private static async Task<(string AccountId, string Username)> LegacyAccount(NpgsqlConnection connection, string username)
    {
        var id = Guid.NewGuid().ToString("N");
        await using var insert = Database.Command(connection, null,
            "INSERT INTO accounts(id,username,normalized_name,password_hash,is_admin) VALUES ($1,$2,$3,'legacy',false)",
            id, username, username.ToUpperInvariant());
        await insert.ExecuteNonQueryAsync();
        return (id, username);
    }
}
