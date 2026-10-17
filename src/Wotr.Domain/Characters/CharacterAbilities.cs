using Wotr.Contracts;

namespace Wotr.Domain;

public sealed record CharacterAbilityPlan(string CharacterId, string AbilityId, DieFace? Cost, string Nation,
    IReadOnlyList<UnitPlacementChoice> Placements, IReadOnlyList<string> ReturnedIds);

public static class CharacterAbilities
{
    public const string VoiceRecruit = "voice-recruit";
    public const string VoiceUpgrade = "voice-upgrade";
    private static readonly IReadOnlyDictionary<string, string> PoliticalAbilities = new Dictionary<string, string>
    {
        ["politics-dwarves-at-erebor"] = "dwarves",
        [FateGimli.Renown] = "dwarves",
        ["politics-gondor-at-city-or-stronghold"] = "gondor",
        ["politics-elves-at-stronghold"] = "elves"
    };

    public static IEnumerable<CharacterActionOption> Options(MatchState state, Side actor)
    {
        foreach (var option in KingsUpgrades.Options(state, actor))
            yield return option;
        foreach (var option in KingsRulerRecruitment.Options(state, actor))
            yield return option;
        foreach (var option in LordsCharacterAbilities.Options(state, actor))
            yield return option;
        foreach (var option in CouncilAbilities.Options(state, actor))
            yield return option;
        foreach (var character in state.Characters.Where(c => c.Side == actor && CharacterRules.OnBoard(c)))
        {
            foreach (var ability in PoliticalAbilities.Where(a => CharacterRules.Has(character, a.Key)))
            {
                if (!CanAdvance(state, character, ability.Value))
                    continue;
                yield return new()
                {
                    CharacterId = character.Id,
                    AbilityId = ability.Key,
                    Title = ability.Key == FateGimli.Renown ? "Dwarf of Renown" : "Advance " + ability.Value,
                    Detail = "Use any available die to advance the nation's politics.",
                    Targets = [character.Location],
                    Dice = Available(state, actor, null)
                };
            }
            if (character.DefinitionId != CharacterRules.Saruman || !CanUseVoice(state))
                continue;
            var targets = VoiceTargets(state);
            var count = Math.Min(targets.Count, Reserve(state, UnitKind.Regular).Count);
            if (count > 0)
                yield return new()
                {
                    CharacterId = character.Id,
                    AbilityId = VoiceRecruit,
                    Title = "Voice · recruit",
                    Detail = "One Isengard Regular in each available settlement.",
                    Targets = targets,
                    TargetCount = count,
                    Dice = Available(state, actor, DieFace.Muster)
                };
            var upgrades = UpgradeCount(state);
            if (upgrades > 0)
                yield return new()
                {
                    CharacterId = character.Id,
                    AbilityId = VoiceUpgrade,
                    Title = "Voice · upgrade",
                    Detail = upgrades == 2 ? "Replace two Isengard Regulars in Orthanc with two Elites."
                        : "One replacement pair is available: replace one Isengard Regular in Orthanc with one Elite.",
                    Targets = ["orthanc"],
                    Dice = Available(state, actor, DieFace.Muster)
                };
        }
    }

    private static List<string> Available(MatchState state, Side actor, DieFace? face) => state.Dice
        .Where(d => d.Owner == actor && d.Location == DieLocation.Available && (!face.HasValue || DiceRules.CanPay(d, face.Value)))
        .Select(d => d.Id).ToList();

    private static bool CanAdvance(MatchState state, CharacterState character, string nation)
    {
        if (CharacterRules.Has(character, FateGimli.Renown))
            return FateGimli.Homelands.Contains(character.Location) &&
                PoliticalRules.CanAdvance(state.Nations.Single(n => n.Id == nation));
        var region = BoardRules.Region(state, character.Location);
        return region.Nation == nation && CharacterEntry.Unconquered(state, region.Id, Side.FreePeoples) &&
            (nation == "dwarves" ? region.Id == "erebor" : nation == "elves" ? region.Settlement == "Stronghold" : region.Settlement is "City" or "Stronghold") &&
            PoliticalRules.CanAdvance(state.Nations.Single(n => n.Id == nation));
    }

