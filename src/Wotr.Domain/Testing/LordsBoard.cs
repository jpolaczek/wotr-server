using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-12: the private choices of a Lords setup — the Companion versions with the Guide, the Council's homelands and
// the Shadow's token when one Companion starts outside the Fellowship.
internal sealed record LordsSetupChoices(string Guide, IReadOnlyList<string> Versions, IReadOnlyList<UnitPlacementChoice> Homelands,
    string Token = CouncilTokens.Politics)
{
    public static LordsSetupChoices Standard { get; } = new(CharacterRules.Strider, [CharacterRules.Grey], []);

    public static LordsSetupChoices Keeper { get; } = new(CharacterRules.Grey, [LordsCharacters.Gandalf], []);
}

// TEST-12: the Lords scenarios start after the real setup of a Lords game — the private Companion versions, the
// Council's homelands and token, the Fellowship phase and the roll of turn 1 — at its first action. The standard dice go
// back to the pool, so ScenarioBoard.Dice and the other changes act as on the base setup; earlier plays of the imagined
// turn run through the engine (Played, Attack).
internal static class LordsBoard
{
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    public static MatchConfiguration Lords => new() { Lords = true };

    public static MatchConfiguration Council => new() { Lords = true, Council = true };

    public static MatchConfiguration WithKings => new() { Lords = true, Kings = true };

    public static ScenarioBoard Setup(string id, IRandomSource random) => Setup(id, random, Lords, LordsSetupChoices.Standard);

    public static ScenarioBoard Setup(string id, IRandomSource random, MatchConfiguration configuration) =>
        Setup(id, random, configuration, LordsSetupChoices.Standard);

    public static ScenarioBoard Setup(string id, IRandomSource random, MatchConfiguration configuration, LordsSetupChoices choices)
    {
        var state = FirstAction(Start(id, random, configuration), choices);
        Unroll(state);
        return ScenarioBoard.Of(state);
    }

    // A new game before its first private decision (the Companion versions).
    public static MatchState Start(string id, IRandomSource random, MatchConfiguration configuration)
    {
        var state = MatchFactory.Create(id, random, configuration);
        TurnFlow.Start(state);
        return state;
    }

    // Answers the setup decisions with the given choices until the first action of turn 1, as ExpansionBoard.Actions.
    public static MatchState FirstAction(MatchState state, LordsSetupChoices choices)
    {
        for (var step = 0; state.Pending?.Kind != DecisionKind.Action; step++)
        {
            if (step > 8)
                throw new InvalidOperationException("The Lords setup did not reach the first action.");
            state = Answer(state, choices);
        }
        return state;
    }

    private static MatchState Answer(MatchState state, LordsSetupChoices choices) => state.Pending!.Kind switch
    {
        DecisionKind.CouncilVersions => ExpansionBoard.Execute(state, CommandKind.ChooseCompanionVersions,
            new() { GuideId = choices.Guide, Ids = [.. choices.Versions] }),
        DecisionKind.CouncilHomelands => ExpansionBoard.Execute(state, CommandKind.ChooseCouncilHomelands,
            new() { Placements = [.. choices.Homelands] }),
        DecisionKind.CouncilToken => ExpansionBoard.Execute(state, CommandKind.ChooseCouncilToken, new() { Ids = [choices.Token] }),
        DecisionKind.FellowshipPhase => ExpansionBoard.Execute(state, CommandKind.ContinueFellowship),
        DecisionKind.AllocateHunt => ExpansionBoard.Execute(state, CommandKind.AllocateHunt),
        DecisionKind.SpecialDieChoice => ExpansionBoard.Execute(state, CommandKind.ChooseSpecialDie,
            new() { DieId = LordsDiceRoll.LegalChoices(state, state.Pending.Owner)[0] }),
        DecisionKind.RulerDiscard => ExpansionBoard.Execute(state, CommandKind.DiscardRulerDie,
            new() { DieId = KingsDice.DiscardCandidates(state, state.Pending.Owner)[0] }),
        _ => throw new InvalidOperationException($"Unexpected Lords setup decision {state.Pending.Kind}.")
    };

    private static void Unroll(MatchState state)
    {
        foreach (var die in state.Dice.Where(d => d.Source == "standard" && d.Location != DieLocation.Removed))
        {
            die.Location = DieLocation.Unrolled;
            die.HuntOrigin = "";
        }
    }

