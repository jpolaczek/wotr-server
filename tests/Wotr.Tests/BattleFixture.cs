using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

internal static class BattleFixture
{
    public const string FpCard = "base.fp.strategy.01";
    public const string ShCard = "base.sh.strategy.01";
    public const string Origin = "minas-tirith";
    public const string Target = "lossarnach";

    public static GameEngine Engine(EffectCatalog? catalog = null) => new(new TurnCommands(), new MusterCommands(),
        new BattleCommands(catalog), new StrongholdCommands(), new EffectCommands(catalog), new DeclineReactionHandler());

    public static MatchState State(Side actor = Side.FreePeoples)
    {
        var state = TurnTests.Actions(actor: actor);
        // These scenarios count battle entropy independently of initial deck shuffles.
        state.RandomSamples.Clear();
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        foreach (var nation in state.Nations)
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
        foreach (var die in state.Dice)
            die.Face = DieFace.Army;
        return state;
    }

    public static UnitState Put(MatchState state, string nation, UnitKind kind, string region)
    {
        var unit = state.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");
        unit.Location = region;
        return unit;
    }

    public static CharacterState Character(MatchState state, string id, Side side, string region, int leadership = 1, int level = 2)
    {
        var character = new CharacterState { Id = id, DefinitionId = id, Side = side, Location = region, Leadership = leadership, Level = level };
        state.Characters.Add(character);
        return character;
    }

    public static GameCommand Command(MatchState state, CommandKind kind, CommandPayload? payload = null)
    {
        var command = KernelTests.Command(state, kind);
        command.Payload = payload ?? new();
        return command;
    }

    public static MatchState Execute(MatchState state, CommandKind kind, CommandPayload? payload = null, IRandomSource? random = null, EffectCatalog? catalog = null)
    {
        var result = Engine(catalog).Execute(state, state.Pending!.Owner, Command(state, kind, payload), random ?? new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    public static CommandPayload AttackPayload(MatchState state, IEnumerable<string> ids, string from = Origin, string to = Target, DieFace cost = DieFace.Army) => new()
    {
        DieId = state.Dice.First(d => d.Owner == state.Pending!.Owner && d.Location == DieLocation.Available).Id,
        MovementCost = cost,
        Moves = [new() { From = from, To = to, UnitIds = ids.Where(id => state.Units.Any(u => u.Id == id)).ToList(),
            CharacterIds = ids.Where(id => state.Characters.Any(c => c.Id == id)).ToList() }]
    };

    public static MatchState Begin(MatchState state, IEnumerable<string> ids, EffectCatalog? catalog = null, string from = Origin, string to = Target) =>
        Execute(state, CommandKind.AttackArmy, AttackPayload(state, ids, from, to), catalog: catalog);

    public static MatchState NoCards(MatchState state, params int[] dice)
    {
        state = Execute(state, CommandKind.CommitCombatCard);
        return Execute(state, CommandKind.CommitCombatCard, random: new SequenceRandom(dice.Select(d => d - 1).ToArray()));
    }

    public static BattleLossChoice Loss(string id, BattleLossKind kind = BattleLossKind.Remove) => new() { UnitId = id, Kind = kind };
    public static MatchState Losses(MatchState state, params BattleLossChoice[] choices) => Execute(state, CommandKind.TakeBattleLosses, new() { Losses = [.. choices] });

    public static void Rejected(MatchState state, GameCommand command, Side? actor = null, EffectCatalog? catalog = null)
    {
        var before = StateCodec.Write(state);
        var rng = new SequenceRandom();
        var result = Engine(catalog).Execute(state, actor ?? state.Pending!.Owner, command, rng);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal(before, StateCodec.Write(result.State));
        Assert.Equal(0, rng.Calls);
    }

    public static EffectCatalog Catalog(IReadOnlyList<CombatEffect> fp, IReadOnlyList<CombatEffect> sh, params EffectDefinition[] programs) =>
        new(programs, [], combats: [new(FpCard, new UnconditionalCombat(), fp), new(ShCard, new UnconditionalCombat(), sh)]);
}
