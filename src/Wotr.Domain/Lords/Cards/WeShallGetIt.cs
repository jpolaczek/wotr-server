using Wotr.Contracts;

namespace Wotr.Domain;

public static class WeShallGetIt
{
    public const string Reaction = "lords.we-shall-get-it";
    public const string Window = "hunt.shadow-replace";

    public static HuntStage AfterDraw(MatchState state) => state.Configuration.Lords
        ? HuntStage.ShadowReplacement : HuntStage.Replacement;
}

public sealed record WeShallGetItCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => state.Configuration.Lords && actor == Side.Shadow &&
        CharacterRules.GuideDefinition(state) == CharacterRules.Gollum &&
        HuntRuntime.Active(state) is { Purpose: HuntPurpose.Hunt, TileId.Length: > 0 };
}
