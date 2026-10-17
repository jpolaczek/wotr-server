using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record HealCorruptionStep(int Amount, int? WithGollum = null) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var fellowship = context.State.Fellowship;
        var amount = fellowship.Guide == "base.character.gollum" ? WithGollum ?? Amount : Amount;
        var healed = Math.Min(fellowship.Corruption, amount);
        fellowship.Corruption -= healed;
        context.Record("CorruptionHealed", $"{context.Execution.SourceId}: healed {healed} Corruption.");
    }
}

public sealed record AdvanceNationStep(bool Activate) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (Activate)
            PoliticalRules.Activate(context.State, context.Execution.NationId);
        PoliticalRules.Advance(context.State, context.Execution.NationId, PoliticalAdvanceCause.Event);
    }
}

public sealed record DrawCardsStep(string Deck, int Count = 1) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        for (var i = 0; i < Count; i++)
            CardFlow.Draw(context.State, context.Owner, Deck);
        CardFlow.RequireDiscard(context.State, context.Owner, DiscardContinuation.ResumeEffect);
    }
}

public sealed record LastingEffectStep(EffectModifier Modifier, EffectDuration Duration, EffectExpiry Expiry,
    string TargetId = "", string ScopeId = "", bool Private = false, IReadOnlyList<string>? RequiredCompanions = null) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        context.State.ActiveEffects.Add(new()
        {
            Id = $"{context.State.Id}:e{context.State.Revision}:{context.Frame.EffectId}:{context.Frame.NextStep}",
            SourceId = context.Execution.SourceId,
            Owner = context.Owner,
            TargetId = TargetId,
            Modifier = Modifier,
            Duration = Duration,
            ExpiresWhen = Expiry,
            Turn = context.State.Turn,
            ScopeId = ScopeId,
            RequiredCompanions = RequiredCompanions is null ? [] : [.. RequiredCompanions],
            OnlyFor = Private ? context.Owner : null
        });
    }
}

public sealed record ReactionWindowStep(string Window, bool Opponent = true) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        context.Execution.Wait = EffectWait.Reaction;
        context.Execution.ReactionWindow = Window;
        var responder = Opponent ? TurnFlow.Other(context.Owner) : context.Owner;
        context.State.Open(DecisionKind.Reaction, responder, [CommandKind.RespondEffect, CommandKind.DeclineReaction]);
        context.State.Pending!.Window = Window;
        context.State.Pending.SourceId = context.Execution.SourceId;
    }
}
