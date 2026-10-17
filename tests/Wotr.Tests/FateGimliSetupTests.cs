using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class FateGimliSetupTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void OrdinaryFateVersionUsesOneStableFigureWithPrintedStatsEvenWithoutCouncil(bool lords, bool kings)
    {
        var state = FateGimliFixture.Setup(lords: lords, council: false, kings: kings);
        if (lords)
            state = KingsFixture.Do(state, CommandKind.ChooseCompanionVersions,
                new() { Ids = [CharacterRules.Grey], GuideId = CharacterRules.Grey });
        var gimli = Assert.Single(CompanionRules.Inside(state), c => c.Id == FateGimli.Figure);
        Assert.Equal(FateGimli.Standard, gimli.DefinitionId);
        Assert.Equal(2, gimli.Level);
        Assert.Equal(1, gimli.Leadership);
        Assert.Equal("dwarves", gimli.NationIcon);
        Assert.Equal(0, CharacterCatalog.Fate[gimli.DefinitionId].ExtraActionDice);
        Assert.Empty(CouncilCompanions.Homelands(gimli));
        Assert.Single(state.Fellowship.Companions, id => id == FateGimli.Figure);
        Assert.Equal(FateGimli.Standard, LordsFixture.Views().For(state, Side.FreePeoples).OwnCompanions.Single(c => c.Id == gimli.Id).DefinitionId);
        Assert.Equal(FateGimli.Standard, CharacterCatalog.Companion(StateCodec.Copy(state), gimli.Id).DefinitionId);
        if (kings)
            Assert.DoesNotContain(state.Kings!.Sovereigns, s => s.Id is SovereignCatalog.Dain or SovereignCatalog.Brand);
    }

    [Theory]
    [InlineData(null, FateGimli.Standard)]
    [InlineData(FateGimli.Standard, FateGimli.Standard)]
    [InlineData(FateGimli.Alternate, FateGimli.Alternate)]
    public void CouncilOptionsUseActualVersionIdsAndOmissionKeepsTheFateDefault(string? choice, string expected)
    {
        var setup = FateGimliFixture.Setup();
        var option = new LordsSetupCommands().GetOptions(setup, Side.FreePeoples).Single();
        var group = option.CompanionVersions.Single(g => g.FigureId == FateGimli.Figure);
        Assert.Equal(FateGimli.Standard, group.BaseId);
        Assert.Equal(FateGimli.Alternate, group.AlternateId);
        Assert.DoesNotContain(FateGimli.Figure, option.EligibleIds);
        Assert.DoesNotContain("lords.character.gimli", option.EligibleIds);
        Assert.Equal(option.EligibleIds, setup.Pending!.PrivateChoices);
        var state = FateGimliFixture.Versions(choice);
        Assert.Equal(expected, Assert.Single(state.Characters, c => c.Id == FateGimli.Figure).DefinitionId);
        Assert.Equal(7, state.Characters.Count);
    }

    [Theory]
    [InlineData("old-base")]
    [InlineData("old-lords")]
    [InlineData("both")]
    [InlineData("duplicate")]
    [InlineData("no-fate")]
    [InlineData("no-council")]
    public void ReplacedDuplicateAndDisabledVersionsAreRejectedBeforeAnyDraw(string invalid)
    {
        var state = FateGimliFixture.Setup(fate: invalid != "no-fate", council: invalid != "no-council");
        var ids = new List<string> { CharacterRules.Grey, FateGimli.Alternate };
        switch (invalid)
        {
            case "old-base": ids[1] = FateGimli.Figure; break;
            case "old-lords": ids[1] = "lords.character.gimli"; break;
            case "both": ids.Add(FateGimli.Standard); break;
            case "duplicate": ids.Add(FateGimli.Alternate); break;
        }
        LordsFixture.Reject(state, CommandKind.ChooseCompanionVersions, new() { Ids = ids, GuideId = CharacterRules.Grey });
    }

    [Theory]
    [InlineData("iron-hills")]
    [InlineData("ered-luin")]
    public void EitherNewHomelandRevealsTheAlternateAndOpensOneShadowTokenChoice(string region)
    {
        var state = FateGimliFixture.Versions(FateGimli.Alternate);
        Assert.False(state.Configuration.NewCities);
        state.Nations.Single(n => n.Id == "dwarves").Active = false;
        var option = new CouncilCommands().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(FateGimli.Homelands, option.CharacterActions.Single(c => c.CharacterId == FateGimli.Figure).Targets);
        var p = new CommandPayload { Placements = [new() { UnitId = FateGimli.Figure, RegionId = region }] };
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine(EffectCatalog.All)).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.ChooseCouncilHomelands, p));
        Assert.True(preview.Legal);
        Assert.Single(preview.Placements);
        Assert.Equal(before, StateCodec.Write(state));
        state = KingsFixture.Do(StateCodec.Read(before), CommandKind.ChooseCouncilHomelands, p);
        Assert.Equal(region, state.Characters.Single(c => c.Id == FateGimli.Figure).Location);
        Assert.False(state.Nations.Single(n => n.Id == "dwarves").Active);
        Assert.Equal(FateGimli.Alternate, state.Lords!.RevealedCompanionVersions[FateGimli.Figure]);
        Assert.Equal(DecisionKind.CouncilToken, state.Pending!.Kind);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
    }

    [Fact]
    public void EreborAndOrdinaryVersionCannotStartOutsideAndHiddenVersionsHaveIdenticalShadowViews()
    {
        var ordinary = FateGimliFixture.Versions();
        var alternate = FateGimliFixture.Versions(FateGimli.Alternate);
        Assert.Equal(JsonSerializer.Serialize(LordsFixture.Views().For(ordinary, Side.Shadow)),
            JsonSerializer.Serialize(LordsFixture.Views().For(alternate, Side.Shadow)));
        foreach (var (state, region) in new[] { (ordinary, "iron-hills"), (alternate, "erebor") })
            LordsFixture.Reject(state, CommandKind.ChooseCouncilHomelands,
                new() { Placements = [new() { UnitId = FateGimli.Figure, RegionId = region }] });
        alternate = KingsFixture.Do(alternate, CommandKind.ChooseCouncilHomelands);
        Assert.Equal(DecisionKind.FellowshipPhase, alternate.Pending!.Kind);
        Assert.Empty(alternate.Lords!.Council!.GrantedTokens);
        Assert.DoesNotContain(FateGimli.Figure, alternate.Lords.RevealedCompanionVersions.Keys);
    }

    [Fact]
    public void BaseAndLordsVersionsAndEreborHomelandRemainAvailableWithoutFate()
    {
        var state = FateGimliFixture.Setup(fate: false);
        var group = new LordsSetupCommands().GetOptions(state, Side.FreePeoples).Single().CompanionVersions.Single(g => g.FigureId == FateGimli.Figure);
        Assert.Equal(FateGimli.Figure, group.BaseId);
        Assert.Equal("lords.character.gimli", group.AlternateId);
        state = KingsFixture.Do(state, CommandKind.ChooseCompanionVersions,
            new() { Ids = [CharacterRules.Grey, group.AlternateId], GuideId = CharacterRules.Grey });
        Assert.Equal("erebor", Assert.Single(CouncilCompanions.Homelands(state.Characters.Single(c => c.Id == FateGimli.Figure))));
    }
}
