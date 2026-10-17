using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record CaptainOfDespairCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => CharacterRules.WitchKingInPlay(state) is not null &&
        CharacterRules.GuideDefinition(state) != CharacterRules.Gollum;
}

public static class CaptainOfDespair
{
    public const string HuntOrigin = "captain-of-despair";

    public static void RecordCost(MatchState state, DieState die)
    {
        if (state.Configuration.Lords && state.Lords is not null && die.Owner == Side.Shadow && die.Face == DieFace.Character &&
            state.Player(Side.Shadow).Table.Contains(LordsShadowCards.Captain))
            state.Lords.CaptainDice.Add(die.Id);
    }

    public static bool CompleteCost(MatchState state, DieState die)
    {
        if (state.Lords?.CaptainDice.Contains(die.Id) != true ||
            !state.Player(Side.Shadow).Table.Contains(LordsShadowCards.Captain))
            return false;
        die.Face = DieFace.Eye;
        die.Location = DieLocation.Hunt;
        die.HuntOrigin = HuntOrigin;
        return true;
    }

    public static void Declared(MatchState state)
    {
        var region = BoardRules.Region(state, state.Fellowship.RegionId);
        if (region is { Settlement: "Stronghold", OriginalOwner: Side.FreePeoples } &&
            state.Control.GetValueOrDefault(region.Id) == Side.FreePeoples)
            ActiveEffects.RemoveWhere(state, e => e.SourceId == LordsShadowCards.Captain);
    }
}