    // The side played the card earlier in this turn with an available die showing its event type, through the engine,
    // so a card kept on the table carries its lasting effect; the engine then gives the other side the action.
    public static ScenarioBoard Played(ScenarioBoard board, Side side, string card)
    {
        var state = board.Act(side);
        var face = CardCatalog.All[card].EventType;
        var die = state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available && d.Source == "standard");
        die.Face = face;
        foreach (var deck in state.Player(side).Decks.Values)
            deck.Remove(card);
        HandRules.Add(state.Player(side), card);
        return ScenarioBoard.Of(ExpansionBoard.Execute(state, CommandKind.PlayEvent, new() { CardId = card, DieId = die.Id, CardCost = face }));
    }

    // The side attacks from a region with its whole army there, characters included, as the legal option lists it,
    // paid with an Army die.
    public static MatchState Attack(MatchState state, Side side, string from, string to)
    {
        var army = Views.For(state, side).Options.First(o => o.Kind == CommandKind.AttackArmy && o.Enabled).Armies
            .Single(a => a.RegionId == from);
        var die = state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        return ExpansionBoard.Execute(state, CommandKind.AttackArmy, new()
        {
            DieId = die.Id,
            MovementCost = DieFace.Army,
            Moves = [new() { From = from, To = to, UnitIds = [.. army.UnitIds], CharacterIds = [.. army.CharacterIds] }]
        });
    }

    // The army attacked in its Stronghold stays out to fight a field battle.
    public static MatchState Field(MatchState state)
    {
        if (state.Pending?.Kind != DecisionKind.StrongholdDefense)
            throw new InvalidOperationException($"The defender must choose how to defend its Stronghold, not {state.Pending?.Kind}.");
        return ExpansionBoard.Execute(state, CommandKind.FightFieldBattle);
    }

    // The side's opponent commits no Combat card, so the battle waits on the side's own choice.
    public static MatchState CombatChoice(MatchState state, Side side)
    {
        while (state.Pending is { Kind: DecisionKind.CombatCard } pending && pending.Owner != side)
            state = ExpansionBoard.Execute(state, CommandKind.CommitCombatCard, new() { CardId = "" });
        if (state.Pending is not { Kind: DecisionKind.CombatCard } choice || choice.Owner != side)
            throw new InvalidOperationException($"The battle must wait on the {side} Combat card choice.");
        return state;
    }

    // Both sides have used every die of this turn; the engine's turn flow recovers them and draws the cards of the next
    // turn and opens its Fellowship phase.
    public static MatchState NextTurn(MatchState state)
    {
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Available))
            die.Location = DieLocation.Spent;
        state.Phase = GamePhase.ActionResolution;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        if (state.Pending?.Kind != DecisionKind.FellowshipPhase)
            throw new InvalidOperationException($"The next turn must open at its Fellowship phase, not {state.Pending?.Kind}.");
        return state;
    }

    // Moves the definition's figure into the region as a character that entered earlier in the imagined game.
    public static CharacterState Put(MatchState state, string definition, string region) => ExpansionBoard.Put(state, definition, region);

    // The roll of the imagined turn: six Shadow results with one die in the Hunt Box and four Free Peoples results.
    public static ScenarioBoard Rolled(ScenarioBoard board) => board
        .Dice(Side.Shadow, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.Army, DieFace.Muster)
        .Hunt(1, 0)
        .Dice(Side.FreePeoples, DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event);

    // Every die of a builder's earlier step rolls a 6 and every draw takes the pool's last tile.
    public static IRandomSource Highest { get; } = new HighestFace();

    // The dice of a builder's earlier step show the given faces in order (5 rolls a 6); later rolls show a 1.
    public static IRandomSource Rolls(params int[] faces) => new FixedRolls(faces);

    private sealed class HighestFace : IRandomSource
    {
        public int Next(int exclusiveMaximum) => exclusiveMaximum - 1;
    }

    private sealed class FixedRolls(int[] faces) : IRandomSource
    {
        private int next;

        public int Next(int exclusiveMaximum) => next < faces.Length ? Math.Min(faces[next++], exclusiveMaximum - 1) : 0;
    }
}
