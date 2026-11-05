using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

// TEST-04: expansion situations reach the first action of turn 1 through the real setup and Fellowship
// phase; dice rolled by a fixed source. Characters placed here entered earlier in the imagined game.
internal static class ExpansionBoard
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);

    public static MatchState Actions(string matchId, IRandomSource random, MatchConfiguration configuration, string gandalf = CharacterRules.Grey)
    {
        var state = MatchFactory.Create(matchId, random, configuration);
        TurnFlow.Start(state);
        for (var step = 0; state.Pending?.Kind != DecisionKind.Action; step++)
        {
            if (step > 6)
                throw new InvalidOperationException("Expansion setup did not reach the first action.");
            state = state.Pending!.Kind switch
            {
                DecisionKind.CouncilVersions => Execute(state, CommandKind.ChooseCompanionVersions,
                    new() { GuideId = gandalf == CharacterRules.Grey ? CharacterRules.Strider : CharacterRules.Grey, Ids = [gandalf] }),
                DecisionKind.CouncilHomelands => Execute(state, CommandKind.ChooseCouncilHomelands),
                DecisionKind.CouncilToken => Execute(state, CommandKind.ChooseCouncilToken, new() { Ids = [CouncilTokens.Politics] }),
                DecisionKind.FellowshipPhase => Execute(state, CommandKind.ContinueFellowship),
                DecisionKind.AllocateHunt => Execute(state, CommandKind.AllocateHunt),
                DecisionKind.SpecialDieChoice => Execute(state, CommandKind.ChooseSpecialDie,
                    new() { DieId = LordsDiceRoll.LegalChoices(state, state.Pending.Owner)[0] }),
                DecisionKind.RulerDiscard => Execute(state, CommandKind.DiscardRulerDie,
                    new() { DieId = KingsDice.DiscardCandidates(state, state.Pending.Owner)[0] }),
                _ => throw new InvalidOperationException($"Unexpected expansion setup decision {state.Pending.Kind}.")
            };
        }
        return state;
    }

    // Commands use a source that always rolls the first face, so the situation stays the same every time.
    public static MatchState Execute(MatchState state, CommandKind kind, CommandPayload? payload = null, IRandomSource? random = null)
    {
        var result = Engine.Execute(state, state.Pending!.Owner, new()
        {
            MatchId = state.Id,
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending.Id,
            CommandId = "scenario-" + state.Revision,
            Kind = kind,
            Payload = payload ?? new()
        }, random ?? new FirstFace());
        if (!result.Receipt.Accepted)
            throw new InvalidOperationException($"Scenario command {kind} refused: {result.Receipt.Error}");
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

    public static DieState Die(MatchState state, Side side) =>
        state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available);

    // A special or Ruler die showing a chosen result, as after the roll and the special-die choice.
    public static DieState Show(MatchState state, string dieId, DieFace face)
    {
        var die = state.Dice.Single(d => d.Id == dieId);
        die.Face = face;
        die.Location = DieLocation.Available;
        if (die.Special is not null)
            die.Special.Chosen = true;
        return die;
    }

    public static void Faces(MatchState state, Side side, DieFace face)
    {
        foreach (var die in state.Dice.Where(d => d.Owner == side && d.Location == DieLocation.Available && d.Source == "standard"))
            die.Face = face;
    }

    public static void AtWar(MatchState state, params string[] nations)
    {
        foreach (var nation in state.Nations.Where(n => nations.Contains(n.Id)))
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
    }

    public static SovereignState Sovereign(MatchState state, string id) => state.Kings!.Sovereigns.Single(s => s.Id == id);

    private sealed class FirstFace : IRandomSource
    {
        public int Next(int exclusiveMaximum) => 0;
    }
}
