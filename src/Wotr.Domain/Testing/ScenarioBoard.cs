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
        if (State.Fellowship.Guide == companion.Id)
            State.Fellowship.Guide = CompanionRules.Guides(State)[0];
        return this;
    }

    // TEST-09: the Fellowship stands in a region after its last move there; Progress counts its hidden steps since.
    public ScenarioBoard FellowshipAt(string region, int progress = 0)
    {
        State.Fellowship.RegionId = region;
        State.Fellowship.Progress = progress;
        return this;
    }

    // TEST-09: the Fellowship has been revealed.
    public ScenarioBoard Reveal()
    {
        State.Fellowship.Hidden = false;
        return this;
    }

    // UI-300: Corruption the Fellowship carries already, so a card that heals it has something to heal.
    public ScenarioBoard Corruption(int value)
    {
        State.Fellowship.Corruption = value;
        return this;
    }

    // UI-300: a Companion of the Fellowship's highest Level guides it.
    public ScenarioBoard Guide(string definitionId)
    {
        var guide = CharacterCatalog.Companion(State, definitionId).Id;
        if (!CompanionRules.Guides(State).Contains(guide))
            throw new ArgumentException($"{definitionId} cannot guide the Fellowship now.", nameof(definitionId));
        State.Fellowship.Guide = guide;
        return this;
    }

    // UI-300: every Companion has left the Fellowship into a region, so Gollum guides it.
    public ScenarioBoard GollumGuides(string region)
    {
        foreach (var companion in State.Fellowship.Companions.ToList())
            Companion(companion, region);
        return this;
    }

    // UI-300: the Fellowship's next move this turn meets an almost certain Hunt that deals damage: five Shadow dice in the
    // Hunt Box, two Free Peoples dice that already moved the Fellowship this turn (+2, so the Hunt hits on 4 or more) and
    // a pool of the chosen tiles (damage tiles by default). The card's owner keeps its event die and an Event die; the
    // Free Peoples keep a Character die for the move, and a Shadow without a card to play has no die left, so the Free
    // Peoples act again at once.
    public ScenarioBoard HuntAhead(Side owner, DieFace eventType, Func<HuntTileDefinition, bool>? tiles = null)
    {
        var shadow = StandardDice(Side.Shadow).ToArray();
        DieFace[] kept = owner == Side.Shadow ? [eventType, DieFace.Event] : [];
        for (var index = 0; index < shadow.Length; index++)
        {
            var die = shadow[index];
            die.HuntOrigin = "";
            if (index < kept.Length)
                (die.Location, die.Face) = (DieLocation.Available, kept[index]);
            else if (index < kept.Length + 5)
                (die.Location, die.Face, die.HuntOrigin) = (DieLocation.Hunt, DieFace.Eye, index == kept.Length ? "allocated" : "rolled-eye");
            else
                (die.Location, die.Face) = (DieLocation.Spent, DieFace.Army);
        }
        var free = StandardDice(Side.FreePeoples).ToArray();
        DieFace[] left = owner == Side.FreePeoples ? [DieFace.Character, DieFace.Event] : [DieFace.Character, DieFace.Muster];
        for (var index = 0; index < free.Length; index++)
        {
            var die = free[index];
            die.HuntOrigin = index < 2 ? DiceRules.FellowshipMove : "";
            (die.Location, die.Face) = index < 2 ? (DieLocation.Hunt, DieFace.Character)
                : index - 2 < left.Length ? (DieLocation.Available, left[index - 2]) : (DieLocation.Spent, DieFace.Army);
        }
        State.Fellowship.Progress += 2;
        return KeepHuntTiles(tiles ?? (tile => tile.Value is "1" or "2" or "3"));
    }

    // UI-300: the Fellowship stands on step 0 of the Mordor Track (MordorRules.Enter), where a special tile played now
    // joins the Hunt Pool at once; the pool keeps one other tile, so each special tile is drawn within two steps.
    public ScenarioBoard OnMordorTrack()
    {
        State.Fellowship.RegionId = MordorRules.Track;
        State.Fellowship.MordorStep = 0;
        State.Fellowship.Progress = 0;
        var kept = false;
        return KeepHuntTiles(tile => tile.Value != "eye" && !kept && (kept = true));
    }

    // UI-300: the side has spent every die it rolled, so the other side takes the remaining actions in a row.
    public ScenarioBoard NoActions(Side side)
    {
        foreach (var die in StandardDice(side).Where(d => d.Location == DieLocation.Available))
            die.Location = DieLocation.Spent;
        return this;
    }

    // UI-300: a card already kept on the side's table.
    public ScenarioBoard Table(Side side, string card)
    {
        var player = State.Players.Single(p => p.Side == side);
        var source = player.Decks.Values.FirstOrDefault(deck => deck.Contains(card)) ?? player.Discard;
        if (!source.Remove(card))
            throw new ArgumentException($"{card} is not available to {side}.", nameof(card));
        player.Table.Add(card);
        return this;
    }

    // TEST-09: a nation turned active without changing its distance from war.
    public ScenarioBoard Activate(string nation)
    {
        State.Nations.Single(n => n.Id == nation).Active = true;
        return this;
    }

    // TEST-09: a character brought into play by its own entry rule, so Gandalf the White takes Gandalf the Grey's figure
    // and Aragorn takes Strider's where the rule allows it.
    public ScenarioBoard Replace(string definitionId)
    {
        var plan = CharacterEntry.Candidates(State, Side.FreePeoples).FirstOrDefault(p => p.DefinitionId == definitionId)
            ?? throw new InvalidOperationException($"{definitionId} cannot enter play here.");
        CharacterEntry.Apply(State, plan);
        return this;
    }

    // TEST-09: a character standing in a region after moving there.
    public ScenarioBoard Relocate(string definitionId, string region)
    {
        State.Characters.Single(c => c.DefinitionId == definitionId).Location = region;
        return this;
    }

    // TEST-09: the Free Peoples army of a stronghold has withdrawn into it and a Shadow army besieges it.
    public ScenarioBoard Besiege(string region, string nation, int regulars)
    {
        foreach (var unit in State.Units.Where(u => u.Location == region && BoardRules.Owner(u) == Side.FreePeoples))
            unit.Garrison = true;
        foreach (var character in State.Characters.Where(c => c.Location == region && c.Side == Side.FreePeoples))
            character.Garrison = true;
        return Place(nation, UnitKind.Regular, region, regulars);
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
    public ScenarioBoard EnterMordor(IRandomSource random) => Command(CommandKind.EnterMordor, new(), random, "scenario-enter-mordor");

    // TEST-09: the owner of the pending decision answers it through the engine, as in a played game.
    public ScenarioBoard Command(CommandKind kind, CommandPayload payload, IRandomSource random, string? commandId = null)
    {
        var decision = State.Pending ?? throw new InvalidOperationException($"No decision to answer with {kind}.");
        var result = Engine.Execute(State, decision.Owner, new GameCommand
        {
            MatchId = State.Id,
            CommandId = commandId ?? "scenario-" + kind + "-" + State.Revision,
            ExpectedRevision = State.Revision,
            DecisionId = decision.Id,
            Kind = kind,
            Payload = payload
        }, random);
        if (!result.Receipt.Accepted)
            throw new InvalidOperationException($"Scenario setup refused {kind}: {result.Receipt.Error}");
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
