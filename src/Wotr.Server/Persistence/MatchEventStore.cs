using System.Text.Json;
using Npgsql;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Server;

internal static class MatchEventStore
{
    public static async Task Append(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        MatchState state, int first, CancellationToken cancellation)
    {
        if (first == state.Events.Count)
            return;
        await using var batch = new NpgsqlBatch(connection, transaction);
        for (var i = first; i < state.Events.Count; i++)
        {
            var item = state.Events[i];
            var command = new NpgsqlBatchCommand("""
                INSERT INTO match_events(match_id,revision,ordinal,event,position,event_id,action_id)
                VALUES ($1,$2,$3,$4::jsonb,$5,$6,$7)
                """);
            foreach (var value in new object[] { state.Id, item.Revision, i, JsonSerializer.Serialize(item, BoardCatalog.Json),
                (long)i + 1, EventIdentity.For(state.Id, i + 1, item), item.Activity?.Action.Id ?? EventIdentity.For(state.Id, i + 1, item) })
                command.Parameters.Add(new NpgsqlParameter { Value = value });
            batch.BatchCommands.Add(command);
        }
        await batch.ExecuteNonQueryAsync(cancellation);
    }
}
