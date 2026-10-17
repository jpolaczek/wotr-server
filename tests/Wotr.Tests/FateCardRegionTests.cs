using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class FateCardRegionTests
{
    [Fact]
    public void PreRegisteredPitsProgramRecruitsInFateEreborAndStillRejectsItForBase()
    {
        var program = EffectCatalog.All.Programs[ShadowStrategyCards04.Pits];
        var state = FateFixture.Actions(Side.Shadow);
        state.Nations.Single(n => n.Id == "sauron").StepsToWar = 0;
        var before = state.Units.Count(u => u.Location == "erebor");
        state = LordsShadowCardsFixture.Play(state, ShadowStrategyCards04.Pits);
        var option = new EffectOptions(EffectCatalog.All).GetOptions(state, Side.Shadow).Single();
        Assert.Contains("erebor", option.Targets);
        var reserve = state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(6).ToArray();
        var destinations = new[] { "erebor", "erebor", "barad-dur", "barad-dur", "dol-guldur", "dol-guldur" };
        state = KingsFixture.Do(state, CommandKind.ChooseEffect, new()
        {
            Placements = reserve.Select((u, i) => new UnitPlacementChoice { UnitId = u.Id, RegionId = destinations[i] }).ToList()
        });
        Assert.Equal(before + 2, state.Units.Count(u => u.Location == "erebor"));
        var baseline = TurnTests.Actions(actor: Side.Shadow);
        baseline.Nations.Single(n => n.Id == "sauron").StepsToWar = 0;
        var step = Assert.IsAssignableFrom<RecruitmentBatchStep>(program.Steps.Single());
        Assert.DoesNotContain("erebor", step.Option(baseline, new() { Owner = Side.Shadow }).Targets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MadnessIncludesTheNewSauronStrongholdNeighborhoodOnlyInFate(bool fate)
    {
        var state = fate ? FateFixture.Setup() : MatchFactory.Create("base", new SequenceRandom(new int[92]));
        var execution = new EffectExecution { Owner = Side.Shadow };
        var candidates = new MadnessAttackStep().Candidates(state, execution);
        Assert.Equal(fate, candidates.Contains("iron-hills"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RangersDoNotTreatNeutralDaleAsNorthTerritory(bool fate)
    {
        var state = fate ? FateFixture.Setup() : MatchFactory.Create("base", new SequenceRandom(new int[92]));
        FateFixture.Clear(state, "erebor");
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "erebor");
        var execution = new EffectExecution { Owner = Side.FreePeoples, RegionId = "iron-hills" };
        var targets = new NorthRangersAttackStep().Candidates(state, execution);
        Assert.Equal(!fate, targets.Contains("erebor"));
        var recruitment = new NorthRangersRegionStep().Candidates(state, execution);
        Assert.Equal(!fate, recruitment.Contains("dale"));
    }

    [Theory]
    [InlineData("erebor", false)]
    [InlineData("iron-hills", true)]
    public void NaryaUsesTheConfiguredNationAndDoesNotActivateDwarvesInCapturedErebor(string region, bool legal)
    {
        var state = LordsNaryaTests.Ready();
        state.Configuration.Fate = true;
        var gandalf = state.Characters.Single(c => c.DefinitionId == LordsDiceCatalog.KeeperGandalf);
        gandalf.Location = region;
        state.Control[region] = Side.FreePeoples;
        Assert.Equal(legal ? "dwarves" : null, KeeperRings.NaryaNation(state));
    }
}
