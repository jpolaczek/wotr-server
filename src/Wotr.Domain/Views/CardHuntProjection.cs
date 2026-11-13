using Wotr.Contracts;

namespace Wotr.Domain;

internal static class CardHuntProjection
{
    internal static CardHuntView? For(MatchState state)
    {
        var frames = state.Resolution.Frames;
        if (frames.Count < 2 || state.Pending is null || state.Resolution.ActionSequence is not null ||
            state.Resolution.PaidDieIds.Count != 1 ||
            !state.Dice.Any(d => d.Id == state.Resolution.PaidDieIds[0] &&
                d.Owner == Side.Shadow && d.Location == DieLocation.Resolving) ||
            !EventFrame(frames[0]) || frames[1].EffectId != "hunt" ||
            !HuntChoiceProjection.PlainFrame(frames[1]) || frames[1].Hunt is not { } hunt)
            return null;
        var effect = frames[0].Effect!;
        if (!state.Player(Side.Shadow).Discard.Contains(effect.SourceId) || !CardHunt(state, hunt, effect.SourceId))
            return null;
        CardHuntStrongholdView? strongholds = null;
        var activeIndex = 1;
        if (frames.Count > 3 && frames[2].Fellowship is { AwaitingHunts: true })
        {
            var reveal = FellowshipStrongholdProjection.RevealContext(state, 1, out activeIndex);
            if (reveal is null)
                return null;
            strongholds = new()
            {
                ResolvedCardHunt = HuntOptions.View(state, hunt),
                Route = [.. reveal.Route],
                RemainingStrongholds = [.. reveal.AdditionalHuntRegions]
            };
            hunt = frames[activeIndex].Hunt!;
        }
        var revelationOffer = strongholds is not null && activeIndex == 1;
        if (!revelationOffer && !BreakingChoice(state, hunt, activeIndex) &&
            !HuntChoiceProjection.Supports(state, hunt, activeIndex))
            return null;
        var facts = new CardHuntView
        {
            CardId = effect.SourceId, CardCost = effect.PaidFace,
            PalantirDrawPending = effect.PalantirDrawPending, StrongholdHunts = strongholds
        };
        HuntChoiceProjection.CopyProgress(hunt, facts);
        return facts;
    }

    private static bool EventFrame(ResolutionFrame frame)
    {
        if (frame.Effect is not { IsEvent: true, Owner: Side.Shadow, Return: EffectReturn.FinishAction,
                Wait: EffectWait.None } effect ||
            effect.SourceId is not (ShadowCharacterCards01.OrcPatrol or ShadowCharacterCards01.IsildursBane or
                ShadowCharacterCards02.FoulThing or ShadowCharacterCards02.Strike or ShadowCharacterCards03.Breaking) ||
            frame.EffectId != effect.SourceId ||
            frame.NextStep != (effect.SourceId == ShadowCharacterCards02.Strike ? 2 : 1) ||
            frame.Window != "" || frame.ReturnDecision is not null || frame.Battle is not null ||
            frame.BattleLoss is not null || frame.EventLoss is not null || frame.Corruption is not null ||
            frame.Hunt is not null || frame.Fellowship is not null)
            return false;
        return effect.PaidFace is DieFace.Character or DieFace.Event &&
            (!effect.PalantirDrawPending || effect.PaidFace == DieFace.Event) &&
            !effect.CompletionWindowOffered && !effect.AnarionDrawPending && !effect.FellowshipMove &&
            effect.NationId == "" && effect.RegionId == "" && effect.TargetIds.Count == 0 &&
            effect.ArmyOrder is null && effect.AppliedHits == 0 && effect.ReactionWindow == "" &&
            effect.OfferedReactions.Count == 0 && effect.InspectedHand is null &&
            effect.InspectedSovereign is null && effect.RingRecovery is null && effect.MadnessTarget is null;
    }

    private static bool CardHunt(MatchState state, HuntProcedure hunt, string card)
    {
        if (hunt.SourceId != card || hunt.Return != HuntReturn.Parent || hunt.Movement ||
            hunt.DeclarationHunt || hunt.IgnoreRevealIcon || hunt.FireAndShadow ||
            hunt.DrawnBy != Side.Shadow || hunt.DiceReduction != 0)
            return false;
        if (card == ShadowCharacterCards03.Breaking)
        {
            var breakingStep = EffectCatalog.All.Programs[card].Steps.SingleOrDefault();
            return breakingStep is BreakingFellowshipStep && hunt.Purpose == HuntPurpose.CompanionSeparation &&
                !hunt.Rolled && hunt.TileRule == HuntTileRule.IgnoreEyesAndFellowship &&
                hunt.DamageRule == HuntDamageRule.Normal && hunt.Bonus == 0 && hunt.RollBonus == 0 &&
                hunt.RerollBonus == 0 && hunt.EyeDamage == 0 && hunt.Dice.Count == 0 &&
                hunt.RerollSources.Count == 0;
        }
        if (hunt.Purpose != HuntPurpose.Hunt)
            return false;
        if (card == ShadowCharacterCards02.Strike)
            return hunt.Rolled && !hunt.Mordor && hunt.TileRule == HuntTileRule.All &&
                hunt.DamageRule == HuntDamageRule.Normal &&
                hunt.RerollBonus == (hunt.Stage is HuntStage.BeforeRoll or HuntStage.Roll
                    ? 0 : KingsChieftainHunt.RerollBonus(state)) &&
                (hunt.Stage is not (HuntStage.BeforeRoll or HuntStage.Roll) || hunt.RollBonus == 0);
        var step = (DrawHuntTileStep)EffectCatalog.All.Programs[card].Steps.Single();
        return !hunt.Rolled && hunt.TileRule == step.TileRule && hunt.DamageRule == step.DamageRule &&
            hunt.Bonus == 0 && hunt.RollBonus == 0 && hunt.RerollBonus == 0 && hunt.EyeDamage == 0 &&
            hunt.Dice.Count == 0 && hunt.RerollSources.Count == 0;
    }

    private static bool BreakingChoice(MatchState state, HuntProcedure hunt, int activeIndex)
    {
        if (hunt.SourceId != ShadowCharacterCards03.Breaking || hunt.Purpose != HuntPurpose.CompanionSeparation ||
            hunt.Stage != HuntStage.Done || hunt.TileId.Length == 0 || hunt.Damage <= 0 ||
            hunt.Damage >= state.Fellowship.Companions.Count ||
            EffectCatalog.All.Programs[ShadowCharacterCards03.BreakingChoice].Steps is not [BreakingSeparationStep] ||
            state.Pending is not { Kind: DecisionKind.EffectChoice, Owner: Side.FreePeoples } pending ||
            pending.SourceId != ShadowCharacterCards03.Breaking || pending.RequiredCount != hunt.Damage ||
            state.Resolution.Frames.Count != activeIndex + 2)
            return false;

        var program = state.Resolution.Frames[activeIndex + 1];
        return HuntEffectProjection.PlainProgram(program, ShadowCharacterCards03.BreakingChoice,
                   Side.FreePeoples, ShadowCharacterCards03.Breaking, 1, "", EffectWait.Choice) &&
               program.Effect!.Committed.Count == 0 && program.Effect.OfferedReactions.Count == 0;
    }
}
