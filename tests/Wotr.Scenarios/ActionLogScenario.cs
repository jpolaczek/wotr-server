using Wotr.Contracts;
using Wotr.Domain;

internal static class ActionLogScenario
{
    public static Dictionary<string, MatchState> Create(IRandomSource random)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
        TurnFlow.Start(state);
        var engine = new GameEngine(new TurnCommands(), new FellowshipCommands(), new DeclineReactionHandler());
        // Real accepted decisions, including normal turn recovery and hidden hand-limit discards.
        while (state.Revision < 140)
        {
            var pending = state.Pending!;
            var payload = new CommandPayload();
            var kind = pending.Kind switch
            {
                DecisionKind.FellowshipPhase => CommandKind.ContinueFellowship,
                DecisionKind.AllocateHunt => CommandKind.AllocateHunt,
                DecisionKind.DiscardCards => CommandKind.DiscardCards,
                DecisionKind.Action => CommandKind.DiscardDie,
                _ => throw new InvalidOperationException("Unexpected log seed decision: " + pending.Kind)
            };
            if (kind == CommandKind.DiscardDie)
                payload.DieId = state.Dice.First(d => d.Owner == pending.Owner && d.Location == DieLocation.Available).Id;
            if (kind == CommandKind.DiscardCards)
                payload.Ids = state.Player(pending.Owner).Hand.Take(pending.RequiredCount).ToList();
            var command = new GameCommand { MatchId = state.Id, ExpectedRevision = state.Revision, DecisionId = pending.Id,
                CommandId = Guid.NewGuid().ToString("N"), Kind = kind, Payload = payload };
            var result = engine.Execute(state, pending.Owner, command, random);
            if (!result.Receipt.Accepted) throw new InvalidOperationException(result.Receipt.Error);
            state = result.State;
        }
        return new() { ["ui41-history"] = state };
    }
}
