using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record EntsCondition : EventCondition
{
    public override bool CanPlay(MatchState state, Side actor) => CharacterRules.InPlay(state, CharacterRules.White) is not null &&
        state.Characters.Any(c => c.Location == "fangorn" && CharacterCatalog.All[c.DefinitionId].Role == "Companion");
}

public sealed record EntsAttackStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var target = ArmyTarget.At(context.State, Side.Shadow, "orthanc");
        var rolls = Enumerable.Range(0, 3).Select(_ => context.Random.Next(6) + 1).ToList();
        var hits = rolls.Count(r => r >= 4);
        context.Record("EntsAttack", $"The Ents Awake: [{string.Join(", ", rolls)}], {(target is null ? 0 : hits)} hit(s) in Orthanc.", rolls: rolls, hitTarget: 4, hits: target is null ? 0 : hits, targets: ["orthanc"]);
        if (target is not null)
        {
            PoliticalRules.ArmyAttacked(context.State, target.Units(context.State), PoliticalAttack.Ents);
            EventLosses.Begin(context, target, hits, rolls);
        }
        else if (CharacterRules.InPlay(context.State, CharacterRules.Saruman) is { Location: "orthanc" } saruman)
        {
            CharacterLifecycle.Eliminate(context.State, saruman);
            context.Record("CharacterEliminated", "The Ents Awake: Saruman eliminated in Orthanc.");
        }
    }
}

public sealed record EntsFollowUpStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var white = CharacterRules.InPlay(context.State, CharacterRules.White);
        if (white is null || white.Location != "fangorn" && BoardRules.Region(context.State, white.Location).Nation != "rohan")
            return;
        context.Execution.Wait = EffectWait.FreeEvent;
        context.State.Open(DecisionKind.Reaction, context.Owner, [CommandKind.PlayEvent, CommandKind.DeclineReaction]);
        context.State.Pending!.Window = "ents.follow-up";
        context.State.Pending.SourceId = context.Execution.SourceId;
    }
}

public sealed record RohanDefenderCondition : ICombatCondition
{
    public bool CanPlay(MatchState state, BattleState battle, Side actor) =>
        battle.Region is "fangorn" or "orthanc" || BoardRules.Region(state, battle.Region).Nation == "rohan";
}
