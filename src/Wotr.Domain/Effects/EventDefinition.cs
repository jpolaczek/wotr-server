using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record EffectDefinition(string Id, IReadOnlyList<EffectStep> Steps);

public abstract record EventCondition
{
    public abstract bool CanPlay(MatchState state, Side actor);
    public virtual bool CanOpenPublicWindow(MatchState state, Side actor) => this is not PublicEventCondition || CanPlay(state, actor);
    public virtual IReadOnlyList<string> NationTargets(MatchState state, Side actor) => [];
    public virtual bool ValidTarget(MatchState state, Side actor, CommandPayload payload) => true;
}

// Opt-in: this condition may be evaluated when deciding whether a public window exists.
public abstract record PublicEventCondition : EventCondition;

public sealed record UnconditionalEvent : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => true;
}

public sealed record FreePeoplesNationTarget : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => NationTargets(state, actor).Count > 0;
    public override IReadOnlyList<string> NationTargets(MatchState state, Side actor) => state.Nations
        .Where(n => BoardRules.Nations[n.Id].Side == Side.FreePeoples).Select(n => n.Id).ToArray();
    public override bool ValidTarget(MatchState state, Side actor, CommandPayload payload) =>
        NationTargets(state, actor).Contains(payload.NationId);
}

public sealed record EventDefinition(string CardId, string ProgramId, EventCondition Condition, bool StaysOnTable = false);

// A reaction is registered for a specific window and program, never supplied as executable client data.
public sealed record EffectReaction(string Id, string Window, string ProgramId, EventCondition Condition,
    string? RequiredTableCard = null, ReactionCost Cost = ReactionCost.None, string? RequiredGuideId = null,
    string? RequiredCharacterId = null);
