using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class KeeperRingCommands : ICommandHandler, IActionOptionsProvider, ICommandPreviewProvider
{
    public bool Handles(CommandKind kind) => kind is CommandKind.UseNarya or CommandKind.UseVilya or CommandKind.DeclineVilya;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        if (actor != Side.FreePeoples || !state.Configuration.Lords || state.Lords is null || state.Phase != GamePhase.ActionResolution)
            return "IllegalChoice";
        var payload = command.Payload;
        return command.Kind switch
        {
            CommandKind.UseNarya => state.Pending?.Kind == DecisionKind.Action &&
                payload.RingId == LordsDiceSetup.NaryaRing && KeeperRings.NaryaNation(state) is { } nation &&
                payload.NationId == nation && DiceRules.AvailableDie(state, actor, payload.DieId) is not null ? null : "IllegalChoice",
            CommandKind.UseVilya => state.Pending?.Kind == DecisionKind.KeepActionDie && state.Lords.ActionCompletion is not null &&
                payload.RingId == LordsDiceSetup.VilyaRing && VilyaCompletion.EligibleDice(state).Contains(payload.DieId) ? null : "IllegalChoice",
            CommandKind.DeclineVilya => state.Pending?.Kind == DecisionKind.KeepActionDie && state.Lords.ActionCompletion is not null ? null : "IllegalChoice",
            _ => "IllegalChoice"
        };
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        if (command.Kind == CommandKind.UseNarya)
            KeeperRings.UseNarya(state, command.Payload.DieId);
        else
            VilyaCompletion.Complete(state, command.Kind == CommandKind.UseVilya ? command.Payload.DieId : null);
    }

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending?.Owner != actor || actor != Side.FreePeoples)
            yield break;
        if (state.Pending.Kind == DecisionKind.Action && KeeperRings.NaryaNation(state) is { } nation)
            yield return new()
            {
                Kind = CommandKind.UseNarya,
                EligibleIds = [LordsDiceSetup.NaryaRing],
                EligibleDice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available).Select(d => d.Id).ToList(),
                Targets = [nation]
            };
        if (state.Pending.Kind == DecisionKind.KeepActionDie)
        {
            yield return new()
            {
                Kind = CommandKind.UseVilya,
                EligibleIds = [LordsDiceSetup.VilyaRing],
                EligibleDice = VilyaCompletion.EligibleDice(state),
                Minimum = 1,
                Maximum = 1
            };
            yield return new() { Kind = CommandKind.DeclineVilya };
        }
    }

    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview)
    {
        if (command.Kind == CommandKind.UseNarya)
        {
            preview.Summary = "Use Narya and this die to activate the settlement's nation and bring it to war.";
            preview.PoliticsChanges.Add(new() { Id = KeeperRings.NaryaNation(state)!, Active = true, StepsToWar = 0 });
        }
        else
        {
            preview.Costs = command.Kind == CommandKind.UseVilya ? [LordsDiceSetup.VilyaRing] : [];
            preview.Summary = command.Kind == CommandKind.UseVilya
                ? "Use Vilya to keep the die available, then finish this action."
                : "Finish this action and spend the die normally.";
        }
    }
}
