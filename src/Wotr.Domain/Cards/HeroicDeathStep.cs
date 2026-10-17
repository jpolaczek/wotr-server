using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record HeroicDeathStep : EffectChoiceStep
{
    public const string Decline = "heroic-death.decline";
    public const string HitPrefix = "heroic-death.hits.";
    public override string Presentation => "SacrificeAndHits";
    public override int Maximum(MatchState state, EffectExecution execution) => 2;

    private static Dictionary<string, int> Figures(MatchState state, Side side)
    {
        var force = BattleRuntime.Active(state)!.Force(side);
        return CombatCardFigures.Leaders(state, force).Select(u => (u.Id, Level: 1))
            .Concat(CombatCardFigures.Companions(state, force).Select(c => (c.Id, Level: CharacterRules.CombatLevel(c))))
            .ToDictionary(c => c.Id, c => c.Level);
    }

    private static int Incoming(MatchState state, Side side)
    {
        var battle = BattleRuntime.Active(state)!;
        return Math.Max(0, BattleRoll.HitCount(battle.Opponent(side)) - battle.Force(side).Round.CancelHits);
    }

    private static List<string> CanCancel(MatchState state, Side side, int amount) => Figures(state, side)
        .Where(f => f.Value >= amount && (amount > 0 || state.Characters.Any(c => c.Id == f.Key))).Select(f => f.Key).ToList();

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var figures = Figures(state, execution.Owner);
        var incoming = Incoming(state, execution.Owner);
        if (figures.Count == 0 || incoming == 0)
            return [];
        var amounts = Enumerable.Range(0, Math.Min(incoming, figures.Values.Max()) + 1)
            .Where(n => CanCancel(state, execution.Owner, n).Count > 0).Select(n => HitPrefix + n);
        return [.. figures.Keys, Decline, .. amounts];
    }

    public override bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids)
    {
        if (!base.ValidSelection(state, execution, ids))
            return false;
        if (ids.Contains(Decline))
            return ids.Count == 1;
        var hit = ids.FirstOrDefault(id => id.StartsWith(HitPrefix, StringComparison.Ordinal));
        return ids.Count == 2 && hit is not null && CanCancel(state, execution.Owner, Amount(hit)).Contains(ids.First(id => id != hit));
    }

    private static int Amount(string id) => int.Parse(id[HitPrefix.Length..], System.Globalization.CultureInfo.InvariantCulture);

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        if (id == Decline)
            return new() { Id = id, SelectionGroup = "Sacrifice", Title = "Keep all", Detail = "Do not use Heroic Death.", Exclusive = true };
        if (!id.StartsWith(HitPrefix, StringComparison.Ordinal))
            return new() { Id = id, SelectionGroup = "Sacrifice", Detail = "Eliminate this figure to cancel the selected hits." };
        var amount = Amount(id);
        return new()
        {
            Id = id,
            SelectionGroup = "Hits",
            Title = amount == 0 ? "Keep hits" : "− " + new string('●', amount),
            Detail = $"Cancel {amount} incoming hits. The selected figure is eliminated.",
            RequiresAny = CanCancel(state, execution.Owner, amount)
        };
    }

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        if (!ids.Contains(Decline))
            preview.RemovedIds.Add(ids.Single(id => !id.StartsWith(HitPrefix, StringComparison.Ordinal)));
    }

    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        if (ids.Contains(Decline))
            return;
        var figure = ids.Single(id => !id.StartsWith(HitPrefix, StringComparison.Ordinal));
        var amount = Amount(ids.Single(id => id.StartsWith(HitPrefix, StringComparison.Ordinal)));
        var unit = context.State.Units.FirstOrDefault(u => u.Id == figure);
        if (unit is not null)
            BoardRules.RemoveCasualty(context.State, unit);
        else
            CharacterLifecycle.Eliminate(context.State, context.State.Characters.Single(c => c.Id == figure));
        BattleRuntime.Active(context.State)!.Force(context.Owner).Round.CancelHits += amount;
        context.Record("HeroicDeath", $"{context.Execution.SourceId}: eliminated {figure} to cancel {amount} hits.");
    }
}
