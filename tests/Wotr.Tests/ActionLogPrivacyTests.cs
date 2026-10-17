using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.ActionLogFamilyTests;
using static Wotr.Tests.ActionLogPersistenceTests;

namespace Wotr.Tests;

public class ActionLogPrivacyTests
{
    [PostgresFact]
    public async Task PrivateRerollDoesNotChangeOpponentsHttpListDetailsCountsOrCursors()
    {
        await using var fixture = await CorrespondenceFixture.Create();
        var state = BattleFixture.State();
        state.Id = Guid.NewGuid().ToString("N");
        var unit = BattleFixture.Put(state, "gondor", UnitKind.Regular, BattleFixture.Origin);
        var leader = BattleFixture.Put(state, "gondor", UnitKind.Leader, BattleFixture.Origin);
        BattleFixture.Put(state, "sauron", UnitKind.Regular, BattleFixture.Target);
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, BattleFixture.Target);
        state = BattleFixture.NoCards(BattleFixture.Begin(state, [unit.Id, leader.Id]), 1, 2);
        await fixture.Save(state);
        var pages = new Dictionary<ActionLogFilter, ActionLogPage>();
        var details = new Dictionary<ActionLogFilter, string>();
        var action = state.Resolution.ActionLog!.Action.Id;
        foreach (var filter in Enum.GetValues<ActionLogFilter>())
        {
            pages[filter] = await Page(fixture.Sh, state.Id, "filter=" + filter);
            if (pages[filter].Steps.Count > 0)
                details[filter] = await fixture.Sh.GetStringAsync($"/api/matches/{state.Id}/actions/{action}?filter={filter}");
        }
        var chosen = BattleRuntime.Current(state)!.Attacker.Round.Dice[0].Id;
        await CorrespondenceFixture.Send(fixture.Fp,
            BattleFixture.Command(state, CommandKind.RerollBattle, new() { Ids = [chosen] }));
        await fixture.Restart();
        foreach (var filter in Enum.GetValues<ActionLogFilter>())
        {
            var current = await Page(fixture.Sh, state.Id, "filter=" + filter);
            Assert.Equal(Json(pages[filter]), Json(current));
            if (current.LatestCursor.Length > 0)
                Assert.Empty((await Page(fixture.Sh, state.Id, $"filter={filter}&after=" + current.LatestCursor)).Steps);
            if (details.TryGetValue(filter, out var previous))
                Assert.Equal(previous, await fixture.Sh.GetStringAsync($"/api/matches/{state.Id}/actions/{action}?filter={filter}"));
        }
        var own = await Page(fixture.Fp, state.Id);
        Assert.Equal(chosen, Assert.Single(own.Steps.Single(s => s.Command == CommandKind.RerollBattle).Facts.Targets));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CouncilPrivateVersionsCannotChangePublicFactsOrStableIdentities(bool fate)
    {
        var state = ExpansionFixture.Start(new() { Lords = true, Council = true, Fate = fate });
        var alternate = CouncilCompanions.AvailableVersions(state).First(v => v.FigureId == "base.character.legolas");
        var standard = LordsFixture.Do(state, CommandKind.ChooseCompanionVersions,
            new() { Ids = [CharacterRules.Grey], GuideId = CharacterRules.Grey });
        var other = LordsFixture.Do(state, CommandKind.ChooseCompanionVersions,
            new() { Ids = [CharacterRules.Grey, alternate.AlternateId], GuideId = CharacterRules.Grey });
        Assert.Equal(Json(Steps(standard, Side.Shadow)), Json(Steps(other, Side.Shadow)));
        Assert.DoesNotContain(alternate.AlternateId, Json(Steps(other, Side.Shadow)));
        Assert.Contains(alternate.AlternateId, Json(Steps(other)));
    }
}
