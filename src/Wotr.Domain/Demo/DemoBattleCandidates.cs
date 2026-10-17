using Wotr.Contracts;

namespace Wotr.Domain;

// DEMO-02: the CPU in a battle: no combat card, every reroll it may, the attacker goes on while it has more army
// units, the defender holds or takes a siege, a won attack advances with the whole army.
internal static class DemoBattleCandidates
{
    public static IEnumerable<(CommandKind, CommandPayload)> For(DemoContext context) => context.View.Decision!.Kind switch
    {
        DecisionKind.CombatCard => DemoCandidates.Plain(CommandKind.CommitCombatCard),
        DecisionKind.BattleReroll => Rerolls(context),
        DecisionKind.BattleContinue => Stronger(context)
            ? DemoCandidates.Plain(CommandKind.ContinueBattle, CommandKind.StopBattle)
            : DemoCandidates.Plain(CommandKind.StopBattle, CommandKind.ContinueBattle),
        DecisionKind.BattleRetreat => DemoCandidates.Plain(CommandKind.HoldBattle).Concat(Retreats(context)),
        DecisionKind.BattleAdvance => Advances(context),
        DecisionKind.StrongholdDefense => DemoCandidates.Plain(CommandKind.RetreatIntoSiege, CommandKind.FightFieldBattle),
        DecisionKind.SiegeContinuation => DemoCandidates.Plain(CommandKind.StopBattle, CommandKind.ExtendSiege),
        _ => []
    };

    private static IEnumerable<(CommandKind, CommandPayload)> Rerolls(DemoContext context) =>
        context.Options(CommandKind.RerollBattle).Select(o => (CommandKind.RerollBattle, new CommandPayload { Ids = o.EligibleIds.Take(o.Maximum).ToList() }))
            .Concat(DemoCandidates.Plain(CommandKind.RerollBattle));

    private static IEnumerable<(CommandKind, CommandPayload)> Retreats(DemoContext context) =>
        from option in context.Options(CommandKind.RetreatBattle)
        from region in option.Targets
        select (CommandKind.RetreatBattle, new CommandPayload { RegionId = region });

    private static IEnumerable<(CommandKind, CommandPayload)> Advances(DemoContext context) =>
        context.Options(CommandKind.AdvanceBattle).Select(o => (CommandKind.AdvanceBattle, new CommandPayload { Ids = [.. o.EligibleIds] }))
            .Concat(DemoCandidates.Plain(CommandKind.AdvanceBattle));

    private static bool Stronger(DemoContext context)
    {
        if (BattleRuntime.Active(context.State) is not { } battle)
            return false;
        var own = BattleRules.Strength(context.State, battle.Force(context.Side));
        var other = BattleRules.Strength(context.State, battle.Force(context.Side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples));
        return own > other;
    }
}

// DEMO-02: casualties of a battle or an event. Regulars go first, then elites: removed when two hits remain, reduced
// when one does. A second plan reduces elites first; a third removes every unit when the hits exceed the army.
internal static class DemoLossCandidates
{
    public static IEnumerable<(CommandKind, CommandPayload)> For(DemoContext context)
    {
        var kind = context.State.Pending!.Commands.Contains(CommandKind.TakeEventLosses) ? CommandKind.TakeEventLosses : CommandKind.TakeBattleLosses;
        foreach (var option in context.Options(kind))
        {
            var units = option.EligibleIds.Select(id => context.State.Units.Single(u => u.Id == id)).ToList();
            var hits = Math.Max(option.Maximum, context.State.Pending.RequiredCount);
            foreach (var plan in Plans(units, hits))
                yield return (kind, new CommandPayload { Losses = plan });
        }
    }

    private static IEnumerable<List<BattleLossChoice>> Plans(IReadOnlyList<UnitState> units, int hits)
    {
        var regulars = units.Where(u => u.Kind != UnitKind.Elite).ToList();
        var elites = units.Where(u => u.Kind == UnitKind.Elite).ToList();
        yield return Pay(regulars.Concat(elites), hits);
        yield return Pay(elites.Concat(regulars), hits);
        yield return units.Select(u => new BattleLossChoice { UnitId = u.Id, Kind = BattleLossKind.Remove }).ToList();
    }

    private static List<BattleLossChoice> Pay(IEnumerable<UnitState> order, int hits)
    {
        var plan = new List<BattleLossChoice>();
        var left = hits;
        foreach (var unit in order.TakeWhile(_ => left > 0))
        {
            var remove = unit.Kind != UnitKind.Elite || left >= 2;
            plan.Add(new() { UnitId = unit.Id, Kind = remove ? BattleLossKind.Remove : BattleLossKind.Downgrade });
            left -= remove && unit.Kind == UnitKind.Elite ? 2 : 1;
        }
        return plan;
    }
}
