using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

// Only persisted test scenarios use fixed rolls. The server still uses its production RNG.
internal static class BattleScenario
{
    private static readonly GameEngine Engine = new(new TurnCommands(), new MusterCommands(), new BattleCommands(), new StrongholdCommands());

    public static Dictionary<string, MatchState> Create(IRandomSource random) => new()
    {
        ["field"] = Field(random),
        ["retreat"] = Retreat(random),
        ["shelter-stay"] = Stronghold(random),
        ["shelter-advance"] = Stronghold(random),
        ["siege"] = Siege(random),
        ["sortie"] = Sortie(random),
        ["relief"] = Relief(random),
        ["no-regular"] = Losses(random, true),
        ["two-hits"] = Losses(random, false),
        ["policy-fp"] = Policy(random, Side.FreePeoples),
        ["policy-sh"] = Policy(random, Side.Shadow)
    };

    private static MatchState Empty(IRandomSource random, Side actor = Side.FreePeoples)
    {
        var state = FellowshipScenario.Actions(random);
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        foreach (var die in state.Dice)
            die.Face = DieFace.Army;
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    private static List<string> Put(MatchState state, string nation, UnitKind kind, string region, int count, bool garrison = false)
    {
        var units = state.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Take(count).ToList();
        if (units.Count != count)
            throw new InvalidOperationException("Not enough scenario figures.");
        foreach (var unit in units)
        {
            unit.Location = region;
            unit.Garrison = garrison;
        }
        return units.Select(u => u.Id).ToList();
    }

    internal static MatchState Field(IRandomSource random)
    {
        var state = Empty(random);
        Put(state, "gondor", UnitKind.Regular, "minas-tirith", 2);
        Put(state, "gondor", UnitKind.Elite, "minas-tirith", 1);
        Put(state, "gondor", UnitKind.Leader, "minas-tirith", 1);
        Put(state, "sauron", UnitKind.Regular, "lossarnach", 3);
        state.Control["pelargir"] = Side.Shadow;
        return state;
    }

    private static MatchState Policy(IRandomSource random, Side attacker)
    {
        var state = Empty(random, attacker);
        Put(state, attacker == Side.FreePeoples ? "gondor" : "sauron", UnitKind.Regular, "minas-tirith", 3);
        Put(state, attacker == Side.FreePeoples ? "sauron" : "gondor", UnitKind.Regular, "lossarnach", 3);
        return Begin(state, "minas-tirith", "lossarnach");
    }

    internal static MatchState Retreat(IRandomSource random)
    {
        var state = Field(random);
        state = Begin(state, "minas-tirith", "lossarnach");
        state = NoCards(state);
        return Execute(state, CommandKind.RerollBattle);
    }

    internal static MatchState Stronghold(IRandomSource random)
    {
        var state = Empty(random, Side.Shadow);
        Put(state, "gondor", UnitKind.Regular, "minas-tirith", 6);
        Put(state, "sauron", UnitKind.Elite, "osgiliath", 4);
        return state;
    }

    private static MatchState Siege(IRandomSource random)
    {
        var state = Empty(random, Side.Shadow);
        Put(state, "gondor", UnitKind.Regular, "minas-tirith", 5, true);
        Put(state, "sauron", UnitKind.Elite, "minas-tirith", 2);
        state = Begin(state, "minas-tirith", "minas-tirith");
        return NoCards(state);
    }

    private static MatchState Sortie(IRandomSource random)
    {
        var state = Empty(random);
        Put(state, "gondor", UnitKind.Regular, "minas-tirith", 5, true);
        Put(state, "sauron", UnitKind.Regular, "minas-tirith", 5);
        state = Begin(state, "minas-tirith", "minas-tirith");
        return NoCards(state);
    }

    private static MatchState Relief(IRandomSource random)
    {
        var state = Empty(random);
        Put(state, "gondor", UnitKind.Regular, "minas-tirith", 5, true);
        Put(state, "rohan", UnitKind.Regular, "lossarnach", 6);
        var defender = Put(state, "sauron", UnitKind.Regular, "minas-tirith", 1).Single();
        state = Begin(state, "lossarnach", "minas-tirith");
        state = NoCards(state, 5, 5, 5, 5, 5, 0);
        return Execute(state, CommandKind.TakeBattleLosses, new() { Losses = [new() { UnitId = defender }] });
    }

    private static MatchState Losses(IRandomSource random, bool noRegular)
    {
        var state = Empty(random);
        var elite = Put(state, "gondor", UnitKind.Elite, "minas-tirith", 1).Single();
        if (noRegular)
        {
            foreach (var regular in state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular))
                regular.Location = "pelargir";
            var gimli = CharacterCatalog.Companion(state, "base.character.gimli");
            state.Fellowship.Companions.Remove(gimli.Id);
            gimli.Location = "minas-tirith";
            state.Characters.Add(gimli);
        }
        else
        {
            Put(state, "rohan", UnitKind.Regular, "minas-tirith", 1);
            Put(state, "north", UnitKind.Regular, "minas-tirith", 1);
        }
        Put(state, "sauron", UnitKind.Regular, "lossarnach", 2);
        state = Begin(state, "minas-tirith", "lossarnach");
        // Gimli contributes a second Combat die through Captain of the West.
        state = NoCards(state, noRegular ? [5, 5, 5, 0] : [0, 0, 0, 5, 5]);
        if (state.Pending?.Kind != DecisionKind.Losses || state.Pending.Owner != Side.FreePeoples)
            throw new InvalidOperationException("Expected a Free Peoples loss decision.");
        return state;
    }

    private static MatchState Begin(MatchState state, string from, string to)
    {
        var actor = state.Pending!.Owner;
        return Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = state.Dice.First(d => d.Owner == actor && d.Location == DieLocation.Available).Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = from, To = to,
                UnitIds = state.Units.Where(u => u.Location == from && BoardRules.Owner(u) == actor).Select(u => u.Id).ToList(),
                CharacterIds = state.Characters.Where(c => c.Location == from && c.Side == actor).Select(c => c.Id).ToList() }]
        });
    }

    private static MatchState NoCards(MatchState state, params int[] rolls)
    {
        state = Execute(state, CommandKind.CommitCombatCard);
        return Execute(state, CommandKind.CommitCombatCard, random: new FixedRoll(rolls));
    }

    private static MatchState Execute(MatchState state, CommandKind kind, CommandPayload? payload = null, IRandomSource? random = null)
    {
        var result = Engine.Execute(state, state.Pending!.Owner, new()
        {
            MatchId = state.Id,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending.Id,
            Kind = kind,
            Payload = payload ?? new()
        }, random ?? new FixedRoll([]));
        if (!result.Receipt.Accepted)
            throw new InvalidOperationException(kind + ": " + result.Receipt.Error);
        return result.State;
    }

    private sealed class FixedRoll(IEnumerable<int> rolls) : IRandomSource
    {
        private readonly Queue<int> values = new(rolls);
        public int Next(int maximum) => values.Count > 0 ? values.Dequeue() : 0;
    }
}