    private static bool CanUseVoice(MatchState state) => CharacterEntry.Unconquered(state, "orthanc", Side.Shadow) &&
        !BoardRules.Besieged(state, "orthanc", Side.Shadow);

    private static List<string> VoiceTargets(MatchState state) => BoardRules.CurrentRegions(state)
        .Where(r => r.Nation == "isengard" && BoardRules.IsSettlement(r) && CharacterEntry.Unconquered(state, r.Id, Side.Shadow) &&
            !BoardRules.Besieged(state, r.Id, Side.Shadow)).Select(r => r.Id).ToList();

    private static List<UnitState> Reserve(MatchState state, UnitKind kind) => state.Units
        .Where(u => u.Nation == "isengard" && u.Kind == kind && u.Location == "reserve").OrderBy(u => u.Id, StringComparer.Ordinal).ToList();
    private static List<UnitState> UpgradeRegulars(MatchState state) => state.Units
        .Where(u => u.Nation == "isengard" && u.Kind == UnitKind.Regular && u.Location == "orthanc" && !u.Garrison).OrderBy(u => u.Id, StringComparer.Ordinal).ToList();
    private static int UpgradeCount(MatchState state) => Math.Min(2, Math.Min(UpgradeRegulars(state).Count, Reserve(state, UnitKind.Elite).Count));

    public static CharacterAbilityPlan? Plan(MatchState state, Side actor, CommandPayload payload)
    {
        if (KingsUpgrades.Handles(payload.AbilityId))
            return KingsUpgrades.Plan(state, actor, payload);
        if (KingsRulerRecruitment.Handles(payload.AbilityId))
            return KingsRulerRecruitment.Plan(state, actor, payload);
        if (CouncilAbilities.Handles(payload.AbilityId))
            return CouncilAbilities.Plan(state, actor, payload);
        if (LordsCharacterAbilities.Handles(payload.AbilityId))
            return LordsCharacterAbilities.Plan(state, actor, payload);
        var option = Options(state, actor).FirstOrDefault(o => o.CharacterId == payload.CharacterId && o.AbilityId == payload.AbilityId);
        if (option is null || !option.Dice.Contains(payload.DieId) ||
            !DraftSelection.IsSubset(payload.Ids, option.Targets, option.TargetCount, option.TargetCount))
            return null;
        if (PoliticalAbilities.TryGetValue(payload.AbilityId, out var nation))
            return new(option.CharacterId, option.AbilityId, null, nation, [], []);
        var upgrading = payload.AbilityId == VoiceUpgrade;
        var reserve = Reserve(state, upgrading ? UnitKind.Elite : UnitKind.Regular);
        var targets = upgrading ? Enumerable.Repeat("orthanc", UpgradeCount(state)).ToArray() : payload.Ids.ToArray();
        var placements = targets.Select((region, index) => new UnitPlacementChoice { UnitId = reserve[index].Id, RegionId = region }).ToArray();
        return new(option.CharacterId, option.AbilityId, DieFace.Muster, "", placements,
            upgrading ? UpgradeRegulars(state).Take(targets.Length).Select(u => u.Id).ToArray() : []);
    }

    public static void Apply(MatchState state, CharacterAbilityPlan plan)
    {
        if (KingsUpgrades.Handles(plan.AbilityId))
        {
            KingsUpgrades.Apply(state, plan);
            return;
        }
        if (KingsRulerRecruitment.Handles(plan.AbilityId))
        {
            KingsRulerRecruitment.Apply(state, plan);
            return;
        }
        LordsCharacterAbilities.Apply(state, plan);
        if (CouncilAbilities.Activates(plan.AbilityId))
            PoliticalRules.Activate(state, plan.Nation);
        if (plan.Nation != "")
            PoliticalRules.Advance(state, plan.Nation, PoliticalAdvanceCause.CompanionAbility);
        foreach (var id in plan.ReturnedIds)
            BoardRules.ReturnToReserve(state.Units.Single(u => u.Id == id));
        foreach (var placement in plan.Placements)
        {
            var unit = state.Units.Single(u => u.Id == placement.UnitId);
            unit.Location = placement.RegionId;
            unit.Garrison = false;
        }
        state.Events.Add(new() { Revision = state.Revision, Kind = "CharacterAbilityUsed", Text = $"{plan.CharacterId}: {plan.AbilityId}." });
    }
}
