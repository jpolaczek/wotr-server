using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowStrategyCards04;

namespace Wotr.Tests;

// EXP-20: a recruit of any kind is one figure change per unit, from the reserve, on its command's decision step, and both
// sides read the same record of it.
public class ActionLogRecruitFactsTests
{
    private static readonly Side[] Viewers = [Side.FreePeoples, Side.Shadow];

    [Fact]
    public void EachStepOfAMusterRecordsItsOwnUnitOnce()
    {
        var state = ActionSequenceFixture.Board();
        state.Dice[0].Face = DieFace.Muster;
        var reserve = ReserveIds(state);
        var first = RecruitSequenceTests.Placement(state, UnitKind.Regular, "minas-tirith");
        state = LordsFixture.Do(state, CommandKind.Recruit, first);
        var second = RecruitSequenceTests.Placement(state, UnitKind.Regular, "pelargir");
        state = LordsFixture.Do(state, CommandKind.Recruit, second);
        AssertRecordedOnce(state, reserve, 2);
        var steps = ActionLogFamilyTests.Steps(state).Where(s => s.Command == CommandKind.Recruit).ToList();
        Assert.Equal([first.Placements[0].UnitId, second.Placements[0].UnitId], steps.Select(s => Assert.Single(s.Facts.Figures).After!.Id));
        AssertSameForBothSides(state, steps[0].ActionId);
    }

    [Fact]
    public void AMusterOfTwoInOneCommandRecordsBothOnItsDecisionStep()
    {
        var state = ActionSequenceFixture.Board();
        var die = state.Dice[0];
        die.Face = DieFace.Muster;
        var regulars = state.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(2).ToArray();
        var reserve = ReserveIds(state);
        state = LordsFixture.Do(state, CommandKind.Recruit, new()
        {
            DieId = die.Id,
            Placements = [new() { UnitId = regulars[0].Id, RegionId = "minas-tirith" }, new() { UnitId = regulars[1].Id, RegionId = "pelargir" }]
        });
        AssertRecordedOnce(state, reserve, 2);
        var steps = ActionLogFamilyTests.Steps(state).Where(s => s.Kind == "UnitRecruited").ToList();
        Assert.Equal(CommandKind.Recruit, steps[0].Command);
        Assert.Equal(2, steps[0].Facts.Figures.Count);
        var placed = steps[1];
        Assert.Empty(placed.Facts.Figures);
        Assert.Null(placed.Actor);
        Assert.Equal(["pelargir"], placed.Context.Regions);
        Assert.Contains("pelargir", placed.Text);
        AssertSameForBothSides(state, steps[0].ActionId);
    }

    [Fact]
    public void ALeaderLostAtOnceIsPlacedOnceThenLeaves()
    {
        var state = ActionSequenceFixture.Board();
        state.Dice[0].Face = DieFace.Muster;
        var leader = RecruitSequenceTests.Placement(state, UnitKind.Leader, "minas-tirith");
        var id = leader.Placements[0].UnitId;
        state = LordsFixture.Do(state, CommandKind.Recruit, leader);
        foreach (var viewer in Viewers)
        {
            var step = ActionLogFamilyTests.Steps(state, viewer).Single(s => s.Command == CommandKind.Recruit);
            var changes = step.Facts.Figures.Where(f => f.After?.Id == id).ToList();
            Assert.Equal([("reserve", "minas-tirith"), ("reserve", "casualties")], changes.Select(f => (f.From, f.To)));
        }
    }

    [Theory]
    [InlineData(SovereignCatalog.Brand)]
    [InlineData(SovereignCatalog.Dain)]
    [InlineData(SovereignCatalog.Theoden)]
    public void AKingsRulerRecruitmentRecordsEachUnitOnceOnItsAbilityStep(string sovereign)
    {
        var (state, payload) = KingsRulerTokenTests.Recruitment(sovereign);
        var reserve = ReserveIds(state);
        state = KingsFixture.Do(state, CommandKind.UseCharacterAbility, payload);
        AssertRecordedOnce(state, reserve, payload.Placements.Count);
        var steps = ActionLogFamilyTests.Steps(state);
        var ability = steps.Single(s => s.Command == CommandKind.UseCharacterAbility);
        Assert.Equal(payload.Placements.Count, ability.Facts.Figures.Count(f => f.From == "reserve"));
        Assert.All(steps.Where(s => s.Kind == "UnitRecruited"), s => Assert.Empty(s.Facts.Figures));
        AssertSameForBothSides(state, ability.ActionId);
    }

