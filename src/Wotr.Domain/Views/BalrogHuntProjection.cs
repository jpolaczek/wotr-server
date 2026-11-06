using Wotr.Contracts;

namespace Wotr.Domain;

// Balrog suspends a public declaration/revelation while its table-card Hunt
// resolves. The enclosing Fellowship procedure determines reveal treatment.
internal static class BalrogHuntProjection
{
    internal static bool Supports(MatchState state, int fellowshipIndex, FellowshipProcedure reveal)
    {
        var frames = state.Resolution.Frames;
        var reaction = frames[fellowshipIndex + 1];
        var program = frames[fellowshipIndex + 2];
        var active = frames[fellowshipIndex + 3];
        var declaration = reveal.Kind == FellowshipProcedureKind.Declaration;
        var window = declaration ? "fellowship.declared" : "fellowship.revealed";
        var region = BoardRules.Region(state, reveal.Route[^1]);
        var ignoreReveal = declaration && region.OriginalOwner == Side.FreePeoples &&
            region.Settlement is "City" or "Stronghold";
        var reactionValid = declaration
            ? HuntChoiceProjection.DeclarationReactionsFrame(state, reaction, EffectWait.None)
            : HuntEffectProjection.PlainShadowProgram(reaction, EffectCatalog.ReactionProgram, reveal.SourceId, 0, window) &&
              reaction.Effect!.OfferedReactions.SequenceEqual(EffectCatalog.All.Reactions.Values
                  .Where(r => r.Window == window).Select(r => r.Id));
        return reveal.Route.Contains("moria") &&
            state.Player(Side.Shadow).Discard.Contains(ShadowCharacterCards03.Balrog) &&
            !state.Player(Side.Shadow).Table.Contains(ShadowCharacterCards03.Balrog) &&
            reactionValid &&
            (fellowshipIndex <= 1 || reaction.Effect.Committed.Count == 0) &&
            HuntEffectProjection.PlainShadowProgram(program, ShadowCharacterCards03.BalrogReaction, ShadowCharacterCards03.Balrog, 1, "") &&
            program.Effect!.OfferedReactions.Count == 0 && program.Effect.Committed.Count == 0 &&
            active.EffectId == "hunt" && HuntChoiceProjection.PlainFrame(active) &&
            active.Hunt is { } hunt && BalrogHunt(hunt, declaration, ignoreReveal);
    }

    private static bool BalrogHunt(HuntProcedure hunt, bool declaration, bool ignoreReveal) =>
        hunt.SourceId == ShadowCharacterCards03.Balrog && hunt.Return == HuntReturn.Parent &&
        !hunt.Movement && !hunt.Rolled && !hunt.Mordor && hunt.TileRule == HuntTileRule.IgnoreEyes &&
        hunt.DamageRule == HuntDamageRule.Normal && hunt.Purpose == HuntPurpose.Hunt &&
        hunt.DeclarationHunt == declaration && hunt.IgnoreRevealIcon == ignoreReveal && !hunt.FireAndShadow &&
        hunt.DrawnBy == Side.Shadow && hunt.DiceReduction == 0 && hunt.Bonus == 0 && hunt.RollBonus == 0 &&
        hunt.RerollBonus == 0 && hunt.EyeDamage == 0 && hunt.Dice.Count == 0 && hunt.RerollSources.Count == 0;
}
