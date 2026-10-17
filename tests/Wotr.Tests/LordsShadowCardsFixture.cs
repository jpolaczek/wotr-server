using Wotr.Contracts;
using Wotr.Domain;

namespace Wotr.Tests;

internal static class LordsShadowCardsFixture
{
    public static MatchState State()
    {
        var state = LordsCardsFixture.State();
        LordsCharacterFixture.War(state, "sauron", "isengard", "southrons");
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    public static CommandPayload Cost(MatchState state, string card, DieFace face = DieFace.Event)
    {
        EventTests.GiveCard(state, Side.Shadow, card);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        die.Face = face;
        return new() { CardId = card, CardCost = face, DieId = die.Id };
    }

    public static MatchState Play(MatchState state, string card) =>
        LordsFixture.Do(state, CommandKind.PlayEvent, Cost(state, card));

    public static MatchState Battle(string card, bool fpAttacks = true, string companion = CharacterRules.Aragorn)
    {
        var state = LordsCardsFixture.Battle("base.fp.character.01", fpAttacks, companion);
        state.Player(Side.FreePeoples).Hand.Clear();
        EventTests.GiveCard(state, Side.Shadow, card);
        return state;
    }

    public static MatchState Reveal(MatchState state, string card, params int[] samples)
    {
        var first = state.Pending!.Owner;
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = first == Side.Shadow ? card : "" });
        return LordsFixture.Do(state, CommandKind.CommitCombatCard, new() { CardId = first == Side.Shadow ? "" : card }, new SequenceRandom(samples));
    }
}
