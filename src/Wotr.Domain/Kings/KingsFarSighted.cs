using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsFarSighted
{
    public const string Ability = "kings.far-sighted";
    public static int Bonus(CommandPayload payload) => payload.AbilityId == Ability ? 1 : 0;
    private static bool Available(MatchState state) => !state.Fellowship.MordorStep.HasValue &&
        KingsRulerAbilities.Awakened(state, SovereignCatalog.Thranduil) is not null;

    public static bool Valid(MatchState state, CommandPayload payload) => payload.AbilityId == "" ||
        payload.AbilityId == Ability && state.Pending?.Kind == DecisionKind.Action && Available(state) &&
        KingsRulerAbilities.CanPay(state, payload.DieId, DieFace.Character);

    public static RulerTravelOption? Option(MatchState state, CommandKind kind)
    {
        if (state.Pending?.Kind != DecisionKind.Action || !Available(state))
            return null;
        var dice = KingsRulerAbilities.Dice(state, DieFace.Character);
        if (dice.Count == 0)
            return null;
        return new()
        {
            AbilityId = Ability, Dice = dice, HuntDiceReduction = kind == CommandKind.MoveFellowship ? 1 : 0,
            RangeBonus = kind == CommandKind.MoveFellowship ? 0 : 1,
            Travel = kind == CommandKind.MoveFellowship ? [] : FellowshipOptions.RulerTravel(state, kind)
        };
    }
}
