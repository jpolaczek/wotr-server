using Wotr.Contracts;

namespace Wotr.Domain;

public static class KingsRulerRecruitment
{
    private static readonly IReadOnlyDictionary<string, string> Abilities = new Dictionary<string, string>
    {
        [SovereignCatalog.Brand] = "kings.call-to-arms",
        [SovereignCatalog.Dain] = "kings.iron-guard",
        [SovereignCatalog.Theoden] = "kings.mustering-of-the-mark"
    };

    public static bool Handles(string ability) => Abilities.Values.Contains(ability);

    private static CharacterState? Character(MatchState state, string id) =>
        Abilities.ContainsKey(id) && KingsRulerAbilities.Awakened(state, id) is { } character &&
        state.Nations.Single(n => n.Id == SovereignCatalog.All[id].Nation).StepsToWar == 0 ? character : null;

    public static IEnumerable<CharacterActionOption> Options(MatchState state, Side actor)
    {
        if (actor != Side.FreePeoples || !state.Configuration.Kings)
            yield break;
        var dice = KingsRulerAbilities.Dice(state, DieFace.Muster);
        foreach (var (id, ability) in Abilities)
        {
            if (Character(state, id) is not { } character)
                continue;
            var plan = new SovereignRecruitPlan(state, character);
            if (plan.Maximum == 0)
                continue;
            var groups = plan.Options();
            yield return new()
            {
                CharacterId = id, AbilityId = ability, Dice = [.. dice], TargetCount = plan.Maximum,
                Title = id == SovereignCatalog.Dain ? "Iron Guard" : id == SovereignCatalog.Brand ? "Call to Arms" : "Mustering of the Mark",
                Detail = id == SovereignCatalog.Dain ? "A Regular or Elite here; a Regular or Leader in another free Dwarven settlement."
                    : "A Regular or Leader in two different free national settlements, plus one in the sovereign's free region.",
                Recruitment = groups, Targets = groups.SelectMany(g => g.Targets).Distinct().ToList(),
                RulerRecruitment = new() { LocalRegion = character.Location, LocalAllowsElite = id == SovereignCatalog.Dain,
                    SettlementCount = id == SovereignCatalog.Dain ? 1 : 2, LocalMustDiffer = id == SovereignCatalog.Dain,
                    Settlements = BoardRules.CurrentRegions(state).Where(r => r.Nation == SovereignCatalog.All[id].Nation &&
                        BoardRules.IsSettlement(r) && KingsRulerAbilities.FreeRegion(state, r.Id)).Select(r => r.Id).ToList() }
            };
        }
    }

    public static CharacterAbilityPlan? Plan(MatchState state, Side actor, CommandPayload payload)
    {
        if (actor != Side.FreePeoples || string.IsNullOrEmpty(payload.CharacterId) ||
            Character(state, payload.CharacterId) is not { } character || Abilities[character.Id] != payload.AbilityId ||
            payload.Ids.Count != 0 || !KingsRulerAbilities.CanPay(state, payload.DieId, DieFace.Muster) ||
            !new SovereignRecruitPlan(state, character).Valid(payload.Placements))
            return null;
        return new(character.Id, payload.AbilityId, DieFace.Muster, "", payload.Placements, []);
    }

    public static void Apply(MatchState state, CharacterAbilityPlan plan)
    {
        RecruitmentRules.Place(state, Side.FreePeoples, plan.Placements);
        state.Events.Add(new() { Revision = state.Revision, Kind = "CharacterAbilityUsed", Text = $"{plan.CharacterId}: {plan.AbilityId}." });
    }
}
