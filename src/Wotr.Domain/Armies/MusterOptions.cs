using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class MusterOptions : IActionOptionsProvider
{
    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (state.Pending?.Owner != actor)
            yield break;
        if (state.Pending.Kind == DecisionKind.RemoveExcess)
        {
            yield return new()
            {
                Kind = CommandKind.RemoveExcess,
                EligibleIds = [.. state.Pending.PrivateChoices],
                Minimum = state.Pending.RequiredCount,
                Maximum = state.Pending.RequiredCount,
                Targets = [state.Units.Single(u => u.Id == state.Pending.PrivateChoices[0]).Location]
            };
            yield break;
        }
        if (state.Pending.Kind != DecisionKind.Action)
            yield break;
        var dice = state.Dice.Where(d => d.Owner == actor && DiceRules.CanPay(d, DieFace.Muster)).Select(d => d.Id).ToList();
        var nations = state.Nations.Where(n => BoardRules.Nations[n.Id].Side == actor && PoliticalRules.CanMusterAdvance(state, n)).Select(n => n.Id).ToList();
        yield return new()
        {
            Kind = CommandKind.AdvancePolitics,
            EligibleDice = dice,
            Targets = nations,
            Enabled = dice.Count > 0 && nations.Count > 0,
            Reason = dice.Count == 0 ? "Requires a Muster die." : "No nation can advance."
        };
        var recruitment = Recruitments(state, actor).ToList();
        yield return new()
        {
            Kind = CommandKind.Recruit,
            EligibleDice = [.. dice],
            Recruitment = recruitment,
            Minimum = 1,
            Maximum = 2,
            EligibleIds = recruitment.SelectMany(r => r.ReserveIds).ToList(),
            Targets = recruitment.SelectMany(r => r.Targets).Distinct().ToList(),
            Enabled = dice.Count > 0 && recruitment.Count > 0,
            Reason = dice.Count == 0 ? "Requires a Muster die." : "No available reinforcement has a legal settlement."
        };
    }

    internal static IEnumerable<RecruitmentOption> Recruitments(MatchState state, Side actor)
    {
        foreach (var group in state.Units.Where(u => u.Location == "reserve" && BoardRules.Owner(u) == actor).GroupBy(u => (u.Nation, u.Kind)))
        {
            var representative = group.First();
            var targets = BoardRules.CurrentRegions(state).Where(r => r.Nation == group.Key.Nation && BoardRules.IsSettlement(r) &&
                RecruitmentRules.CanPlaceMuster(state, actor, representative, r.Id)).Select(r => r.Id).ToList();
            if (targets.Count > 0)
                yield return new() { Nation = group.Key.Nation, Kind = group.Key.Kind, ReserveIds = group.Select(u => u.Id).ToList(), Targets = targets };
        }
    }
}
