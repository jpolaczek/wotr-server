using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class DismayCommands : ICommandHandler, IActionOptionsProvider, ICommandPreviewProvider
{
    public bool Handles(CommandKind kind) => kind is CommandKind.UseDismay or CommandKind.DeclineDismay or CommandKind.UseLoreBeforeActions;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        var p = command.Payload;
        if (!state.Configuration.Kings || !ConfigurationRules.DismayEnabled(state.Configuration) || actor != Side.Shadow ||
            state.Phase != GamePhase.ActionRoll || state.Pending?.Kind != DecisionKind.Dismay ||
            state.Kings is null || state.Kings.DismayDeclinedTurn == state.Turn || p.RingId != "")
            return "IllegalChoice";
        var valid = command.Kind switch
        {
            CommandKind.UseDismay => DraftSelection.IsSubset(p.Ids,
                KingsDismay.Tokens(state).Select(t => KingsRulerTokens.Id(t.SovereignId)).ToArray(), 1, 1) &&
                KingsDismay.Dice(state).Any(d => d.Id == p.DieId),
            CommandKind.DeclineDismay => p.DieId == "" && p.Ids.Count == 0,
            CommandKind.UseLoreBeforeActions => p.DieId == "" && p.Ids.Count == 0 && LoreOfTheRings.CanBegin(state),
            _ => false
        };
        return valid ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        if (command.Kind == CommandKind.UseLoreBeforeActions)
        {
            LoreOfTheRings.Begin(state);
            return;
        }
        if (command.Kind == CommandKind.UseDismay)
            KingsDismay.Use(state, command.Payload.Ids.Single(), command.Payload.DieId, random);
        else
            state.Kings!.DismayDeclinedTurn = state.Turn;
        TurnFlow.BeginActions(state);
    }

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending is not { Kind: DecisionKind.Dismay } pending || pending.Owner != actor)
            yield break;
        yield return new()
        {
            Kind = CommandKind.UseDismay, Minimum = 1, Maximum = 1,
            EligibleDice = KingsDismay.Dice(state).Select(d => d.Id).ToList(),
            EligibleIds = KingsDismay.Tokens(state).Select(t => KingsRulerTokens.Id(t.SovereignId)).ToList()
        };
        yield return new() { Kind = CommandKind.DeclineDismay };
        if (LoreOfTheRings.CanBegin(state))
            yield return new() { Kind = CommandKind.UseLoreBeforeActions };
    }

    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview)
    {
        preview.Costs = [.. command.Payload.Ids];
        preview.HasUnknownResult = command.Kind == CommandKind.UseDismay;
        preview.Summary = command.Kind switch
        {
            CommandKind.UseDismay => "Remove this Dismay token and re-roll the selected Ruler die.",
            CommandKind.UseLoreBeforeActions => "Resolve Lore of the Elven Rings before deciding about Dismay.",
            _ => "Keep your Dismay tokens for a future turn."
        };
    }
}
