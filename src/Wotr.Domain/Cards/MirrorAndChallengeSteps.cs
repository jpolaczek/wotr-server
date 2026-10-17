using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public sealed record MirrorDieStep : EffectChoiceStep
{
    public override string Presentation => "ActionDie";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => state.Dice
        .Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character).Select(d => d.Id).ToList();

    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id,
        Title = "C → W",
        Detail = "Change this unused Character die to Will of the West."
    };

    public override void Choose(EffectContext context, string id)
    {
        context.State.Dice.Single(d => d.Id == id).Face = DieFace.WillOfTheWest;
        context.Record("ActionDieChanged", "Mirror of Galadriel: changed a Character die to Will of the West.");
    }
}

public sealed record MirrorHealingStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        if (context.State.Fellowship.RegionId == "lorien" && context.State.Control.GetValueOrDefault("lorien") == Side.FreePeoples)
            new HealCorruptionStep(1).Apply(context);
    }
}

public sealed record ChallengeOfTheKingCondition : EventCondition
{
    public static CharacterState? King(MatchState state) => state.Characters.FirstOrDefault(c =>
        CharacterRules.IsStriderOrAragorn(c) &&
        BoardRules.Regions.ContainsKey(c.Location) && BoardRules.Region(state, c.Location).Nation is "gondor" or "rohan" &&
        state.Units.Any(u => u.Location == c.Location && u.Garrison == c.Garrison && BoardRules.IsArmyUnit(u) && BoardRules.Owner(u) == Side.FreePeoples));

    public override bool CanPlay(MatchState state, Side actor) => King(state) is not null;
}

public sealed record ChallengeOfTheKingStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var drawn = Enumerable.Range(0, 3).Select(_ => HuntPool.Draw(context.State, context.Random)).ToList();
        var allEyes = drawn.All(t => HuntPool.Definitions[t.Id].Value == "eye");
        foreach (var tile in drawn.Where(t => t.Location != "removed"))
            tile.Location = !allEyes && HuntPool.Definitions[tile.Id].Value == "eye" ? "removed" : "pool";
        context.Record("ChallengeDraw", $"Challenge of the King: [{string.Join(", ", drawn.Select(t => HuntPool.Definitions[t.Id].Value))}].", targets: drawn.Select(t => t.Id).ToList());
        if (!allEyes)
            return;
        var king = ChallengeOfTheKingCondition.King(context.State)!;
        CharacterLifecycle.Eliminate(context.State, king);
        context.Record("CharacterEliminated", $"Challenge of the King: {CharacterCatalog.All[king.DefinitionId].Title} was eliminated.");
    }
}
