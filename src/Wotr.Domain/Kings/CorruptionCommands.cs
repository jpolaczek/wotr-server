using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class CorruptionCommands : ICommandHandler, IActionOptionsProvider, ICommandPreviewProvider
{
    public bool Handles(CommandKind kind) => kind is CommandKind.AttemptCorruption or CommandKind.DrawCorruptionTile or CommandKind.ChooseCorruptionTile;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        if (!state.Configuration.Kings || state.Kings is null || actor != Side.Shadow || command.Payload.RingId != "")
            return "IllegalChoice";
        var p = command.Payload;
        var attempt = CorruptionRuntime.Current(state);
        var valid = command.Kind switch
        {
            CommandKind.AttemptCorruption => state.Phase == GamePhase.ActionResolution && state.Pending?.Kind == DecisionKind.Action &&
                CorruptionRules.CanAttempt(state) && CorruptionRules.Targets(state).Any(s => s.Id == p.CharacterId) &&
                CorruptionRules.Eyes(state).Any(d => d.Id == p.DieId),
            CommandKind.DrawCorruptionTile => p.DieId == "" && state.Pending?.Kind == DecisionKind.CorruptionTile &&
                attempt is not null && CorruptionRules.CanDraw(attempt),
            CommandKind.ChooseCorruptionTile => p.DieId == "" && state.Pending?.Kind == DecisionKind.CorruptionTile &&
                attempt is not null && DraftSelection.IsSubset(p.Ids, CorruptionRules.Choices(attempt), 1, 1),
            _ => false
        };
        return valid ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        switch (command.Kind)
        {
            case CommandKind.AttemptCorruption:
                CorruptionRuntime.Begin(state, command.Payload.CharacterId, command.Payload.DieId, random);
                break;
            case CommandKind.DrawCorruptionTile:
                CorruptionRuntime.Draw(state, random);
                break;
            case CommandKind.ChooseCorruptionTile:
                CorruptionRuntime.Choose(state, command.Payload.Ids.Single());
                break;
        }
    }

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (!state.Configuration.Kings || state.Kings is null || actor != Side.Shadow || state.Pending?.Owner != actor)
            yield break;
        if (state.Pending.Kind == DecisionKind.Action && CorruptionRules.CanAttempt(state))
            yield return new() { Kind = CommandKind.AttemptCorruption, Minimum = 1, Maximum = 1,
                EligibleIds = CorruptionRules.Targets(state).Select(s => s.Id).ToList(),
                EligibleDice = CorruptionRules.Eyes(state).Select(d => d.Id).ToList() };
        if (state.Pending.Kind == DecisionKind.CorruptionTile && CorruptionRuntime.Current(state) is { } attempt)
        {
            if (CorruptionRules.CanDraw(attempt))
                yield return new() { Kind = CommandKind.DrawCorruptionTile };
            var choices = CorruptionRules.Choices(attempt);
            if (choices.Count > 0)
                yield return new() { Kind = CommandKind.ChooseCorruptionTile, EligibleIds = [.. choices], Minimum = 1, Maximum = 1 };
        }
    }

    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview)
    {
        preview.HasUnknownResult = command.Kind != CommandKind.ChooseCorruptionTile;
        preview.Summary = command.Kind switch
        {
            CommandKind.AttemptCorruption => $"Spend this Eye and draw up to {Math.Min(3, CorruptionRules.Eyes(state).Count() - 1)} Corruption tiles for {SovereignCatalog.All[command.Payload.CharacterId].Title}.",
            CommandKind.DrawCorruptionTile => "Draw the next Corruption tile.",
            _ => state.Configuration.VisibleCorruption
                ? "Place this tile face up; reveal and return the others."
                : "Place this tile face down; reveal and return the others."
        };
    }
}
