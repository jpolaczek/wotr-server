using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class LordsCharacterFixture
{
    public static MatchState State(Side actor = Side.FreePeoples, bool emptyBoard = false)
    {
        var state = LordsFixture.Actions();
        if (emptyBoard)
            foreach (var unit in state.Units)
                BoardRules.ReturnToReserve(unit);
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    public static CharacterState Put(MatchState state, string definition, string region, string? figure = null)
    {
        var character = new CharacterState { Id = figure ?? definition, Location = region };
        CharacterLifecycle.ApplyVersion(character, CharacterCatalog.All[definition]);
        state.Characters.Add(character);
        state.Fellowship.Companions.Remove(character.Id);
        CharacterLifecycle.ReconcileDice(state);
        return character;
    }

    public static CommandPayload Entry(MatchState state, string definition, string region)
    {
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        payload.CharacterId = definition;
        payload.RegionId = region;
        return payload;
    }

    public static void War(MatchState state, params string[] nations)
    {
        foreach (var nation in state.Nations.Where(n => nations.Contains(n.Id)))
        {
            nation.Active = true;
            nation.StepsToWar = 0;
        }
    }
}
