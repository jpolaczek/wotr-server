using Wotr.Contracts;

namespace Wotr.Domain;

internal static class WornHuntProjection
{
    internal static bool Supports(MatchState state, HuntProcedure hunt, int activeIndex)
    {
        if (state.Pending is not { Kind: DecisionKind.EffectChoice, Owner: Side.Shadow } pending ||
            hunt.Stage != HuntStage.Guide || !hunt.CasualtyTaken ||
            pending.SourceId != ShadowCharacterCards03.Worn ||
            !state.Player(Side.Shadow).Table.Contains(ShadowCharacterCards03.Worn) ||
            !new HuntCasualtyCondition().CanPlay(state, Side.Shadow))
            return false;
        var reaction = state.Resolution.Frames[activeIndex + 1];
        var program = state.Resolution.Frames[activeIndex + 2];
        return HuntEffectProjection.PlainShadowProgram(reaction, EffectCatalog.ReactionProgram, hunt.SourceId, 0, "hunt.casualty") &&
            reaction.Effect!.OfferedReactions.SequenceEqual(EffectCatalog.All.Reactions.Values
                .Where(r => r.Window == "hunt.casualty").Select(r => r.Id)) &&
            (!state.Resolution.Frames.Take(activeIndex + 1).Any(f => f.Effect is not null) || reaction.Effect.Committed.Count == 0) &&
            HuntEffectProjection.PlainShadowProgram(program, ShadowCharacterCards03.WornReaction,
                ShadowCharacterCards03.Worn, 1, "", EffectWait.Choice) &&
            program.Effect!.OfferedReactions.Count == 0 && program.Effect.Committed.Count == 0;
    }
}
