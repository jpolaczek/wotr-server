using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsFixture;
using static Wotr.Tests.LordsSmeagolTests;

namespace Wotr.Tests;

public class LordsSmeagolDepartureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeparatingSmeagolWithTheWholeGroupEntersTheEventRegardlessOfPayloadOrder(bool reverse)
    {
        var state = State(true);
        var ids = state.Fellowship.Companions.ToList();
        if (reverse)
            ids.Reverse();
        var payload = new CommandPayload { DieId = state.Dice[0].Id, Ids = ids, Route = ["hollin"] };
        var before = StateCodec.Write(state);
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.SeparateCompanions, payload));
        Assert.True(preview.Legal);
        Assert.Contains(LordsCharacters.Smeagol, preview.RemovedIds);
        Assert.DoesNotContain(preview.Placements, p => p.UnitId == LordsCharacters.Smeagol);
        Assert.Equal(before, StateCodec.Write(state));
        state = Do(state, CommandKind.SeparateCompanions, payload);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Equal("removed", state.Characters.Single(c => c.Id == LordsCharacters.Smeagol).Location);
        Assert.Equal(7, state.Characters.Count(c => c.Location == "hollin"));
    }

    [Fact]
    public void LeavingOnlySmeagolReplacesHimWithGollumWithoutTheEvent()
    {
        var state = State(true);
        var payload = new CommandPayload
        {
            DieId = state.Dice[0].Id,
            Ids = state.Fellowship.Companions.Where(id => id != LordsCharacters.Smeagol).ToList(),
            Route = ["hollin"]
        };
        var preview = new PreviewService(Engine()).Preview(state, Side.FreePeoples,
            BattleFixture.Command(state, CommandKind.SeparateCompanions, payload));
        Assert.Contains(LordsCharacters.Smeagol, preview.RemovedIds);
        state = Do(state, CommandKind.SeparateCompanions, payload);
        Assert.Empty(state.Fellowship.Companions);
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.True(state.Lords!.SmeagolLeft);
        Assert.True(state.Lords.GollumEntered);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
    }

    [Theory]
    [InlineData("lorien", Side.FreePeoples, true)]
    [InlineData("edoras", Side.FreePeoples, true)]
    [InlineData("lorien", Side.Shadow, false)]
    [InlineData("edoras", Side.Shadow, false)]
    [InlineData("hollin", Side.FreePeoples, false)]
    public void OnlyDeclarationInUnconqueredCityOrStrongholdDiscardsSmeagol(string region, Side control, bool removed)
    {
        var state = State(true);
        state.Phase = GamePhase.Fellowship;
        state.Fellowship.RegionId = region;
        state.Control[region] = control;
        TurnFlow.OpenFellowshipPhase(state);
        state = Do(state, CommandKind.DeclareFellowship, new() { Route = [region] });
        Assert.Equal(!removed, SmeagolRules.Present(state));
        Assert.Equal(removed, state.Player(Side.Shadow).Table.Contains(SmeagolRules.WeShallGetIt));
        if (removed)
        {
            Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
            state = Do(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Strider });
        }
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
    }

    [Fact]
    public void RevealToConqueredCityDoesNotDiscardSmeagol()
    {
        var state = State(true);
        state.Fellowship.RegionId = "edoras";
        state.Control["edoras"] = Side.Shadow;
        FellowshipFlow.BeginReveal(state, "source");
        state = Do(state, CommandKind.RevealFellowship, new() { Route = ["edoras"] });
        Assert.True(SmeagolRules.Present(state));
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
    }

    [Fact]
    public void ChallengeDrawAlsoIntroducesSmeagolAndDoesNotReturnHisTile()
    {
        var state = State();
        CompanionTests.Outside(state, CharacterRules.Strider, "minas-tirith");
        HuntTests.Pool(state, FirstTile, HuntTests.Three, HuntTests.Eye);
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards03.Challenge);
        state = Do(state, CommandKind.PlayEvent,
            EventTests.Play(state, FreePeoplesCharacterCards03.Challenge, DieFace.Character).Payload, new SequenceRandom(2, 0, 0));
        Assert.True(SmeagolRules.Present(state));
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == FirstTile).Location);
        Assert.Equal(0, state.Fellowship.Corruption);
    }
}
