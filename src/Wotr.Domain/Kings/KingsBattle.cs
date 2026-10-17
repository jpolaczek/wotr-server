using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class KingsBattleState
{
    public int BrandOfferedRound { get; set; }
    public bool BrandUsed { get; set; }
    public bool SiegeWeaknessUsed { get; set; }
}

public static class KingsBattle
{
    public const string BrandChoice = "kings.brand.combat-choice";
    public const string BrandCancel = "kings.brand.cancel-card";
    public static IReadOnlyList<EffectDefinition> Programs { get; } =
        [new(BrandChoice, [new BrandCombatChoiceStep()]), new(BrandCancel, [new BrandCancelStep()])];

    public static List<CombatReplacementView> Replacements(MatchState state) => BattleRuntime.Active(state) is { } battle
        ? new[] { battle.Attacker, battle.Defender }.Where(f => f.Round.ReplacementSource is not null)
            .Select(f => new CombatReplacementView { Side = f.Side, SourceId = f.Round.ReplacementSource! }).ToList() : [];

    public static bool OfferBrand(MatchState state, BattleState battle)
    {
        if (battle.Stage != BattleStage.BeforeRoll || !battle.CardsRevealed ||
            battle.Kings?.BrandUsed == true || battle.Kings?.BrandOfferedRound == battle.Round ||
            SovereignLifecycle.Find(state, SovereignCatalog.Brand) is not { } brand ||
            brand.Status is not (SovereignStatus.Awakened or SovereignStatus.Corrupted))
            return false;
        var fp = battle.Force(Side.FreePeoples);
        if (!BattleRules.Units(state, fp).Any(u => u.Nation == "north" && BoardRules.IsArmyUnit(u)))
            return false;
        var owner = brand.Status == SovereignStatus.Awakened ? Side.FreePeoples : Side.Shadow;
        if (battle.Force(owner).Round.CardId.Length == 0 || owner == Side.FreePeoples &&
            (!BattleRules.Characters(state, fp).Any(c => c.Id == brand.Id) || fp.Round.CancelledAbilities.Contains(brand.Id)))
            return false;
        (battle.Kings ??= new()).BrandOfferedRound = battle.Round;
        EffectRuntime.BeginProgram(state, owner, brand.Id, BrandChoice);
        return true;
    }

    public static void ReplaceCard(MatchState state, BattleState battle, Side owner)
    {
        (battle.Kings ??= new()).BrandUsed = true;
        battle.Force(owner).Round.ReplacementSource = SovereignCatalog.Brand;
        var card = battle.Force(owner).Round.CardId;
        battle.Work.RemoveAll(w => w.Owner == owner);
        battle.Work.Add(new(owner, card, BrandCancel, 0));
        battle.Work = battle.Work.OrderBy(w => w.Initiative).ThenBy(w => w.Owner == battle.Defender.Side ? 0 : 1).ToList();
        state.Events.Add(new() { Revision = state.Revision, Kind = "CombatCardReplaced", Text = $"Brand replaced {card}'s effects." });
    }

    public static int Leadership(MatchState state, BattleForce force, CharacterState character) =>
        character.Id == SovereignCatalog.Theoden && SovereignLifecycle.Find(state, character.Id)?.Status == SovereignStatus.Awakened &&
        BattleRuntime.Active(state)?.Round == 1 && !force.Round.CancelledAbilities.Contains(character.Id) ? 3 : character.Leadership;
}

public sealed record BrandCombatChoiceStep : EffectChoiceStep
{
    public override string Presentation => "CombatReplacement";
    public override IReadOnlyList<string> Candidates(MatchState state, EffectExecution execution) => ["replace", "keep"];
    public override EffectChoiceView Describe(MatchState state, EffectExecution execution, string id) => new()
    {
        Id = id, SourceId = execution.SourceId,
        Title = id == "replace" ? "Use Brand" : "Keep card effects",
        Detail = id == "replace" ? "Replace your card effects: cancel the opponent's card at initiative 0. Once per battle."
            : "Resolve the combat card as printed."
    };
    public override void Choose(EffectContext context, string id)
    {
        if (id == "replace")
            KingsBattle.ReplaceCard(context.State, BattleRuntime.Active(context.State)!, context.Owner);
    }
}

public sealed record BrandCancelStep : EffectStep
{
    public override void Apply(EffectContext context)
    {
        var opposing = BattleRuntime.Active(context.State)!.Opponent(context.Owner).Round;
        if (opposing.CardId.Length == 0 || opposing.CardCancelled)
            return;
        opposing.CardCancelled = true;
        context.Record("CombatCardCancelled", $"Brand cancelled {opposing.CardId}.");
    }
}
