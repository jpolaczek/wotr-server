using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public static class ChiefReactions
{
    public const string Spear = "lords.chief.spear-of-terror";
    public const string Follow = "lords.chief.he-sees-he-knows";

    public static IEnumerable<EffectDefinition> Programs =>
    [
        new(Spear, [new SpearOfTerrorStep()]),
        new(Follow, [new ChiefFollowStep()])
    ];

    public static IEnumerable<EffectReaction> Reactions =>
    [
        new(Spear, "event.played", Spear, new SpearOfTerrorCondition(), RequiredCharacterId: LordsCharacters.WitchKing),
        new(Follow, "fellowship.declared", Follow, new ChiefFollowCondition(), RequiredCharacterId: LordsCharacters.WitchKing)
    ];

    public static EffectExecution? PlayedEvent(MatchState state) =>
        state.Resolution.Frames.LastOrDefault(f => f.Effect is { IsEvent: true })?.Effect;

    public static bool CanDrawFor(MatchState state, Side actor, CardDefinition card) =>
        state.Configuration.Lords && actor == Side.Shadow && card.Side == actor && card.Deck == "character" &&
        CharacterRules.InPlay(state, LordsCharacters.WitchKing) is { } chief && chief.Location == state.Fellowship.RegionId;

    public static (string Title, string Detail)? Describe(string id) => id switch
    {
        Spear => ("Spear of Terror · Draw card", "Draw one Character card. This replaces any Palantír draw for the same Event."),
        Follow => ("He Sees, He Knows · Follow", "Move the Chief of the Ringwraiths to the declared Fellowship region. No die is spent."),
        _ => null
    };

    public static void DescribePreview(MatchState state, string id, ActionPreview preview)
    {
        if (id == Spear)
        {
            preview.HasUnknownResult = true;
            preview.Summary = "Draw one Character card; no second draw from Palantír for this Event.";
        }
        if (id == Follow)
        {
            preview.HasUnknownResult = FireAndShadow.Applies(state, FellowshipFlow.Active(state)!.Route);
            preview.Placements.Add(new()
            {
                UnitId = CharacterRules.InPlay(state, LordsCharacters.WitchKing)!.Id,
                RegionId = state.Fellowship.RegionId
            });
            preview.Summary = "Move the Chief of the Ringwraiths to the Fellowship.";
        }
    }
}

public sealed record SpearOfTerrorCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) =>
        ChiefReactions.PlayedEvent(state) is { Owner: Side.Shadow } played &&
        CardCatalog.All.TryGetValue(played.SourceId, out var card) && ChiefReactions.CanDrawFor(state, actor, card);
}

public sealed record SpearOfTerrorStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        ChiefReactions.PlayedEvent(context.State)!.PalantirDrawPending = false;
        CardFlow.Draw(context.State, Side.Shadow, "character");
        context.Record("CharacterAbilityUsed", "Spear of Terror: draw one Character card.");
        CardFlow.RequireDiscard(context.State, Side.Shadow, DiscardContinuation.ResumeEffect);
    }
}

public sealed record ChiefFollowCondition : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => state.Configuration.Lords && actor == Side.Shadow &&
        FellowshipFlow.Active(state) is { Kind: FellowshipProcedureKind.Declaration } &&
        LordsCharacterEntry.ChiefDestination(state, state.Fellowship.RegionId);
}

public sealed record ChiefFollowStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var chief = CharacterRules.InPlay(context.State, LordsCharacters.WitchKing)!;
        chief.Location = context.State.Fellowship.RegionId;
        chief.Garrison = false;
        context.Record("CharacterMoved", $"He Sees, He Knows: the Chief of the Ringwraiths moves to {chief.Location}.");
    }
}
