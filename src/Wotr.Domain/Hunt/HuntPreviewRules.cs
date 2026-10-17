using Wotr.Contracts;

namespace Wotr.Domain;

public static class HuntPreviewRules
{
    public static void Describe(MatchState state, GameCommand command, ActionPreview preview)
    {
        if (command.Kind == CommandKind.MoveFellowship)
        {
            if (KingsFarSighted.Bonus(command.Payload) > 0)
                preview.Summary = "Move the Fellowship using Far-sighted: one fewer Hunt die, with at least one if the Hunt Pool has dice.";
            var fellowship = state.Fellowship;
            preview.HasUnknownResult = fellowship.MordorStep.HasValue || state.Dice.Any(DiceRules.IsShadowHuntDie);
            preview.Fellowship = new()
            {
                RegionId = fellowship.RegionId,
                Progress = fellowship.MordorStep.HasValue ? fellowship.Progress : fellowship.Progress + 1,
                MordorStep = fellowship.MordorStep,
                Hidden = true
            };
        }
        if (command.Kind is CommandKind.RerollHunt or CommandKind.ResolveHuntDamage)
        {
            preview.Costs.Clear();
            preview.HasUnknownResult = command.Kind == CommandKind.RerollHunt &&
                (command.Payload.Ids.Count > 0 || HuntRuntime.Active(state)!.EyeDamage > 0) ||
                command.Kind == CommandKind.ResolveHuntDamage && command.Payload.HuntResponse == HuntResponse.RandomCompanion;
            if (command.Kind == CommandKind.ResolveHuntDamage && command.Payload.HuntResponse == HuntResponse.Guide && HuntOptions.View(state)?.GuideReturns != true)
                preview.RemovedIds.Add(state.Fellowship.Guide);
        }
    }
}
