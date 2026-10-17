using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class KingsFixture
{
    public const int SetupSamples = 100;

    public static MatchState Setup()
    {
        var state = MatchFactory.Create("kings-test", new SequenceRandom(new int[KingsFixture.SetupSamples]), new() { Kings = true });
        KingsSetup.Initialize(state);
        return state;
    }

    public static MatchState Actions(Side actor = Side.FreePeoples)
    {
        var state = Setup();
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.Guide = CharacterRules.Strider;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Face = DieFace.Muster;
            die.Location = DieLocation.Available;
        }
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    public static SovereignState Sovereign(MatchState state, string id) => state.Kings!.Sovereigns.Single(s => s.Id == id);
    public static DieState Die(MatchState state, Side side = Side.FreePeoples) =>
        state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available);

    // A chosen Ruler result for focused ability scenarios; recovery/rolling has separate integration tests.
    public static DieState Ruler(MatchState state, DieFace face)
    {
        var die = KingsDice.Ruler(state, Side.FreePeoples)!;
        die.Face = face;
        die.Location = DieLocation.Available;
        return die;
    }

    public static void Pool(MatchState state, params string[] ids)
    {
        foreach (var tile in state.HuntTiles)
            tile.Location = ids.Contains(tile.Id) ? "pool" : "set-aside";
    }

    public static void Eyes(MatchState state, int count)
    {
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Take(count))
        {
            die.Location = DieLocation.Hunt;
            die.Face = DieFace.Eye;
            die.HuntOrigin = "allocated";
        }
    }

    public static MatchState Attempt(int eyes = 2, string sovereign = SovereignCatalog.Dain, params string[] tiles)
    {
        var state = Actions(Side.Shadow);
        Eyes(state, eyes);
        Pool(state, tiles.Length > 0 ? tiles : [HuntTests.Three]);
        return Do(state, CommandKind.AttemptCorruption,
            new() { CharacterId = sovereign, DieId = CorruptionRules.Eyes(state).First().Id }, 0);
    }

    public static MatchState Do(MatchState state, CommandKind kind, CommandPayload? payload = null, params int[] samples) =>
        LordsFixture.Do(state, kind, payload, new SequenceRandom(samples));

    public static string Json(MatchState state, Side viewer) => JsonSerializer.Serialize(LordsFixture.Views().For(state, viewer), BoardCatalog.Json);
}
