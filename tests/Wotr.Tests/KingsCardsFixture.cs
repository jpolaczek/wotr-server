using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

internal static class KingsCardsFixture
{
    public const string Boromir = "base.character.boromir";
    public const string Gimli = "base.character.gimli";
    public const string Merry = "base.character.merry";
    public const string One = KingsCorruptionTests.One;

    public static MatchState State(Side actor = Side.FreePeoples)
    {
        var state = Actions(actor);
        state.RandomSamples.Clear();
        state.Player(Side.FreePeoples).Hand.Clear();
        foreach (var unit in state.Units.Where(u => !u.Id.StartsWith("kings.")))
            BoardRules.ReturnToReserve(unit);
        return state;
    }

    public static CharacterState Companion(MatchState state, string id, string region)
    {
        var companion = CharacterCatalog.Companion(state, id);
        state.Fellowship.Companions.Remove(id);
        companion.Location = region;
        state.Characters.Add(companion);
        return companion;
    }

    public static void Tile(MatchState state, string sovereign, string tile = One) =>
        CorruptionRules.Attach(state, Sovereign(state, sovereign), tile);

    public static CommandPayload Cost(MatchState state, string card, DieFace face = DieFace.Event)
    {
        var side = CardCatalog.All[card].Side;
        EventTests.GiveCard(state, side, card);
        var die = Die(state, side);
        die.Face = face;
        return new() { CardId = card, CardCost = face, DieId = die.Id };
    }

    public static MatchState Play(MatchState state, string card) => Do(state, CommandKind.PlayEvent, Cost(state, card));
    public static MatchState Choose(MatchState state, string id, params int[] samples) =>
        Do(state, CommandKind.ChooseEffect, new() { Ids = [id] }, samples);

    public static MatchState Battle(string sovereign = SovereignCatalog.Dain) => KingsBattleTests.Battle(sovereign, true);

    public static MatchState Cards(MatchState state, string card, params int[] samples)
    {
        state.Player(Side.FreePeoples).Hand = [card];
        state.Player(Side.Shadow).Hand.Clear();
        state = Do(state, CommandKind.CommitCombatCard, new() { CardId = card });
        return Do(state, CommandKind.CommitCombatCard, new(), samples);
    }
}
