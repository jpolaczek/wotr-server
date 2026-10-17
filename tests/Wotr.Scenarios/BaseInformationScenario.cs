using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

internal static class BaseInformationScenario
{
    public static Dictionary<string, MatchState> Create(MatchState movement)
    {
        return new[] { "information-loading", "information-readonly" }
            .ToDictionary(key => key, _ => ReadOnly(movement));
    }

    private static MatchState ReadOnly(MatchState movement)
    {
        var state = StateCodec.Copy(movement);
        state.Id = Guid.NewGuid().ToString("N");
        var strider = CharacterCatalog.Companion(state, "base.character.strider");
        state.Fellowship.Companions.Remove(strider.Id);
        strider.Location = "minas-tirith";
        state.Characters.Add(strider);
        state.Events.Add(new GameEvent { Kind = "InformationFixture", Text = "Public history fixture" });
        foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            state.Events.Add(new GameEvent
            {
                Kind = "InformationFixture",
                OnlyFor = side,
                Text = side == Side.FreePeoples ? "FP private history fixture" : "SH private history fixture"
            });
        TurnFlow.SelectNext(state, Side.FreePeoples);
        return state;
    }
}