    [Theory]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Elite)]
    public void TheBalrogsEntryRecordsItsRecruitOnce(UnitKind kind)
    {
        var state = LordsCharacterFixture.State(Side.Shadow);
        var payload = LordsCharacterFixture.Entry(state, LordsCharacters.Balrog, "moria");
        payload.Placements = [new() { UnitId = state.Units.First(u => u.Nation == "sauron" && u.Kind == kind && u.Location == "reserve").Id, RegionId = "moria" }];
        var reserve = ReserveIds(state);
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        AssertRecordedOnce(state, reserve, 1);
        var entry = ActionLogFamilyTests.Steps(state).Single(s => s.Command == CommandKind.EnterCharacter);
        Assert.Contains(entry.Facts.Figures, f => f.After?.DefinitionId == LordsCharacters.Balrog && f.To == "moria");
        Assert.Single(entry.Facts.Figures, f => f.After?.Id == payload.Placements[0].UnitId);
    }

    [Fact]
    public void GothmogsRecruitRecordsItsUnitOnce()
    {
        var state = LordsCharacterFixture.State(Side.Shadow, emptyBoard: true);
        var gothmog = LordsCharacterFixture.Put(state, LordsCharacters.Gothmog, "gap-of-rohan");
        BattleFixture.Put(state, "isengard", UnitKind.Regular, gothmog.Location);
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        payload.CharacterId = gothmog.Id;
        payload.AbilityId = LordsCharacterAbilities.GothmogRecruit;
        payload.Ids = [gothmog.Location];
        var reserve = ReserveIds(state);
        state = LordsFixture.Do(state, CommandKind.UseCharacterAbility, payload);
        AssertRecordedOnce(state, reserve, 1);
    }

    [Fact]
    public void TheVoiceOfSarumanRecordsEachUnitOnce()
    {
        var state = BattleFixture.State(Side.Shadow);
        var saruman = CharacterFixture.Put(state, CharacterRules.Saruman, "orthanc");
        state.Control["north-dunland"] = Side.FreePeoples;
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        payload.CharacterId = saruman.Id;
        payload.AbilityId = CharacterAbilities.VoiceRecruit;
        payload.Ids = ["orthanc", "south-dunland"];
        var reserve = ReserveIds(state);
        state = CharacterFixture.Do(state, CommandKind.UseCharacterAbility, payload);
        AssertRecordedOnce(state, reserve, 2);
    }

    [Fact]
    public void ACardsRecruitRecordsEachUnitOnce()
    {
        var state = ShadowStrategy04Fixture.State(Pits);
        ShadowStrategy04Fixture.Reserve(state, "sauron", UnitKind.Regular, 3);
        state = ShadowStrategy04Fixture.Play(state, Pits);
        var reserve = ReserveIds(state);
        state = CharacterFixture.Do(state, CommandKind.ChooseEffect, ShadowStrategy04Fixture.Plan(state,
            ("sauron", UnitKind.Regular, "moria", 2), ("sauron", UnitKind.Regular, "morannon", 1)));
        AssertRecordedOnce(state, reserve, 3);
        Assert.All(ActionLogFamilyTests.Steps(state).Where(s => s.Kind == "EventRecruited"), s => Assert.Empty(s.Facts.Figures));
    }

    private static HashSet<string> ReserveIds(MatchState state) =>
        state.Units.Where(u => u.Location == "reserve").Select(u => u.Id).ToHashSet();

    // Every unit that left the reserve for a region has exactly one change, and no figure's origin is left blank.
    private static void AssertRecordedOnce(MatchState state, HashSet<string> reserve, int expected)
    {
        var placed = state.Units.Where(u => reserve.Contains(u.Id) && BoardRules.Regions.ContainsKey(u.Location)).ToList();
        Assert.Equal(expected, placed.Count);
        foreach (var viewer in Viewers)
        {
            var changes = ActionLogFamilyTests.Steps(state, viewer).SelectMany(s => s.Facts.Figures).ToList();
            foreach (var unit in placed)
            {
                var change = Assert.Single(changes, f => f.After?.Id == unit.Id);
                Assert.Equal(("reserve", unit.Location), (change.From, change.To));
                Assert.Equal(unit.Garrison, change.After!.Garrison);
            }
            Assert.DoesNotContain(changes, f => f.From.Length == 0 && f.After?.UnitKind is not null);
        }
    }

    private static void AssertSameForBothSides(MatchState state, string actionId)
    {
        var records = Viewers.Select(viewer => ActionLogFamilyTests.Json(
            ActionLogFixture.Action(state, actionId, viewer).Steps)).ToArray();
        Assert.Equal(records[0], records[1]);
    }
}
