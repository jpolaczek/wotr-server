using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// TEST-14: plays a Fate scenario the way a client does: the option comes from the server's projection for the side
// holding the decision, and every command is validated and executed by the server's engine.
internal static class FateScenarioDriver
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    public static MatchState Create(string id)
    {
        var scenario = TestScenarioCatalog.Find(id);
        Assert.True(scenario is not null, $"{id} is not in the catalog.");
        var state = scenario!.Create("test14-" + id);
        Assert.True(state.Configuration.Fate, id);
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        return state;
    }

    public static PlayerView View(MatchState state) => Views.For(state, state.Pending!.Owner);

    public static ActionOption Option(MatchState state, CommandKind kind)
    {
        var option = View(state).Options.FirstOrDefault(o => o.Kind == kind && o.Enabled);
        Assert.True(option is not null, $"{kind} is not offered at {state.Pending?.Kind} of {state.Pending?.Owner}.");
        return option!;
    }

    public static MatchState Do(MatchState state, CommandKind kind, CommandPayload? payload = null, IRandomSource? random = null)
    {
        var side = state.Pending!.Owner;
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = "test14-" + kind + "-" + state.Revision,
            ExpectedRevision = state.Revision,
            DecisionId = View(state).Decision!.Id,
            Kind = kind,
            Payload = payload ?? new()
        };
        var error = Engine.Validate(state, side, command);
        Assert.True(error is null, $"{kind}: {error}");
        var result = Engine.Execute(state, side, command, random ?? new SeededRandom(7));
        Assert.True(result.Receipt.Accepted, $"{kind}: {result.Receipt.Error}");
        return result.State;
    }

    // The owner plays the card from the hand with the first die the projection offers for its printed cost.
    public static MatchState Play(MatchState state, string cardId)
    {
        var play = Option(state, CommandKind.PlayEvent).Events.Single(e => e.CardId == cardId);
        var cost = play.Costs.First(c => c.AbilityId.Length == 0);
        return Do(state, CommandKind.PlayEvent, new() { CardId = cardId, DieId = cost.DieId, CardCost = cost.Face });
    }

    // A recruitment plan: the first reserve figures of each named kind go to the named region.
    public static CommandPayload Plan(ActionOption option, params (string Nation, UnitKind Kind, string Region, int Count)[] allocations)
    {
        var reserve = option.Recruitment.ToDictionary(g => (g.Nation, g.Kind), g => new Queue<string>(g.ReserveIds));
        return new()
        {
            Placements = allocations.SelectMany(a => Enumerable.Range(0, a.Count).Select(_ => new UnitPlacementChoice
            {
                UnitId = reserve[(a.Nation, a.Kind)].Dequeue(), RegionId = a.Region
            })).ToList()
        };
    }

    // The whole army of a region moves or attacks with an Army die, as the projection lists it.
    public static MatchState March(MatchState state, CommandKind kind, string from, string to)
    {
        var option = Option(state, kind);
        var army = option.Armies.Single(a => a.RegionId == from);
        Assert.Contains(to, army.Targets.Select(t => t.RegionId));
        var die = state.Dice.First(d => d.Owner == state.Pending!.Owner && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        return Do(state, kind, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = from, To = to, UnitIds = [.. army.UnitIds], CharacterIds = [.. army.CharacterIds] }]
        });
    }

    // Leaders' re-rolls are declined until the battle moves on.
    public static MatchState NoRerolls(MatchState state)
    {
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = Do(state, CommandKind.RerollBattle);
        return state;
    }

    public static NationState Nation(MatchState state, string id) => state.Nations.Single(n => n.Id == id);

    // Every die rolls a 1: no hits, so a battle moves on without casualties.
    public sealed class LowestFace : IRandomSource
    {
        public int Next(int exclusiveMaximum) => 0;
    }
}
