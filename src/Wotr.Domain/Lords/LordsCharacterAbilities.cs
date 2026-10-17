using Wotr.Contracts;

namespace Wotr.Domain;

public static class LordsCharacterAbilities
{
    public const string ActivateBalrog = "activate-balrog";
    public const string DeactivateBalrog = "deactivate-balrog";
    public const string GothmogRecruit = "gothmog-recruit";
    public const string GothmogUpgrade = "gothmog-upgrade";

    public static bool Handles(string ability) => ability is ActivateBalrog or DeactivateBalrog or GothmogRecruit or GothmogUpgrade;

    public static IEnumerable<CharacterActionOption> Options(MatchState state, Side actor)
    {
        if (!state.Configuration.Lords || state.Lords is null)
            yield break;
        if (CharacterRules.InPlay(state, LordsCharacters.Balrog) is { } balrog)
        {
            if (actor == Side.Shadow && balrog.Level == 0)
                yield return Option(balrog, ActivateBalrog, "Activate Balrog", "Activate without moving.",
                    state.Dice.Where(d => BalrogDie(d)).Select(d => d.Id));
            if (actor == Side.FreePeoples && balrog.Level > 0)
                yield return Option(balrog, DeactivateBalrog, "Deactivate Balrog", balrog.Location == "moria" ? "Return to Level 0." : "Eliminate the Balrog.",
                    state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available &&
                        (d.Face == DieFace.WillOfTheWest || DiceRules.CanPay(d, DieFace.Character) &&
                            state.Characters.Any(c => CharacterRules.SameIdentity(c, CharacterRules.Grey) && c.Location == balrog.Location)))
                        .Select(d => d.Id));
        }
        if (actor != Side.Shadow || CharacterRules.InPlay(state, LordsCharacters.Gothmog) is not { Garrison: false } gothmog ||
            !BattleMovement.IsFree(state, actor, gothmog.Location) || !WithArmy(state, gothmog))
            yield break;
        var dice = state.Dice.Where(d => d.Owner == actor && DiceRules.CanPay(d, DieFace.Muster)).Select(d => d.Id).ToArray();
        if (Reserve(state, UnitKind.Regular) is not null)
            yield return Option(gothmog, GothmogRecruit, "Fling into the fray · recruit", "Recruit one Sauron Regular with Gothmog.", dice);
        if (Reserve(state, UnitKind.Elite) is not null && Regular(state, gothmog.Location) is not null)
            yield return Option(gothmog, GothmogUpgrade, "Fling into the fray · upgrade", "Replace a Sauron Regular with an Elite.", dice);
    }

    public static bool BalrogDie(DieState die) => die.Owner == Side.Shadow && die.Source == LordsDiceCatalog.Balrog &&
        die.Location == DieLocation.Available && die.Face == DieFace.UseBalrog && die.Special?.Chosen == true;

    private static bool WithArmy(MatchState state, CharacterState character) => state.Units.Any(u => u.Location == character.Location &&
        u.Garrison == character.Garrison && BoardRules.Owner(u) == character.Side && BoardRules.IsArmyUnit(u));
    private static UnitState? Reserve(MatchState state, UnitKind kind) => state.Units.FirstOrDefault(u =>
        u.Nation == "sauron" && u.Kind == kind && u.Location == "reserve");
    private static UnitState? Regular(MatchState state, string region) => state.Units.FirstOrDefault(u =>
        u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == region && !u.Garrison);

    private static CharacterActionOption Option(CharacterState character, string ability, string title, string detail, IEnumerable<string> dice) => new()
    {
        CharacterId = character.Id, AbilityId = ability, Title = title, Detail = detail, Targets = [character.Location], Dice = dice.ToList()
    };

    public static CharacterAbilityPlan? Plan(MatchState state, Side actor, CommandPayload payload)
    {
        var option = Options(state, actor).FirstOrDefault(o => o.CharacterId == payload.CharacterId && o.AbilityId == payload.AbilityId);
        if (option is null || !option.Dice.Contains(payload.DieId) || !DraftSelection.IsSubset(payload.Ids, option.Targets, 1, 1))
            return null;
        var die = DiceRules.AvailableDie(state, actor, payload.DieId)!;
        var cost = payload.AbilityId switch
        {
            ActivateBalrog => DieFace.UseBalrog,
            DeactivateBalrog when die.Face == DieFace.WillOfTheWest => DieFace.WillOfTheWest,
            DeactivateBalrog => DieFace.Character,
            _ => DieFace.Muster
        };
        if (payload.AbilityId is ActivateBalrog or DeactivateBalrog)
            return new(option.CharacterId, option.AbilityId, cost, "", [], []);
        var upgrade = payload.AbilityId == GothmogUpgrade;
        return new(option.CharacterId, option.AbilityId, cost, "",
            [new() { UnitId = Reserve(state, upgrade ? UnitKind.Elite : UnitKind.Regular)!.Id, RegionId = option.Targets[0] }],
            upgrade ? [Regular(state, option.Targets[0])!.Id] : []);
    }

    public static void Apply(MatchState state, CharacterAbilityPlan plan)
    {
        if (!Handles(plan.AbilityId))
            return;
        var character = state.Characters.Single(c => c.Id == plan.CharacterId);
        if (plan.AbilityId == ActivateBalrog)
            character.Level = 2;
        if (plan.AbilityId == DeactivateBalrog)
        {
            character.Level = 0;
            if (character.Location != "moria")
                CharacterLifecycle.Eliminate(state, character);
        }
    }

    public static void DescribePreview(MatchState state, CharacterAbilityPlan plan, ActionPreview preview)
    {
        if (plan.AbilityId == DeactivateBalrog && state.Characters.Single(c => c.Id == plan.CharacterId).Location != "moria")
            preview.RemovedIds.Add(plan.CharacterId);
    }
}
