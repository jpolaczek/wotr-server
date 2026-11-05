using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// RANK-03 (R35, docs/RANKED_PLAY.md): a concession and a timeout end the game at any decision, as a loss for the
// sender. The engine holds no clock: the server issues TimeOut for the decision whose deadline passed, and only
// ranked matches send either (the ordinary commands endpoint refuses both).
public sealed class GameEndingCommands : ICommandHandler
{
    private static readonly string EmptyPayload = JsonSerializer.Serialize(new CommandPayload(), BoardCatalog.Json);

    public bool Handles(CommandKind kind) => kind is CommandKind.Concede or CommandKind.TimeOut;

    // Nothing rides on the ending: a token or choice in the payload would otherwise be spent before the game ends.
    public string? Validate(MatchState state, Side actor, GameCommand command) =>
        JsonSerializer.Serialize(command.Payload, BoardCatalog.Json) == EmptyPayload ? null : "IllegalChoice";

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random) =>
        GameEnding.Finish(state, TurnFlow.Other(actor),
            command.Kind == CommandKind.Concede ? VictoryCondition.Conceded : VictoryCondition.TimedOut);
}
