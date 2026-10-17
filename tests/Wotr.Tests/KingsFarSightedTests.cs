using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.KingsFixture;

namespace Wotr.Tests;

public class KingsFarSightedTests
{
    internal static MatchState Ready()
    {
        var state = Actions();
        SovereignLifecycle.Awaken(state, Sovereign(state, SovereignCatalog.Thranduil), "woodland-realm");
        Ruler(state, DieFace.Character);
        return state;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 3)]
    [InlineData(6, 4)]
    public void FellowshipModeReducesOnlyHuntDiceNotBonusOrRerollSources(int eyes, int rolled)
    {
        var state = Ready();
        state.Fellowship.RegionId = "dol-guldur";
        Eyes(state, eyes);
        var previousMove = Die(state);
        previousMove.Location = DieLocation.Hunt;
        previousMove.HuntOrigin = DiceRules.FellowshipMove;
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource).Id;
        var option = new HuntOptions().GetOptions(state, Side.FreePeoples).Single();
        Assert.Equal(1, option.RulerTravel!.HuntDiceReduction);
        state = Do(state, CommandKind.MoveFellowship, new() { DieId = cost, AbilityId = KingsFarSighted.Ability }, new int[rolled]);
        Assert.Equal(KingsFixture.SetupSamples + rolled, state.RandomSamples.Count);
        if (rolled == 0)
        {
            Assert.Null(HuntRuntime.Active(state));
            Assert.Equal(DieLocation.Hunt, state.Dice.Single(d => d.Id == cost).Location);
            return;
        }
        var hunt = HuntRuntime.Active(state)!;
        Assert.Equal(rolled, hunt.Dice.Count);
        Assert.Equal(1, hunt.Bonus);
        Assert.Contains("stronghold", hunt.RerollSources);
        Assert.Contains("army", hunt.RerollSources);
        Assert.Equal(DecisionKind.HuntReroll, state.Pending!.Kind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CompanionModeAddsOneRegionToSeparationOrMovementWithoutChangingPrintedLevel(bool separate)
    {
        var state = Ready();
        const string merry = "base.character.merry";
        var character = CharacterCatalog.Companion(state, merry);
        if (!separate)
        {
            state.Fellowship.Companions.Remove(merry);
            character.Location = "rivendell";
            state.Characters.Add(character);
        }
        var kind = separate ? CommandKind.SeparateCompanions : CommandKind.MoveCompanions;
        var cost = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource).Id;
        var payload = new CommandPayload { DieId = cost, AbilityId = KingsFarSighted.Ability };
        string[] route = ["rivendell", "trollshaws", "hollin"];
        if (separate)
        {
            payload.Ids = [merry];
            payload.Route = [.. route];
        }
        else
            payload.CompanionMoves = [new() { Ids = [merry], Route = [.. route] }];
        var option = new FellowshipOptions().GetOptions(state, Side.FreePeoples).Single(o => o.Kind == kind);
        Assert.Equal(1, option.RulerTravel!.RangeBonus);
        Assert.Equal(2, option.RulerTravel.Travel.Single(t => t.Id == merry).Range);
        payload.AbilityId = "";
        LordsFixture.Reject(state, kind, payload);
        payload.AbilityId = KingsFarSighted.Ability;
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples, BattleFixture.Command(state, kind, payload));
        Assert.True(preview.Legal);
        Assert.Contains(preview.Placements, p => p.UnitId == merry && p.RegionId == "hollin");
        state = Do(state, kind, payload);
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == merry).Location);
        Assert.Equal(1, state.Characters.Single(c => c.Id == merry).Level);
        Assert.Null(HuntRuntime.Active(state));
    }

    [Theory]
    [InlineData("standard")]
    [InlineData("mordor")]
    [InlineData("wrong-face")]
    [InlineData("removed")]
    public void FarSightedRejectsAnOrdinaryDieMordorWrongFaceOrAbsentThranduil(string reason)
    {
        var state = Ready();
        var die = state.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == SovereignCatalog.RulerSource);
        if (reason == "standard")
            die.Source = "standard";
        if (reason == "mordor")
            state.Fellowship.MordorStep = 1;
        if (reason == "wrong-face")
            die.Face = DieFace.Event;
        if (reason == "removed")
            SovereignLifecycle.Remove(state, Sovereign(state, SovereignCatalog.Thranduil));
        LordsFixture.Reject(state, CommandKind.MoveFellowship, new() { DieId = die.Id, AbilityId = KingsFarSighted.Ability });
        Assert.Null(KingsFarSighted.Option(state, CommandKind.MoveFellowship));
    }
}
