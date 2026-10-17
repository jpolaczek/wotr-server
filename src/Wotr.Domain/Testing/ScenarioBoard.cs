using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-02: a situation built from the standard setup of turn 1 with the action dice already rolled,
// so only what a scenario tests differs from a normal game. Every change uses real pieces and cards.
public sealed class ScenarioBoard
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);

    private ScenarioBoard(MatchState state) => State = state;

    public MatchState State { get; }

    public static ScenarioBoard Setup(string matchId, IRandomSource random)
    {
        var state = MatchFactory.Create(matchId, random);
        TurnFlow.Start(state);
        return new(state);
    }

    // The side's standard dice show these results in order; dice beyond them stay in the pool unrolled.
    public ScenarioBoard Dice(Side side, params DieFace[] faces)
    {
        var dice = StandardDice(side).ToArray();
        if (faces.Length > dice.Length)
            throw new ArgumentException($"{side} has only {dice.Length} action dice.", nameof(faces));
        for (var index = 0; index < faces.Length; index++)
        {
            dice[index].Location = DieLocation.Available;
            dice[index].Face = faces[index];
        }
        return this;
    }

    // Dice already used earlier in this turn.
    public ScenarioBoard Spent(Side side, params DieFace[] faces)
    {
        var dice = StandardDice(side).Where(d => d.Location == DieLocation.Unrolled).ToArray();
        if (faces.Length > dice.Length)
            throw new ArgumentException($"{side} has only {dice.Length} unrolled dice.", nameof(faces));
        for (var index = 0; index < faces.Length; index++)
        {
            dice[index].Location = DieLocation.Spent;
            dice[index].Face = faces[index];
        }
        return this;
    }

    // Shadow dice in the Hunt Box: allocated before the roll, and Eye results added by the roll.
    public ScenarioBoard Hunt(int allocated, int eyes)
    {
        var dice = StandardDice(Side.Shadow).Where(d => d.Location == DieLocation.Unrolled).ToArray();
        if (allocated + eyes > dice.Length)
            throw new ArgumentException("Not enough unrolled Shadow dice for the Hunt Box.");
        for (var index = 0; index < allocated + eyes; index++)
        {
            dice[index].Location = DieLocation.Hunt;
            dice[index].Face = DieFace.Eye;
            dice[index].HuntOrigin = index < allocated ? "allocated" : "rolled-eye";
        }
        return this;
    }

    public ScenarioBoard AtWar(params string[] nations)
    {
        foreach (var id in nations)
            Nation(id, 0, true);
        return this;
    }

    public ScenarioBoard Nation(string id, int stepsToWar, bool active)
    {
        var nation = State.Nations.Single(n => n.Id == id);
        nation.StepsToWar = stepsToWar;
        nation.Active = active;
        return this;
    }

    // Moves named cards from the side's decks (or discard pile) into its hand.
    public ScenarioBoard Hand(Side side, params string[] cards)
    {
        var player = State.Players.Single(p => p.Side == side);
        foreach (var card in cards)
        {
            if (player.Hand.Contains(card))
                continue;
            var source = player.Decks.Values.FirstOrDefault(deck => deck.Contains(card)) ?? player.Discard;
            if (!source.Remove(card))
                throw new ArgumentException($"{card} is not available to {side}.", nameof(cards));
            player.Hand.Add(card);
        }
        player.HandVersion++;
        return this;
    }

    public ScenarioBoard Place(string nation, UnitKind kind, string region, int count = 1)
    {
        var pieces = State.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Take(count).ToArray();
        if (pieces.Length != count)
            throw new ArgumentException($"Only {pieces.Length} {nation} {kind} pieces are in the reserve.");
        foreach (var piece in pieces)
            piece.Location = region;
        return this;
    }

    // Separates a companion from the Fellowship into a region, as if it had left earlier in the game.
    public ScenarioBoard Companion(string definitionId, string region)
    {
        var companion = CharacterCatalog.Companion(State, definitionId);
        State.Fellowship.Companions.Remove(companion.Id);
        companion.Location = region;
        if (!State.Characters.Contains(companion))
            State.Characters.Add(companion);
        return this;
    }

    // Returns every piece of the side in the region to its reserve.
    public ScenarioBoard Clear(string region)
    {
        foreach (var unit in State.Units.Where(u => u.Location == region))
            BoardRules.ReturnToReserve(unit);
        return this;
    }

    // Brings a character into play exactly as the entry action would, e.g. a Minion for Gandalf the White.
    public ScenarioBoard Enter(string definitionId, string region)
    {
        CharacterEntry.Apply(State, new CharacterEntryPlan(definitionId, definitionId, region, false, CharacterEntry.RequiredCost(definitionId)));
        return this;
    }

    // TEST-06: the Fellowship enters the Mordor Track through the real command of the Fellowship phase,
    // so the engine returns prepared tiles and discarded Eyes to the Hunt Pool (BASE p. 43).
    public ScenarioBoard EnterMordor(IRandomSource random)
    {
        var decision = State.Pending ?? throw new InvalidOperationException("No Fellowship phase to enter Mordor from.");
        var result = Engine.Execute(State, decision.Owner, new GameCommand
        {
            MatchId = State.Id,
            CommandId = "scenario-enter-mordor",
            ExpectedRevision = State.Revision,
            DecisionId = decision.Id,
            Kind = CommandKind.EnterMordor
        }, random);
        if (!result.Receipt.Accepted)
            throw new InvalidOperationException("Scenario setup refused EnterMordor: " + result.Receipt.Error);
        return new(result.State);
    }

    // TEST-07: only the Hunt tiles that keep a promised ending certain stay in the pool; the rest wait in the
    // discard pile, from which the pool refills as usual once it is empty.
    public ScenarioBoard KeepHuntTiles(Func<HuntTileDefinition, bool> keep)
    {
        foreach (var tile in State.HuntTiles.Where(t => t.Location == "pool" && !keep(HuntPool.Definitions[t.Id])))
            tile.Location = "discard";
        if (!State.HuntTiles.Any(t => t.Location == "pool"))
            throw new ArgumentException("The Hunt pool would be empty.", nameof(keep));
        return this;
    }

    // TEST-07: the side holds the named settlements of the other side with one Regular of its nation in each,
    // as after their capture; the defenders went back to their reserve.
    public ScenarioBoard Conquer(Side side, string nation, params string[] regions)
    {
        foreach (var region in regions)
        {
            Clear(region);
            Place(nation, UnitKind.Regular, region);
            State.Control[region] = side;
        }
        return this;
    }

    public ScenarioBoard Change(Action<MatchState> change)
    {
        change(State);
        return this;
    }

    // Action resolution of turn 1 with the side to act holding the decision.
    public MatchState Act(Side side)
    {
        State.Phase = GamePhase.ActionResolution;
        TurnFlow.SelectNext(State, side);
        return State;
    }

    private IEnumerable<DieState> StandardDice(Side side) =>
        State.Dice.Where(d => d.Owner == side && d.Source == "standard" && d.Location != DieLocation.Removed);
}
