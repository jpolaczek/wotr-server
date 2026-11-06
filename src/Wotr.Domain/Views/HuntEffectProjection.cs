using Wotr.Contracts;

namespace Wotr.Domain;

internal static class HuntEffectProjection
{
    internal static bool PlainShadowProgram(ResolutionFrame frame, string program, string source, int step, string window,
        EffectWait wait = EffectWait.None) =>
        PlainProgram(frame, program, Side.Shadow, source, step, window, wait);

    internal static bool PlainProgram(ResolutionFrame frame, string program, Side owner, string source, int step,
        string window, EffectWait wait = EffectWait.None) =>
        frame.EffectId == program && frame.NextStep == step && frame.Window == "" && frame.ReturnDecision is null &&
        frame.Battle is null && frame.BattleLoss is null && frame.EventLoss is null && frame.Corruption is null &&
        frame.Hunt is null && frame.Fellowship is null &&
        frame.Effect is { Return: EffectReturn.Parent } effect && effect.Owner == owner && effect.Wait == wait &&
        effect.SourceId == source && effect.ReactionWindow == window && !effect.IsEvent &&
        !effect.CompletionWindowOffered && !effect.PalantirDrawPending && !effect.AnarionDrawPending &&
        !effect.FellowshipMove && effect.PaidFace == default && effect.NationId == "" && effect.RegionId == "" &&
        effect.TargetIds.Count == 0 && effect.ArmyOrder is null && effect.AppliedHits == 0 &&
        effect.InspectedHand is null && effect.InspectedSovereign is null &&
        effect.RingRecovery is null && effect.MadnessTarget is null;
}
