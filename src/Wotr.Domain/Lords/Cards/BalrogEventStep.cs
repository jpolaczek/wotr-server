using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record BalrogEventStep : EffectChoiceStep
{
    public const string Alone = "balrog.alone";
    public const string Army = "balrog.army";
    public const string Decline = "balrog.finish";
    public override string Presentation => "BalrogOrder";

    public override void Apply(EffectContext context)
    {
        var balrog = CharacterRules.InPlay(context.State, LordsCharacters.Balrog)!;
        if (balrog.Level == 0)
        {
            balrog.Level = 2;
            context.Record("BalrogActivated", "The Balrog is active (Level 2).");
            return;
        }
        base.Apply(context);
    }

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var choices = new List<string> { Decline };
        if (BalrogEventTravel.Travel(state) is { } travel && travel.Targets.Any(r => r != travel.Origin))
            choices.Add(Alone);
        if (ArmyEventOptions.Armies(state, execution.Owner, ArmyEventOrder.BalrogManeuver).Any())
            choices.Add(Army);
        return choices;
    }

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = id == Alone ? "Move Balrog alone" : id == Army ? "Move or attack with army" : "Finish",
        Detail = id == Army ? "Choose an army including the Balrog. No additional die." : ""
    };

    public override void Choose(EffectContext context, string id)
    {
        if (id == Army)
            new ArmyOrderStep(ArmyEventOrder.BalrogManeuver).Apply(context);
        else if (id == Alone)
        {
            context.Execution.Wait = EffectWait.BalrogTravel;
            context.State.Open(DecisionKind.CharacterEvent, context.Owner, [CommandKind.MoveShadowCharacters]);
            context.State.Pending!.SourceId = context.Execution.SourceId;
        }
    }
}
