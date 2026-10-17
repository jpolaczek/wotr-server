using Wotr.Contracts;

namespace Wotr.Domain;

internal static class MovementFacts
{
    public static ActionEvent Event(MatchState state, ActionMovementKind kind, string from, string to,
        IEnumerable<UnitState> units, IEnumerable<CharacterState> characters, IReadOnlyList<string>? route = null)
    {
        var result = HistoryFacts.At(state, kind == ActionMovementKind.Travel ? ActionContextKind.Characters : ActionContextKind.Army);
        result.Context.Regions = new[] { from, to }.Where(BoardRules.Regions.ContainsKey).Distinct().ToList();
        result.Movements.Add(new()
        {
            Kind = kind, From = from, To = to, Route = Route(from, to, route),
            Participants = units.Select(Unit).Concat(characters.Select(Character)).ToList()
        });
        return result;
    }

    private static List<string> Route(string from, string to, IReadOnlyList<string>? route) =>
        route is { Count: > 0 } ? route[0] == from ? [.. route] : [from, .. route] : from == to ? [from] : [from, to];

    public static List<ActionMovement> Capture(MatchState before, CommandPayload payload)
    {
        var units = before.Units.ToDictionary(u => u.Id);
        var characters = before.Characters.ToDictionary(c => c.Id);
        return payload.Moves.Select(move => new ActionMovement
        {
            Kind = ActionMovementKind.March,
            From = move.From,
            To = move.To,
            Route = Route(move.From, move.To, move.Route.Count > 0 ? move.Route : payload.Route),
            Participants = move.UnitIds.Select(id => Unit(units[id]))
                .Concat(move.CharacterIds.Select(id => Character(characters[id]))).ToList()
        }).ToList();
    }

    internal static ActionParticipant Unit(UnitState unit) => new()
    {
        Id = unit.Id, Side = BoardRules.Owner(unit), NationId = unit.Nation,
        UnitKind = unit.Kind, Garrison = unit.Garrison
    };

    internal static ActionParticipant Character(CharacterState character) => new()
    {
        Id = character.Id, Side = character.Side, DefinitionId = character.DefinitionId,
        NationId = character.NationIcon, Garrison = character.Garrison,
        Level = character.Level, Leadership = character.Leadership
    };
}
