using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class KingsChieftainCombat
{
    public const string Choice = "kings.chieftain.combat-choice";
    public const string Wrath = "kings.chieftain.red-wrath";
    public const string RelentlessWrath = "kings.chieftain.relentless-wrath";
    public const string Bats = "kings.chieftain.lord-of-the-bats";
    public const string Cancel = "kings.chieftain.cancel-card";
    public const string CommandBoth = "kings.chieftain.i-command-both";
    public const string CommandShadow = "kings.chieftain.i-command-shadow";
    public static IReadOnlyList<EffectDefinition> Programs { get; } =
    [
        new(Choice, [new ChieftainCombatChoiceStep()]),
        new(Wrath, [new CombatModifierStep(1, 0)]),
        new(RelentlessWrath, [new CombatModifierStep(2, 0)]),
        new(Bats, [new CombatModifierStep(0, 1)]),
        new(Cancel, [new ChieftainCancelStep()]),
        new(CommandBoth, [new BothArmiesModifierStep(1)]),
        new(CommandShadow, [new CombatModifierStep(1, 1)])
    ];

    public static IReadOnlyList<string> Candidates(MatchState state, BattleState battle)
    {
        var force = battle.Force(Side.Shadow);
        if (!state.Configuration.Kings || battle.CardsRevealed || force.Round.CardId.Length == 0)
            return [];
        var leadership = BattleLeadership.Available(state, force);
        return BattleRules.Characters(state, force).Where(c => KingsChieftains.Ids.Contains(c.Id) &&
            !force.Round.CancelledAbilities.Contains(c.Id) &&
            (c.Id != KingsChieftains.Mirkwood || leadership.GetValueOrDefault(c.Id) > 0))
            .Select(c => c.Id).ToArray();
    }

    public static bool Offer(MatchState state, BattleState battle)
    {
        if (Candidates(state, battle).Count == 0)
            return false;
        battle.Stage = BattleStage.BeforeReveal;
        EffectRuntime.BeginProgram(state, Side.Shadow, Choice, Choice);
        return true;
    }

    public static IReadOnlyList<CombatEffect> Effects(BattleForce force, CombatDefinition card)
    {
        bool Printed(string program) => card.Effects.Any(e => e.ProgramId == program);
        return force.Round.ReplacementSource switch
        {
            KingsChieftains.Serpent => [new(3, Printed("combat.relentless-assault") ? RelentlessWrath : Wrath)],
            KingsChieftains.Mirkwood => Printed("combat.swarm-of-bats") ? [new(0, Cancel), new(4, Bats)] : [new(4, Bats)],
            KingsChieftains.Ugluk => [new(3, Printed("combat.desperate-battle") ? CommandShadow : CommandBoth)],
            _ => card.Effects
        };
    }

    public static void Choose(MatchState state, string chieftain)
    {
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        force.Round.ReplacementSource = chieftain;
        if (chieftain == KingsChieftains.Mirkwood)
            BattleLeadership.Forfeit(state, force, new Dictionary<string, int> { [chieftain] = 1 });
        if (chieftain == KingsChieftains.Serpent)
            BattleLosses.Begin(state, force, 1, chieftain);
        state.Events.Add(new() { Revision = state.Revision, Kind = "CombatCardReplaced",
            Text = $"{CharacterCatalog.All[chieftain].Title} replaced Shadow's combat card effects." });
    }
}

public sealed record ChieftainCombatChoiceStep : EffectChoiceStep
{
    public override string Presentation => "CombatReplacement";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) =>
        KingsChieftainCombat.Candidates(state, BattleRuntime.Active(state)!).Append("keep").ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = id == "keep" ? "" : id,
        Title = id == "keep" ? "Keep card effects" : CharacterCatalog.All[id].Title,
        Detail = id switch
        {
            KingsChieftains.Serpent => "Take one hit before reveal. Combat +1 at initiative 3 (+2 with Relentless Assault).",
            KingsChieftains.Mirkwood => "Forfeit this Chieftain's Leadership. Leader re-roll +1 at initiative 4; Swarm of Bats also cancels the opposing card at 0.",
            KingsChieftains.Ugluk => "Combat and Leader re-roll +1 for both armies at initiative 3 (only Shadow with Desperate Battle).",
            _ => "Resolve the combat card as printed."
        }
    };
    public override void Choose(EffectContext context, string id)
    {
        if (id != "keep")
            KingsChieftainCombat.Choose(context.State, id);
    }
    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        var id = ids.Single();
        if (id != "keep")
        {
            preview.Costs.Add(BattleRuntime.Active(state)!.Force(Side.Shadow).Round.CardId);
            preview.Summary = Describe(state, execution, id).Detail;
        }
        if (id == KingsChieftains.Mirkwood)
            preview.Costs.Add(id);
    }
}

public sealed record ChieftainCancelStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var opposing = BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round;
        if (opposing.CardId.Length == 0 || opposing.CardCancelled)
            return;
        opposing.CardCancelled = true;
        context.Record("CombatCardCancelled", $"The Shadow of Mirkwood cancelled {opposing.CardId}.");
    }
}
