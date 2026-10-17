using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public enum CharacterReaction { GreyDraw, WhiteRider, Sorcerer, HobbitGuide, GollumReveal }

public sealed record CharacterReactionCondition(CharacterReaction Ability) : PublicEventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => Ability switch
    {
        CharacterReaction.GreyDraw => actor == Side.FreePeoples && CharacterRules.GuideDefinition(state) == CharacterRules.Grey &&
            state.Resolution.Frames.Any(f => f.Effect is { Return: EffectReturn.FinishAction, PaidFace: DieFace.Event } e &&
                e.Owner == actor && CardCatalog.All.ContainsKey(e.SourceId)),
        CharacterReaction.WhiteRider => actor == Side.FreePeoples && BattleRuntime.Active(state) is { Round: 1, WhiteRiderUsed: false } battle &&
            BattleRules.Characters(state, battle.Force(actor)).Any(c => c.DefinitionId == CharacterRules.White),
        CharacterReaction.Sorcerer => actor == Side.Shadow && BattleRuntime.Active(state) is { Round: 1 } battle &&
            battle.Force(actor).Round.CardId != "" && BattleRules.Characters(state, battle.Force(actor)).Any(c => c.DefinitionId == CharacterRules.WitchKing),
        CharacterReaction.HobbitGuide => actor == Side.FreePeoples && HuntRuntime.Active(state) is { Damage: > 0, DamageRule: not HuntDamageRule.CorruptionOnly },
        CharacterReaction.GollumReveal => actor == Side.FreePeoples && GollumHunt.CanReveal(state),
        _ => false
    };
}

public sealed record CharacterReactionStep(CharacterReaction Ability) : EffectStep
{
    public override void Apply(EffectContext context)
    {
        switch (Ability)
        {
            case CharacterReaction.GreyDraw:
                var root = context.State.Resolution.Frames.First(f => f.Effect?.Return == EffectReturn.FinishAction).Effect!;
                Draw(context, root.SourceId);
                break;
            case CharacterReaction.Sorcerer:
                Draw(context, BattleRuntime.Active(context.State)!.Force(Side.Shadow).Round.CardId);
                break;
            case CharacterReaction.WhiteRider:
                BattleRuntime.Active(context.State)!.WhiteRiderUsed = true;
                break;
            case CharacterReaction.HobbitGuide:
                HobbitDeparture.UseGuide(context.State);
                break;
            case CharacterReaction.GollumReveal:
                var hunt = HuntRuntime.Active(context.State)!;
                hunt.Damage = Math.Max(0, hunt.Damage - 1);
                hunt.Reveal = true;
                hunt.VoluntaryReveal = true;
                break;
        }
        context.Record("CharacterAbilityUsed", $"{context.Execution.SourceId}: {Ability}.");
    }

    private static void Draw(EffectContext context, string cardId)
    {
        CardFlow.Draw(context.State, context.Owner, CardCatalog.All[cardId].Deck);
        if (BattleRuntime.Active(context.State) is null)
            CardFlow.RequireDiscard(context.State, context.Owner, DiscardContinuation.ResumeEffect);
    }
}

public static class CharacterReactions
{
    public static (string Title, string Detail) Describe(EffectReaction reaction) => reaction.Condition is CharacterReactionCondition c ? c.Ability switch
    {
        CharacterReaction.GreyDraw => ("Draw matching card", "Gandalf the Grey: draw from the deck matching the Event card just resolved."),
        CharacterReaction.Sorcerer => ("Sorcerer · Draw card", "The Witch-king: draw from the deck matching the Combat card used in this battle's first round."),
        CharacterReaction.WhiteRider => ("White Rider", "Forfeit Gandalf's Leadership to cancel all Nazgûl Leadership, including the Witch-king, for this battle."),
        CharacterReaction.HobbitGuide => ("Separate · Reduce damage", "Separate this Companion to reduce Hunt damage by one. Companions separating against this tile move together."),
        CharacterReaction.GollumReveal => ("Reveal · Reduce damage", "Reveal the Fellowship to reduce Hunt damage by one, to a minimum of zero."),
        _ => ("", "")
    } : ("", "");

    private static readonly (string Id, string Source, string Window, CharacterReaction Ability, bool Guide)[] Definitions =
    [
        ("grey-draw", CharacterRules.Grey, "event.completed", CharacterReaction.GreyDraw, true),
        ("white-rider", CharacterRules.White, "battle.start", CharacterReaction.WhiteRider, false),
        ("sorcerer", CharacterRules.WitchKing, "battle.round.closed", CharacterReaction.Sorcerer, false),
        ("merry-guide", "base.character.merry", "hunt.guide", CharacterReaction.HobbitGuide, true),
        ("pippin-guide", "base.character.pippin", "hunt.guide", CharacterReaction.HobbitGuide, true),
        ("gollum-reveal", CharacterRules.Gollum, "hunt.guide", CharacterReaction.GollumReveal, true)
    ];

    public static IEnumerable<EffectDefinition> Programs => Definitions.Select(d =>
        new EffectDefinition("character." + d.Id, [new CharacterReactionStep(d.Ability)]));
    public static IEnumerable<EffectReaction> Reactions => Definitions.Select(d => new EffectReaction(
        "character." + d.Id, d.Window, "character." + d.Id, new CharacterReactionCondition(d.Ability),
        RequiredGuideId: d.Guide ? d.Source : null, RequiredCharacterId: d.Guide ? null : d.Source));
}
