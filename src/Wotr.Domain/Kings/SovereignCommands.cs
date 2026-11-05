using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class SovereignCommands : ICommandHandler, IActionOptionsProvider, ICommandPreviewProvider
{
    public bool Handles(CommandKind kind) => kind is CommandKind.AwakenSovereign or CommandKind.RemoveCorruptedSovereign;

    public string? Validate(MatchState state, Side actor, GameCommand command)
    {
        if (!state.Configuration.Kings || state.Kings is null || actor != Side.FreePeoples ||
            state.Phase != GamePhase.ActionResolution || state.Pending?.Kind != DecisionKind.Action || command.Payload.RingId != "")
            return "IllegalChoice";
        var p = command.Payload;
        var valid = command.Kind == CommandKind.AwakenSovereign ? SovereignAwakening.Valid(state, p) :
            SovereignAwakening.CanRemove(state, p.CharacterId) &&
            DiceRules.AvailableDie(state, actor, p.DieId) is { } die && DiceRules.CanPay(die, DieFace.Character);
        return valid ? null : "IllegalChoice";
    }

    public void Apply(MatchState state, Side actor, GameCommand command, IRandomSource random)
    {
        var p = command.Payload;
        var sovereign = SovereignLifecycle.Find(state, p.CharacterId)!;
        if (command.Kind == CommandKind.AwakenSovereign)
        {
            var die = DiceRules.AvailableDie(state, actor, p.DieId)!;
            DiceRules.Pay(state, actor, p.DieId, die.Source == SovereignCatalog.RulerSource ? die.Face : DieFace.Muster);
            SovereignLifecycle.Awaken(state, sovereign, p.Route[^1]);
        }
        else
        {
            DiceRules.Pay(state, actor, p.DieId, DieFace.Character);
            SovereignLifecycle.Remove(state, sovereign);
        }
        TurnFlow.FinishAction(state, actor);
    }

    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (!state.Configuration.Kings || state.Kings is null || actor != Side.FreePeoples ||
            state.Pending is not { Kind: DecisionKind.Action, Owner: Side.FreePeoples })
            yield break;
        foreach (var sovereign in state.Kings.Sovereigns)
        {
            var awakening = sovereign.Status == SovereignStatus.Initial;
            var targets = awakening ? SovereignAwakening.Destinations(state, sovereign) :
                SovereignAwakening.CanRemove(state, sovereign.Id) ? [SovereignLifecycle.Region(state, sovereign)] : new List<string>();
            var dice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available &&
                (awakening ? SovereignAwakening.CanPay(state, d) : DiceRules.CanPay(d, DieFace.Character))).Select(d => d.Id).ToList();
            if (targets.Count == 0 || dice.Count == 0)
                continue;
            yield return new()
            {
                Kind = awakening ? CommandKind.AwakenSovereign : CommandKind.RemoveCorruptedSovereign,
                EligibleIds = [sovereign.Id], EligibleDice = dice, Targets = targets,
                Travel = awakening ? [AwakeningTravel(state, sovereign, targets)] : [],
                Minimum = 1, Maximum = 1,
                CharacterActions = [new() { CharacterId = sovereign.Id, Title = SovereignCatalog.All[sovereign.Id].Title,
                    Targets = targets, Dice = dice }]
            };
        }
    }

    private static TravelOption AwakeningTravel(MatchState state, SovereignState sovereign, List<string> targets) => new()
    {
        Id = sovereign.Id,
        Origin = SovereignLifecycle.Region(state, sovereign),
        Range = SovereignCatalog.All[sovereign.Id].Level,
        Targets = targets,
        StopRegions = BoardRules.Regions.Keys.Where(r => TravelRules.ShadowStronghold(state, r)).ToList(),
        BlockedRegions = BoardRules.Regions.Keys.Where(r => BoardRules.Besieged(state, r, Side.FreePeoples)).ToList()
    };

    public void DescribePreview(MatchState state, GameCommand command, ActionPreview preview)
    {
        var p = command.Payload;
        var title = SovereignCatalog.All[p.CharacterId].Title;
        if (command.Kind == CommandKind.AwakenSovereign)
        {
            preview.Summary = $"Awaken {title} in {BoardRules.Regions[p.Route[^1]].Name}; return all his Corruption tiles.";
            preview.Placements.Add(new() { UnitId = p.CharacterId, RegionId = p.Route[^1] });
        }
        else
        {
            preview.Summary = $"Remove the Corrupted {title} and end his weaknesses.";
            preview.RemovedIds.Add(p.CharacterId);
        }
    }
}
