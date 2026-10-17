using Wotr.Contracts;
using Wotr.Domain;

// Fixed test positions are used only to create the reviewed regression corpus.
// They are never injected into the normal full-game acceptance scenario.
internal static class BaseCorpusSeeds
{
    internal static readonly GameEngine Engine = new(new DeclineReactionHandler(), new TurnCommands(), new MusterCommands(),
        new MovementCommands(), new EffectCommands(), new FellowshipCommands(), new CharacterCommands(),
        new HuntCommands(), new BattleCommands(), new StrongholdCommands());
    internal static readonly Projection Projection = new(new CoreOptions(), new TurnOptions(), new MusterOptions(),
        new MovementOptions(), new EffectOptions(), new FellowshipOptions(), new CharacterOptions(),
        new HuntOptions(), new BattleOptions(), new StrongholdCommands());

    public static Dictionary<string, MatchState> Create()
    {
        var random = new ZeroRandom();
        var states = BattleScenario.Create(random)
            .Concat(BaseTurnScenario.Create(random))
            .Concat(BaseBoundaryScenario.Create(random))
            .Concat(CharacterScenario.Create(random))
            .Concat(CardPackScenario.Create(random))
            .ToDictionary(p => p.Key, p => p.Value);
        states.Add("hunt-reroll", FellowshipScenario.Hunt(random));
        // Keep every Gondor regular deployed, without an unrelated illegal stack.
        foreach (var unit in states["no-regular"].Units.Where(u => u.Location == "pelargir").Skip(10))
            unit.Location = "dol-amroth";
        AddDerived(states);
        foreach (var (key, state) in states.ToArray())
        {
            var normalized = StateCodec.Write(state).Replace(state.Id, "corpus-" + key, StringComparison.Ordinal);
            states[key] = StateCodec.Read(normalized);
        }
        return states;
    }

    private static void AddDerived(Dictionary<string, MatchState> states)
    {
        states.Add("paid-army-step", PaidArmyStep());
        states.Add("allocation", Apply(states["turn-hunt-mandatory"], CommandKind.ContinueFellowship));
        var discard = StateCodec.Copy(states["turn-hunt-optional"]);
        foreach (var card in discard.Player(Side.FreePeoples).Decks["character"].Take(5).ToArray())
            CardPackScenario.Give(discard, card);
        TurnFlow.BeginTurn(discard);
        states.Add("discard", discard);
        states.Add("guide", Apply(states["boundary-random-companion"], CommandKind.ResolveHuntDamage,
            new() { HuntResponse = HuntResponse.Guide }));
        var hunt = Apply(states["hunt-reroll"], CommandKind.RerollHunt);
        states.Add("reveal", Apply(hunt, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }));
        var ring = states["turn-ring-politics"];
        states.Add("ring-paid", Apply(ring, CommandKind.UseRing, new()
        {
            DieId = ring.Dice[0].Id,
            RingId = ring.Rings[0].Id,
            Face = DieFace.Muster
        }));
        var combat = StateCodec.Copy(states["policy-fp"]);
        CardPackScenario.Give(combat, "base.fp.strategy.02");
        states.Add("secret-combat", Apply(combat, CommandKind.CommitCombatCard, new() { CardId = "base.fp.strategy.02" }));
        foreach (var (key, corruption) in new[] { ("ring-destroyed", 10), ("ring-corrupted", 11) })
        {
            var state = FellowshipScenario.Mordor(new ZeroRandom(), corruption);
            state = Apply(state, CommandKind.MoveFellowship, new() { DieId = state.Dice[0].Id });
            states.Add(key, Apply(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }));
        }
        foreach (var key in new[] { "fp", "sh" })
        {
            var state = states["boundary-military-" + key];
            for (var i = 0; i < 2; i++)
                state = Apply(state, CommandKind.DiscardDie, new()
                {
                    DieId = state.Dice.First(d => d.Owner == state.Pending!.Owner && d.Location == DieLocation.Available).Id
                });
            states.Add("military-" + key, state);
        }
    }

    internal static GameCommand Command(MatchState state, CommandKind kind, CommandPayload? payload = null) => new()
    {
        MatchId = state.Id,
        CommandId = "corpus-command-" + state.Revision,
        ExpectedRevision = state.Revision,
        DecisionId = state.Pending?.Id ?? "",
        Kind = kind,
        Payload = payload ?? new()
    };

    private static MatchState PaidArmyStep()
    {
        var state = FellowshipScenario.Actions(new ZeroRandom());
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        die.Face = DieFace.Army;
        return Apply(state, CommandKind.MoveArmies, new()
        {
            Stepwise = true,
            DieId = die.Id,
            Moves = [new()
            {
                From = "minas-tirith", To = "osgiliath",
                UnitIds = state.Units.Where(u => u.Location == "minas-tirith" && !u.Garrison && BoardRules.Owner(u) == Side.FreePeoples).Select(u => u.Id).ToList()
            }]
        });
    }

    internal static MatchState Apply(MatchState state, CommandKind kind, CommandPayload? payload = null)
    {
        var result = Engine.Execute(state, state.Pending!.Owner, Command(state, kind, payload), new ZeroRandom());
        if (!result.Receipt.Accepted)
            throw new InvalidOperationException($"Corpus {state.Pending.Kind}/{kind}: {result.Receipt.Error}");
        return result.State;
    }

    internal sealed class ZeroRandom : IRandomSource
    {
        public int Next(int upperBound) => 0;
    }
}
