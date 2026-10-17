using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record LoreChangeStep : EffectChoiceStep
{
    public override string Presentation => "DieAndResult";
    public override int Minimum(MatchState state, EffectExecution execution) => 1;
    public override int Maximum(MatchState state, EffectExecution execution) => 2;

    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution)
    {
        var dice = LoreOfTheRings.Dice(state).ToArray();
        return [LoreOfTheRings.Decline, .. dice.Select(d => d.Id), .. dice.SelectMany(d => LoreOfTheRings.Faces(d).Keys).Distinct()];
    }

    public override bool ValidSelection(MatchState state, EffectExecution execution, IReadOnlyList<string> ids) =>
        ids.SequenceEqual([LoreOfTheRings.Decline]) || ids.Count == 2 && ids.Distinct().Count() == 2 &&
            LoreOfTheRings.Dice(state).Any(d => ids.Contains(d.Id) && ids.Any(LoreOfTheRings.Faces(d).ContainsKey));

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id)
    {
        if (id == LoreOfTheRings.Decline)
            return new() { Id = id, Title = "Keep results", Exclusive = true };
        var dice = LoreOfTheRings.Dice(state).ToArray();
        if (dice.SingleOrDefault(d => d.Id == id) is { } die)
            return new() { Id = id, Title = die.Face.ToString(), SelectionGroup = "Die",
                Detail = die.Source == "ruler" ? "Shadow Ruler die." : die.Special is null
                    ? "Standard Shadow die." : die.Source + (die.Special.RemoveSymbol ? " · Remove" : "") };
        var face = dice.SelectMany(d => LoreOfTheRings.Faces(d)).First(p => p.Key == id).Value;
        return new()
        {
            Id = id, Title = face.Face + (face.Remove ? " · Remove" : ""), SelectionGroup = "Result",
            RequiresAny = dice.Where(d => LoreOfTheRings.Faces(d).ContainsKey(id)).Select(d => d.Id).ToList(),
            Detail = face.Face == DieFace.Eye ? "Add the changed die to the Hunt Box." : "Make the changed die available."
        };
    }

    public override void DescribePreview(MatchState state, EffectExecution execution, IReadOnlyList<string> ids, ActionPreview preview)
    {
        preview.Costs.Clear();
        preview.Summary = ids.Count == 1 ? "Keep the rolled results and begin actions." :
            "Change the selected die to " + Describe(state, execution, ids.Single(id => !state.Dice.Any(d => d.Id == id))).Title + ". No action die is spent.";
    }

    public override void Choose(EffectContext context, string id) => ChooseSelection(context, [id]);

    public override void ChooseSelection(EffectContext context, IReadOnlyList<string> ids)
    {
        if (ids.Count == 1)
            return;
        var die = LoreOfTheRings.Dice(context.State).Single(d => ids.Contains(d.Id));
        var face = LoreOfTheRings.Faces(die)[ids.Single(id => id != die.Id)];
        die.Face = face.Face;
        if (die.Special is not null)
            die.Special.RemoveSymbol = face.Remove;
        die.Location = face.Face == DieFace.Eye ? DieLocation.Hunt : DieLocation.Available;
        die.HuntOrigin = face.Face != DieFace.Eye ? "" : die.Special is null ? "lore-eye" : LordsDiceCatalog.LesserEye;
        context.Record("ActionDieChanged", $"The Lore of the Elven Rings: {die.Id} changed to {face.Face}.");
    }
}
