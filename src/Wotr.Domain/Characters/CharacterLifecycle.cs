using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class CharacterLifecycle
{
    public static void ApplyVersion(CharacterState character, CharacterDefinition definition)
    {
        character.DefinitionId = definition.Id;
        character.Side = definition.Side;
        character.Level = definition.Level ?? 0;
        character.Leadership = definition.Leadership ?? 0;
        character.NationIcon = definition.NationIcon ?? "";
    }

    public static void Eliminate(MatchState state, CharacterState character)
    {
        CouncilHobbitReturn.Record(state, character);
        character.Location = "casualties";
        character.Garrison = false;
        SovereignLifecycle.Reconcile(state);
        ReconcileDice(state);
    }

    public static void ReconcileDice(MatchState state)
    {
        foreach (var player in state.Players)
            player.ExtraDiceNextRecovery = state.Characters.Where(c => c.Side == player.Side && CharacterRules.OnBoard(c))
                .Sum(c => CharacterCatalog.All.GetValueOrDefault(c.DefinitionId)?.ExtraActionDice ?? 0);
    }
}
