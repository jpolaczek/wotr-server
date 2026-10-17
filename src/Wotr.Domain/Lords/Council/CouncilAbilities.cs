using Wotr.Contracts;

namespace Wotr.Domain;

public static class CouncilAbilities
{
    private static readonly IReadOnlyDictionary<string, string> Nations = new Dictionary<string, string>
    {
        ["messenger-gondor"] = "gondor",
        ["messenger-dwarves"] = "dwarves",
        ["messenger-north"] = "north",
        ["prince-of-mirkwood"] = "elves"
    };

    public static bool Handles(string ability) => Nations.ContainsKey(ability);
    public static bool Activates(string ability) => ability.StartsWith("messenger-", StringComparison.Ordinal);

    public static IEnumerable<CharacterActionOption> Options(MatchState state, Side actor)
    {
        if (actor != Side.FreePeoples || !state.Configuration.Council)
            yield break;
        var dice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available).Select(d => d.Id).ToList();
        foreach (var character in state.Characters.Where(c => c.Side == actor && CharacterRules.OnBoard(c)))
            foreach (var ability in Nations.Where(a => CharacterRules.Has(character, a.Key)))
            {
                var nation = state.Nations.Single(n => n.Id == ability.Value);
                if (!ValidRegion(state, character, ability.Value) ||
                    !(Activates(ability.Key) ? nation.StepsToWar > 0 || !nation.Active : PoliticalRules.CanAdvance(nation)))
                    continue;
                yield return new()
                {
                    CharacterId = character.Id,
                    AbilityId = ability.Key,
                    Title = Activates(ability.Key) ? "Messenger of Elrond" : "Prince of Mirkwood",
                    Detail = Activates(ability.Key) ? "Activate and advance " + nation.Id + "." : "Advance Elves.",
                    Targets = [character.Location],
                    Dice = dice
                };
            }
    }

    private static bool ValidRegion(MatchState state, CharacterState character, string nation)
    {
        var region = BoardRules.Region(state, character.Location);
        return nation switch
        {
            "dwarves" => character.DefinitionId == FateGimli.Alternate ? FateGimli.Homelands.Contains(region.Id) : region.Id == "erebor",
            "gondor" => region.Nation == nation && region.Settlement is "City" or "Stronghold",
            "north" => region.Nation == nation && region.Settlement == "City",
            "elves" => region.Nation == nation && region.Settlement == "Stronghold",
            _ => false
        };
    }

    public static CharacterAbilityPlan? Plan(MatchState state, Side actor, CommandPayload payload)
    {
        var option = Options(state, actor).FirstOrDefault(o => o.CharacterId == payload.CharacterId && o.AbilityId == payload.AbilityId);
        return option is not null && option.Dice.Contains(payload.DieId) &&
            DraftSelection.IsSubset(payload.Ids, option.Targets, option.TargetCount, option.TargetCount)
            ? new(option.CharacterId, option.AbilityId, null, Nations[option.AbilityId], [], []) : null;
    }

    public static int ChampionBonus(MatchState state, CharacterState character)
    {
        if (BattleRuntime.Active(state) is not { } battle)
            return 0;
        var region = BoardRules.Region(state, battle.Defender.Region);
        return (region.Id == "osgiliath" && CharacterRules.Has(character, "champion-gondor") ||
            region.Nation is not null && CharacterRules.Has(character, "champion-" + region.Nation)) ? 1 : 0;
    }
}
