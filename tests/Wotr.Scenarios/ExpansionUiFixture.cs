using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class ExpansionUiFixture
{
    private static readonly GameEngine Engine = new(new TurnCommands(), new LordsDiceCommands(), new EffectCommands(),
        new FellowshipCommands(), new CharacterCommands(), new HuntCommands(), new BattleCommands(), new StrongholdCommands(),
        new MusterCommands(), new MovementCommands(), new DeclineReactionHandler(), new KeeperRingCommands(),
        new LordsSetupCommands(), new CouncilCommands(), new SovereignCommands(), new CorruptionCommands(),
        new KingsDiceCommands(), new DismayCommands());

    public static MatchConfiguration Full(bool fate = true) => new() { Lords = true, Council = true, Kings = true, Fate = fate };

    public static MatchState Start(IRandomSource random, MatchConfiguration configuration)
    {
        var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random, configuration);
        TurnFlow.Start(state);
        return state;
    }

    public static MatchState Actions(IRandomSource random, MatchConfiguration? configuration = null)
    {
        var state = Start(random, configuration ?? Full());
        for (var step = 0; state.Pending?.Kind != DecisionKind.Action; step++)
        {
            if (step > 6)
                throw new InvalidOperationException("Expansion setup failed to reach actions.");
            state = state.Pending!.Kind switch
            {
                DecisionKind.CouncilVersions => Execute(state, CommandKind.ChooseCompanionVersions,
                    new() { GuideId = CharacterRules.Strider, Ids = [CharacterRules.Grey] }),
                DecisionKind.CouncilHomelands => Execute(state, CommandKind.ChooseCouncilHomelands),
                DecisionKind.FellowshipPhase => Execute(state, CommandKind.ContinueFellowship),
                DecisionKind.AllocateHunt => Execute(state, CommandKind.AllocateHunt, samples: new int[state.Dice.Count(d => d.Location == DieLocation.Unrolled)]),
                _ => throw new InvalidOperationException("Unexpected expansion setup decision.")
            };
        }
        return state;
    }

    public static MatchState Execute(MatchState state, CommandKind kind, CommandPayload? payload = null, params int[] samples)
    {
        var result = Engine.Execute(state, state.Pending!.Owner, new()
        {
            MatchId = state.Id, ExpectedRevision = state.Revision, DecisionId = state.Pending.Id,
            CommandId = Guid.NewGuid().ToString("N"), Kind = kind, Payload = payload ?? new()
        }, new FixedRandom(samples));
        if (!result.Receipt.Accepted)
            throw new InvalidOperationException($"Invalid expansion UI scenario {kind}: {result.Receipt.Error}");
        return result.State;
    }

    public static CharacterState Put(MatchState state, string definition, string region)
    {
        var identity = CharacterCatalog.All[definition].Identity;
        var figure = CharacterCatalog.Base.Values.FirstOrDefault(c => c.Identity == identity && c.Initial)?.Id ?? definition;
        var character = state.Characters.Find(c => c.Id == figure) ?? new CharacterState { Id = figure };
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.All[definition]);
        character.Location = region;
        if (!state.Characters.Contains(character))
            state.Characters.Add(character);
        state.Fellowship.Companions.Remove(figure);
        LordsCompanionSetup.Reveal(state, character);
        return character;
    }

    public static DieState Die(MatchState state, Side side) => state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available);
    public static SovereignState Sovereign(MatchState state, string id) => state.Kings!.Sovereigns.Single(s => s.Id == id);

    private sealed class FixedRandom(int[] samples) : IRandomSource
    {
        private readonly Queue<int> values = new(samples);
        public int Next(int upperBound) => values.Dequeue();
    }
}
