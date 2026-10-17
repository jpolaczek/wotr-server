using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsUpgrades
{
    private static readonly IReadOnlyDictionary<string, string> Abilities = new Dictionary<string, string>
    {
        [SovereignCatalog.Dain] = "kings.dwarvenking",
        [SovereignCatalog.Denethor] = "kings.stewardship",
        [SovereignCatalog.Thranduil] = "kings.elvenking"
    };

    public static bool Handles(string ability) => Abilities.Values.Contains(ability);

    private static (UnitState Regular, UnitState Elite)? Pair(MatchState state, CharacterState character)
    {
        var definition = SovereignCatalog.All[character.Id];
        if (character.Location != definition.Home || state.Nations.Single(n => n.Id == definition.Nation).StepsToWar != 0)
            return null;
        var regular = state.Units.FirstOrDefault(u => u.Kind == UnitKind.Regular && u.Nation == definition.Nation &&
            u.Location == character.Location && u.Garrison == character.Garrison);
        var elite = state.Units.FirstOrDefault(u => u.Kind == UnitKind.Elite && u.Nation == definition.Nation && u.Location == "reserve");
        return regular is not null && elite is not null && !KingsWeaknesses.BlocksRecruit(state, elite, character.Location)
            ? (regular, elite) : null;
    }

    public static IEnumerable<CharacterActionOption> Options(MatchState state, Side actor)
    {
        if (actor != Side.FreePeoples || !state.Configuration.Kings || state.Kings is null)
            yield break;
        foreach (var sovereign in state.Kings.Sovereigns.Where(s => s.Status == SovereignStatus.Awakened && Abilities.ContainsKey(s.Id)))
        {
            var character = state.Characters.Single(c => c.Id == sovereign.Id);
            if (Pair(state, character) is null)
                continue;
            yield return new()
            {
                CharacterId = sovereign.Id, AbilityId = Abilities[sovereign.Id], Title = "Upgrade a Regular",
                Detail = "Return one Regular from this army to the reserves and recruit an Elite, including during a siege.",
                Targets = [character.Location],
                Dice = state.Dice.Where(d => d.Owner == actor && d.Location == DieLocation.Available && DiceRules.CanPay(d, DieFace.Muster))
                    .Select(d => d.Id).ToList()
            };
        }
    }

    public static CharacterAbilityPlan? Plan(MatchState state, Side actor, CommandPayload payload)
    {
        var option = Options(state, actor).FirstOrDefault(o => o.CharacterId == payload.CharacterId && o.AbilityId == payload.AbilityId);
        if (option is null || !option.Dice.Contains(payload.DieId) || payload.Placements.Count != 0 ||
            !payload.Ids.SequenceEqual(option.Targets))
            return null;
        var character = state.Characters.Single(c => c.Id == option.CharacterId);
        var pair = Pair(state, character)!.Value;
        return new(character.Id, option.AbilityId, DieFace.Muster, "",
            [new() { UnitId = pair.Elite.Id, RegionId = character.Location }], [pair.Regular.Id]);
    }

    public static void Apply(MatchState state, CharacterAbilityPlan plan)
    {
        BoardRules.ReturnToReserve(state.Units.Single(u => u.Id == plan.ReturnedIds.Single()));
        var character = state.Characters.Single(c => c.Id == plan.CharacterId);
        var elite = state.Units.Single(u => u.Id == plan.Placements.Single().UnitId);
        elite.Location = character.Location;
        elite.Garrison = character.Garrison;
        state.Events.Add(new() { Revision = state.Revision, Kind = "CharacterAbilityUsed", Text = $"{character.Id}: {plan.AbilityId}." });
    }
}
