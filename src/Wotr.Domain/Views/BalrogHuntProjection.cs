using Wotr.Contracts;

namespace Wotr.Domain;

// A paid movement/card revelation can suspend its Stronghold queue while the
// publicly discarded Balrog table card resolves. Declaration returns differ.
internal static class BalrogHuntProjection
{
    internal static bool Supports(MatchState state, int sourceIndex, FellowshipProcedure reveal)
    {
        var frames = state.Resolution.Frames;
        var reaction = frames[sourceIndex + 2];
        var program = frames[sourceIndex + 3];
        var active = frames[sourceIndex + 4];
        return reveal.Route.Contains("moria") &&
            state.Player(Side.Shadow).Discard.Contains(ShadowCharacterCards03.Balrog) &&
            !state.Player(Side.Shadow).Table.Contains(ShadowCharacterCards03.Balrog) &&
            PlainEffect(reaction, EffectCatalog.ReactionProgram, reveal.SourceId, 0, "fellowship.revealed") &&
            reaction.Effect!.OfferedReactions.SequenceEqual(EffectCatalog.All.Reactions.Values
                .Where(r => r.Window == "fellowship.revealed").Select(r => r.Id)) &&
            (sourceIndex == 0 || reaction.Effect.Committed.Count == 0) &&
            PlainEffect(program, ShadowCharacterCards03.BalrogReaction, ShadowCharacterCards03.Balrog, 1, "") &&
            program.Effect!.OfferedReactions.Count == 0 && program.Effect.Committed.Count == 0 &&
            active.EffectId == "hunt" && HuntChoiceProjection.PlainFrame(active) &&
            active.Hunt is { } hunt && BalrogHunt(hunt);
    }

    private static bool PlainEffect(ResolutionFrame frame, string program, string source, int step, string window) =>
        frame.EffectId == program && frame.NextStep == step && frame.Window == "" && frame.ReturnDecision is null &&
        frame.Battle is null && frame.BattleLoss is null && frame.EventLoss is null && frame.Corruption is null &&
        frame.Hunt is null && frame.Fellowship is null &&
        frame.Effect is { Owner: Side.Shadow, Return: EffectReturn.Parent, Wait: EffectWait.None } effect &&
        effect.SourceId == source && effect.ReactionWindow == window && !effect.IsEvent &&
        !effect.CompletionWindowOffered && !effect.PalantirDrawPending && !effect.AnarionDrawPending &&
        !effect.FellowshipMove && effect.PaidFace == default && effect.NationId == "" && effect.RegionId == "" &&
        effect.TargetIds.Count == 0 && effect.ArmyOrder is null && effect.AppliedHits == 0 &&
        effect.InspectedHand is null && effect.InspectedSovereign is null &&
        effect.RingRecovery is null && effect.MadnessTarget is null;

    private static bool BalrogHunt(HuntProcedure hunt) =>
        hunt.SourceId == ShadowCharacterCards03.Balrog && hunt.Return == HuntReturn.Parent &&
        !hunt.Movement && !hunt.Rolled && !hunt.Mordor && hunt.TileRule == HuntTileRule.IgnoreEyes &&
        hunt.DamageRule == HuntDamageRule.Normal && hunt.Purpose == HuntPurpose.Hunt &&
        !hunt.DeclarationHunt && !hunt.IgnoreRevealIcon && !hunt.FireAndShadow &&
        hunt.DrawnBy == Side.Shadow && hunt.DiceReduction == 0 && hunt.Bonus == 0 && hunt.RollBonus == 0 &&
        hunt.RerollBonus == 0 && hunt.EyeDamage == 0 && hunt.Dice.Count == 0 && hunt.RerollSources.Count == 0;
}
