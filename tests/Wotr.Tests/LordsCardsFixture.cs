using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class LordsCardsFixture
{
    public static MatchState State()
    {
        var state = LordsFixture.Setup();
        SmeagolTiles.Initialize(state);
        state.Phase = GamePhase.ActionResolution;
        state.Turn = 2;
        state.RandomSamples.Clear();
        state.Fellowship.Guide = CharacterRules.Strider;
        foreach (var die in state.Dice.Where(d => d.Source == "standard"))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Event;
        }
        foreach (var unit in state.Units)
            BoardRules.ReturnToReserve(unit);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }

    public static CommandPayload Cost(MatchState state, string card, DieFace face = DieFace.Event)
    {
        if (!state.Player(Side.FreePeoples).Hand.Contains(card))
            EventTests.GiveCard(state, Side.FreePeoples, card);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
        die.Face = face;
        return new() { CardId = card, CardCost = face, DieId = die.Id };
    }

    public static MatchState Play(MatchState state, string card, params int[] samples) =>
        LordsFixture.Do(state, CommandKind.PlayEvent, Cost(state, card), new SequenceRandom(samples));

    public static MatchState Choose(MatchState state, params string[] ids) =>
        LordsFixture.Do(state, CommandKind.ChooseEffect, new() { Ids = [.. ids] });

    public static MatchState Battle(string card, bool fpAttacks = true, string companion = CharacterRules.Aragorn)
    {
        var state = CardPackBattleFixture.BeforeCards(card, companion, fpAttacks);
        state.Configuration.Lords = true;
        state.Lords = new();
        return state;
    }

    public static string Json(MatchState state, Side side) =>
        System.Text.Json.JsonSerializer.Serialize(LordsFixture.Views().For(state, side), BoardCatalog.Json);
}
