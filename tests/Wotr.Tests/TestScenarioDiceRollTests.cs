using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

// TEST-19 (owner's playtest, 2026-11-03): once a scenario's turn has rolled its dice, every die of the pool shows a
// result, waits in the Hunt Box or was spent earlier in the turn. A played game never leaves a die unrolled after the
// roll: a character that brings a die (a Minion, Gandalf the White, Aragorn) adds it only at the next Recover
// (CharacterLifecycle.ReconcileDice). A scenario that must still show an unrolled die is named here with the words of
// its description that explain it.
public class TestScenarioDiceRollTests
{
    private static readonly IReadOnlyDictionary<string, string> Justified = new Dictionary<string, string>();

    private static readonly GamePhase[] AfterTheRoll = [GamePhase.ActionRoll, GamePhase.ActionResolution, GamePhase.VictoryCheck];

    [Fact]
    public void NoScenarioLeavesADieUnrolledAfterTheRoll()
    {
        var unexplained = TestScenarioCatalog.All.Where(scenario => !Justified.ContainsKey(scenario.Id))
            .Select(scenario => (scenario.Id, Dice: UnrolledAfterTheRoll(scenario.Create("dice-audit"))))
            .Where(audit => audit.Dice.Count > 0)
            .Select(audit => $"{audit.Id}: {string.Join(", ", audit.Dice)}").ToList();
        Assert.Empty(unexplained);
    }

    [Fact]
    public void EveryJustifiedUnrolledDieIsExplainedByItsScenario()
    {
        foreach (var (id, explanation) in Justified)
        {
            var scenario = TestScenarioCatalog.Find(id);
            Assert.NotNull(scenario);
            Assert.Contains(explanation, scenario.Description, StringComparison.Ordinal);
            Assert.NotEmpty(UnrolledAfterTheRoll(scenario.Create("dice-audit")));
        }
    }

    [Fact]
    public void DiceBeforeTheRollStayUnrolled()
    {
        var state = TestScenarioCatalog.Find("sh-eye-hunt-allocation")!.Create("dice-audit");
        Assert.Equal(GamePhase.HuntAllocation, state.Phase);
        Assert.Contains(state.Dice, die => die.Location == DieLocation.Unrolled);
    }

    private static List<string> UnrolledAfterTheRoll(MatchState state) => AfterTheRoll.Contains(state.Phase)
        ? state.Dice.Where(die => die.Location == DieLocation.Unrolled).Select(die => die.Id).ToList()
        : [];
}
