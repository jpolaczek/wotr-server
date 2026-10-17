using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.CharacterRosterFixture;

namespace Wotr.Tests;

public class CharacterRosterProjectionTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void AllEightConfigurationsHaveOnlyOwnConfiguredNamedFigures(int expansions)
    {
        var state = Actions(expansions);
        var before = StateCodec.Write(state);
        var fp = Roster(state, Side.FreePeoples);
        var sh = Roster(state, Side.Shadow);
        Assert.Equal(2 + (state.Configuration.Lords ? 2 : 0), fp.Count);
        Assert.Equal(3 + (state.Configuration.Lords ? 2 : 0) + (state.Configuration.Kings ? 3 : 0), sh.Count);
        Assert.All(fp, r => Assert.Equal(Side.FreePeoples, r.Side));
        Assert.All(sh, r => Assert.Equal(Side.Shadow, r.Side));
        Assert.Equal(fp.Count, fp.Select(r => r.FigureId).Distinct().Count());
        Assert.Equal(sh.Count, sh.Select(r => r.FigureId).Distinct().Count());
        Assert.DoesNotContain(fp, r => r.Identity is "smeagol" or "gollum" or "gimli");
        Assert.All(sh, r => Assert.Equal(CharacterRosterLifecycle.NotEntered, r.Lifecycle));
        Assert.All(sh, r => Assert.Empty(r.CurrentDefinitionId));
        Assert.All(sh, r => Assert.Null(r.Level));
        Assert.Equal(CharacterRosterLifecycle.InFellowship, fp.Single(r => r.FigureId == CharacterRules.Grey).Lifecycle);
        Assert.Equal(before, StateCodec.Write(state));
    }

    [Theory]
    [InlineData(Side.FreePeoples)]
    [InlineData(Side.Shadow)]
    public void EachProjectedEntryTargetAndCostPassesTheRealValidator(Side side)
    {
        var state = Actions(7, side, council: true);
        foreach (var nation in state.Nations)
            nation.StepsToWar = 0;
        state.Fellowship.RegionId = "old-forest-road";
        state.Fellowship.MordorStep = 0;
        state.MinionEverEntered = true;
        Put(state, CharacterRules.Grey, "fangorn");
        Put(state, CharacterRules.Strider, "minas-tirith");
        var versions = Roster(state, side).SelectMany(r => r.FutureVersions).ToArray();
        var accepted = 0;
        foreach (var version in versions)
        {
            var candidates = CharacterEntry.Candidates(state, side).Where(c => c.DefinitionId == version.DefinitionId).ToArray();
            Assert.Equal(candidates.Length > 0, version.Entry.Conditions.Single(c => c.Code == "entry-requirements").Met);
            Assert.Equal(candidates.Select(c => c.Region).Distinct().Order(), version.Entry.Targets);
            foreach (var target in version.Entry.Targets)
                foreach (var die in version.Entry.EligibleDice)
                {
                    var payload = new CommandPayload { CharacterId = version.DefinitionId, RegionId = target, DieId = die };
                    Assert.Null(LordsFixture.Engine().Validate(state, side, BattleFixture.Command(state, CommandKind.EnterCharacter, payload)));
                    accepted++;
                }
        }
        Assert.True(accepted > 0);
        TurnFlow.SelectNext(state, side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples);
        var later = Roster(state, side).SelectMany(r => r.FutureVersions).ToArray();
        Assert.Equal(versions.Select(v => v.Entry.ConditionsMet), later.Select(v => v.Entry.ConditionsMet));
        Assert.All(later, v => Assert.False(v.Entry.WindowOpen));
        Assert.All(later, v => Assert.False(v.Entry.CanAct));
    }

    [Fact]
    public void EntryConditionsCostAndWindowRemainIndependent()
    {
        var state = Actions(actor: Side.Shadow);
        CharacterEntryVersionView Entry() => Row(state, Side.Shadow, CharacterRules.Saruman).FutureVersions.Single();
        Assert.False(Entry().Entry.Conditions.Single(c => c.Code == "entry-requirements").Met);
        Assert.True(Entry().Entry.WindowOpen);
        state.Nations.Single(n => n.Id == "isengard").StepsToWar = 0;
        Assert.True(Entry().Entry.CanAct);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow))
            die.Location = DieLocation.Spent;
        Assert.True(Entry().Entry.Conditions.Single(c => c.Code == "entry-requirements").Met);
        Assert.False(Entry().Entry.Conditions.Single(c => c.Code == "entry-die").Met);
        Assert.False(Entry().Entry.CanAct);
        state.Winner = Side.FreePeoples;
        Assert.False(Entry().Entry.WindowOpen);
    }

    [Fact]
    public void CouncilChieftainEntryPreservesTheConcreteTokenAndRulerCost()
    {
        var state = Actions(3, Side.Shadow, council: true);
        state.Lords!.Council!.GrantedTokens.Add(CouncilTokens.Travel);
        var ruler = KingsDice.Ruler(state, Side.Shadow)!;
        ruler.Location = DieLocation.Available;
        ruler.Face = DieFace.Character;
        var version = Row(state, Side.Shadow, KingsChieftains.Serpent).FutureVersions.Single();
        Assert.False(version.Entry.ConditionsMet);
        Assert.True(version.CouncilEntry!.CanAct);
        Assert.Equal(CouncilTokens.Travel, Assert.Single(version.CouncilTokens));
        Assert.Contains(ruler.Id, version.CouncilEntry.EligibleDice);
        var command = BattleFixture.Command(state, CommandKind.EnterCharacter, new()
        {
            CharacterId = version.DefinitionId, RegionId = version.CouncilEntry.Targets[0],
            DieId = ruler.Id, CouncilTokenId = version.CouncilTokens[0]
        });
        Assert.Null(LordsFixture.Engine().Validate(state, Side.Shadow, command));
        var after = LordsFixture.Engine().Execute(state, Side.Shadow, command, new SequenceRandom()).State;
        Assert.Equal(CharacterRosterLifecycle.OnBoard, Row(after, Side.Shadow, KingsChieftains.Serpent).Lifecycle);
        Assert.Equal(DieLocation.Spent, after.Dice.Single(d => d.Id == ruler.Id).Location);
        Assert.Contains(CouncilTokens.Travel, after.Lords!.Council!.SpentTokens);
        var unavailable = Row(after, Side.Shadow, KingsChieftains.Mirkwood).FutureVersions.Single().CouncilEntry!;
        Assert.False(unavailable.ConditionsMet);
        Assert.DoesNotContain(unavailable.Conditions, c => c.Code == "entry-requirements");
        Assert.False(unavailable.Conditions.Single(c => c.Code == "entry-council").Met);
    }

    [Fact]
    public void PrivateCouncilVersionsCannotChangeAnyShadowPayloadOrRejectedResponse()
    {
        var standard = CouncilSetupTests.Versions();
        var alternate = CouncilSetupTests.AllVersions();
        var before = StateCodec.Write(alternate);
        Assert.Equal(KingsFixture.Json(standard, Side.Shadow), KingsFixture.Json(alternate, Side.Shadow));
        var own = Row(alternate, Side.FreePeoples, CharacterRules.Strider);
        Assert.Equal(CouncilCompanions.Strider, own.CurrentDefinitionId);
        Assert.Equal(3, own.MovementLevel);
        Assert.Equal(3, own.Level);
        var invalid = BattleFixture.Command(standard, CommandKind.EnterCharacter,
            new() { CharacterId = CharacterRules.Aragorn, RegionId = "minas-tirith", DieId = standard.Dice[0].Id });
        var entropy = new SequenceRandom();
        Assert.Equal(LordsFixture.Engine().Execute(standard, Side.Shadow, invalid, entropy).Receipt.Error,
            LordsFixture.Engine().Execute(alternate, Side.Shadow, invalid, entropy).Receipt.Error);
        Assert.Equal(0, entropy.Calls);
        Assert.Equal(before, StateCodec.Write(alternate));
    }

    [Fact]
    public void OldResponseAndUnknownFigureHistoryDoNotInventDeathOrPermission()
    {
        var old = JsonSerializer.Deserialize<PlayerView>("{}", BoardCatalog.Json)!;
        Assert.Empty(old.OwnCharacterRoster);
        var row = JsonSerializer.Deserialize<CharacterRosterView>("{}", BoardCatalog.Json)!;
        Assert.Equal(CharacterRosterLifecycle.Unknown, row.Lifecycle);
        Assert.Null(row.Level);
        Assert.Empty(row.FutureVersions);
        var state = Actions();
        state.Fellowship.Companions.Remove(CharacterRules.Grey);
        var unknown = Row(state, Side.FreePeoples, CharacterRules.Grey);
        Assert.Equal(CharacterRosterLifecycle.Unknown, unknown.Lifecycle);
        Assert.Empty(unknown.CurrentDefinitionId);
        Assert.False(unknown.FutureVersions.Single().Entry.CanAct);
    }
}
