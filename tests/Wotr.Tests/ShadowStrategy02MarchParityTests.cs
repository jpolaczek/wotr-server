using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class ShadowStrategy02MarchParityTests
{
    [Theory]
    [InlineData(ArmyEventOrder.ShadowsGather, false)]
    [InlineData(ArmyEventOrder.ShadowsGather, true)]
    [InlineData(ArmyEventOrder.ShadowLengthens, false)]
    [InlineData(ArmyEventOrder.ShadowLengthens, true)]
    [InlineData(ArmyEventOrder.ShadowMoving, false)]
    [InlineData(ArmyEventOrder.ShadowMoving, true)]
    public void DenseOptionsExactlyMatchAuthoritativePlansIncludingUnfilteredRoutes(ArmyEventOrder order, bool restricted)
    {
        var state = Dense(restricted);
        var before = StateCodec.Write(state);
        var expected = Expected(state, order).Order(StringComparer.Ordinal).ToArray();
        var option = ShadowMarchRules.Option(state, Side.Shadow, order);
        var actual = option.Armies.SelectMany(army => army.Targets.Select(target =>
            Key(army.RegionId, target.Route, target.EligibleIds))).Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
        Assert.Equal(before, StateCodec.Write(state));
    }

    private static IEnumerable<string> Expected(MatchState state, ArmyEventOrder order)
    {
        var groups = state.Units.Where(u => !u.Garrison && BoardRules.Owner(u) == Side.Shadow &&
            BoardRules.Regions.ContainsKey(u.Location)).GroupBy(u => u.Location).Where(g => g.Any(BoardRules.IsArmyUnit));
        foreach (var group in groups)
        {
            var characters = state.Characters.Where(c => c.Side == Side.Shadow && !c.Garrison &&
                c.Location == group.Key && CharacterRules.Mobile(c)).Select(c => c.Id).ToList();
            // Enumerate every map walk, even through enemy or protected regions. The public
            // server Plan decides legality independently of the optimized option generator.
            foreach (var route in Walks(group.Key, ShadowMarchRules.MaximumSteps(order)))
            {
                var units = group.Where(u => route.All(region => MovementRules.CanCrossNationalBorder(state, u, region))).ToArray();
                if (!units.Any(BoardRules.IsArmyUnit))
                    continue;
                var move = new ArmyMoveChoice
                {
                    From = group.Key, To = route[^1], Route = route,
                    UnitIds = units.Select(u => u.Id).ToList(), CharacterIds = characters
                };
                var eligible = move.UnitIds.Concat(characters).ToArray();
                var legal = Legal(state, order, move);
                if (!legal && route.Contains(group.Key) && BoardRules.HasEnemyArmy(state, group.Key, Side.Shadow, true) &&
                    units.Count(BoardRules.IsArmyUnit) > 1)
                {
                    move.UnitIds.Remove(units.First(BoardRules.IsArmyUnit).Id);
                    legal = Legal(state, order, move);
                }
                if (legal)
                    yield return Key(group.Key, route, eligible);
            }
        }
    }

    private static bool Legal(MatchState state, ArmyEventOrder order, ArmyMoveChoice move)
    {
        var result = ShadowMarchRules.Plan(state, Side.Shadow, new() { Moves = [move] }, order, out var error);
        // Lengthens validates this first route before requesting its mandatory second army.
        return result != null || error == "MoreArmyMovesRequired";
    }

    private static IEnumerable<List<string>> Walks(string from, int remaining)
    {
        foreach (var target in BoardRules.Regions[from].Neighbors)
        {
            yield return [target];
            if (remaining > 1)
                foreach (var tail in Walks(target, remaining - 1))
                    yield return [target, .. tail];
        }
    }

    private static string Key(string from, IEnumerable<string> route, IEnumerable<string> ids) =>
        from + ":" + string.Join(">", route) + ":" + string.Join(",", ids.Order(StringComparer.Ordinal));

    private static MatchState Dense(bool restricted)
    {
        var state = BattleFixture.State(Side.Shadow);
        string[] strongholds = ["moria", "orthanc", "dol-guldur"];
        var regions = strongholds.Concat(BoardRules.Regions.Values
            .Where(r => r.OriginalOwner != Side.FreePeoples || r.Settlement is not ("City" or "Stronghold"))
            .OrderByDescending(r => r.Neighbors.Count).ThenBy(r => r.Id, StringComparer.Ordinal).Select(r => r.Id))
            .Distinct().Take(32).ToArray();
        var units = state.Units.Where(u => BoardRules.Owner(u) == Side.Shadow && BoardRules.IsArmyUnit(u))
            .OrderBy(u => u.Id, StringComparer.Ordinal).ToArray();
        for (var index = 0; index < regions.Length; index++)
        {
            units[index * 2].Location = regions[index];
            units[index * 2 + 1].Location = regions[index];
            if (BoardRules.IsSettlement(BoardRules.Regions[regions[index]]))
                state.Control[regions[index]] = Side.Shadow;
        }
        foreach (var region in strongholds)
        {
            state.Control[region] = Side.FreePeoples;
            BattleFixture.Put(state, "gondor", UnitKind.Regular, region).Garrison = true;
        }
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "moria");
        CharacterFixture.Put(state, CharacterRules.WitchKing, "moria");
        CharacterFixture.Put(state, CharacterRules.Saruman, "orthanc");
        BattleFixture.Put(state, "elves", UnitKind.Regular, "lorien");
        if (restricted)
        {
            state.Nations.Single(n => n.Id == "isengard").StepsToWar = 1;
            state.Nations.Single(n => n.Id == "southrons").StepsToWar = 2;
            state.ActiveEffects.Add(new()
            {
                Id = "parity-protection", Owner = Side.FreePeoples, TargetId = "north-dunland",
                Modifier = EffectModifier.PreventShadowArmyEntry, Duration = EffectDuration.UntilRemoved
            });
        }
        return state;
    }
}
