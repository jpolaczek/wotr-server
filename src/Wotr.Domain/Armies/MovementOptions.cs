using Wotr.Contracts;

namespace Wotr.Domain;

public sealed class MovementOptions : IActionOptionsProvider
{
    public IEnumerable<ActionOption> GetOptions(MatchState state, Side actor)
    {
        if (ArmyEvents.Current(state, actor) is { } march && (ShadowMarchRules.Supports(march) || march == ArmyEventOrder.Corsairs))
        {
            yield return march == ArmyEventOrder.Corsairs ? CorsairRules.Option(state, actor) : ShadowMarchRules.Option(state, actor, march);
            if (ArmyEvents.Optional(march))
                yield return new() { Kind = CommandKind.FinishArmyEvent };
            yield break;
        }
        if (ArmyEvents.Current(state, actor) is { } shadowOrder && ShadowArmyEvents.Optional(shadowOrder))
        {
            yield return ShadowArmyEvents.Option(state, actor, shadowOrder, false);
            yield return new() { Kind = CommandKind.FinishArmyEvent };
            yield break;
        }
        if (ArmyEvents.Current(state, actor) is ArmyEventOrder.RohanPassage or ArmyEventOrder.CompanionMarch)
        {
            yield return ArmyEventOptions.Option(state, actor, ArmyEvents.Current(state, actor)!.Value);
            yield break;
        }
        if (state.Pending?.Owner != actor || state.Pending.Kind != DecisionKind.Action)
            yield break;
        var armies = Armies(state, actor).ToList();
        var costs = CharacterCosts.ArmyOptions(state, actor, 2)
            .Where(c => c.Face != DieFace.Character || armies.Any(a => a.CommanderIds.Count > 0))
            .Where(c => c.RequiredCharacterId == "" || armies.Any(a => a.CharacterIds.Contains(c.RequiredCharacterId))).ToList();
        yield return new()
        {
            Kind = CommandKind.MoveArmies,
            Armies = armies,
            MovementCosts = costs,
            EligibleDice = costs.Select(c => c.DieId).Distinct().ToList(),
            EligibleIds = armies.SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).ToList(),
            Targets = armies.SelectMany(a => a.Targets.Select(t => t.RegionId)).Distinct().ToList(),
            Minimum = 1,
            Maximum = 2,
            Enabled = costs.Count > 0 && armies.Count > 0,
            Reason = costs.Count == 0 ? "Requires an Army die or a Character die with a commander." : "No army has a legal move."
        };
    }

    internal static IEnumerable<ArmyOption> Armies(MatchState state, Side actor, IReadOnlySet<string>? used = null)
    {
        foreach (var group in state.Units.Where(u => !u.Garrison && BoardRules.Owner(u) == actor && BoardRules.Regions.ContainsKey(u.Location) && used?.Contains(u.Id) != true)
            .GroupBy(u => u.Location).Where(g => g.Any(BoardRules.IsArmyUnit)))
        {
            var characters = state.Characters.Where(c => c.Location == group.Key && !c.Garrison && c.Side == actor && CharacterRules.Mobile(c) && used?.Contains(c.Id) != true).ToList();
            var army = new ArmyOption
            {
                RegionId = group.Key,
                UnitIds = group.Select(u => u.Id).ToList(),
                CharacterIds = characters.Select(c => c.Id).ToList(),
                CommanderIds = group.Where(u => CharacterRules.Leads(state, u)).Select(u => u.Id).Concat(characters.Select(c => c.Id)).ToList()
            };
            foreach (var to in BoardRules.Regions[group.Key].Neighbors.Where(to => MovementRules.CanEnter(state, actor, group.Key, to)))
            {
                var eligible = group.Where(u => MovementRules.CanCrossNationalBorder(state, u, to)).ToList();
                if (eligible.Any(BoardRules.IsArmyUnit))
                    army.Targets.Add(new() { RegionId = to, EligibleIds = eligible.Select(u => u.Id).Concat(army.CharacterIds).ToList() });
            }
            if (army.Targets.Count > 0)
                yield return army;
        }
    }

}
