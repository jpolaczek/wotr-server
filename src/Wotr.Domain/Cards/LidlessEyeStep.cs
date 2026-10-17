using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record LidlessEyeStep : EffectChoiceStep
{
    public override string Presentation => "ActionDie";
    public override int Minimum(MatchState state, EffectExecution execution) => 0;
    public override int Maximum(MatchState state, EffectExecution execution) => 3;
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => state.Dice
        .Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Select(d => d.Id).ToArray();
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = "→ Eye",
        Detail = "Turn this unused die into an Eye and place it in the Hunt Box.",
        SelectionGroup = "Dice",
        GroupMinimum = 0,
        GroupMaximum = 3
    };

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);
    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        foreach (var die in context.State.Dice.Where(d => ids.Contains(d.Id)))
        {
            die.Face = DieFace.Eye;
            die.Location = DieLocation.Hunt;
            die.HuntOrigin = "lidless-eye";
        }
        context.Record("HuntDiceAdded", $"The Lidless Eye: placed {ids.Count} unused dice in the Hunt Box.");
    }
}
