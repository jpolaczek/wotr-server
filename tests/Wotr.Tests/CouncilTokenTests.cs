using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class CouncilTokenTests
{
    internal static MatchState Actions()
    {
        var state = LordsCharacterFixture.State(Side.Shadow);
        state.Configuration.Council = true;
        state.Lords!.Council = new() { HomelandsChosen = true, GrantedTokens = [.. CouncilTokens.All] };
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    [Fact]
    public void PoliticsTokenSpendsNoDieAndOnlyOneTokenMayBeUsedPerTurn()
    {
        var state = Actions();
        var nation = state.Nations.Single(n => n.Id == "sauron");
        nation.StepsToWar = 2;
        var count = TurnFlow.Available(state, Side.Shadow);
        var p = new CommandPayload { AbilityId = CouncilTokens.Politics, NationId = "sauron" };
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow,
            BattleFixture.Command(state, CommandKind.UseCouncilToken, p));
        Assert.True(preview.Legal);
        Assert.Equal(CouncilTokens.Politics, Assert.Single(preview.Costs));
        Assert.Equal(1, Assert.Single(preview.PoliticsChanges).StepsToWar);
        state = LordsFixture.Do(state, CommandKind.UseCouncilToken, p);
        Assert.Equal(count, TurnFlow.Available(state, Side.Shadow));
        Assert.Empty(state.Resolution.PaidDieIds);
        Assert.Equal(1, state.Nations.Single(n => n.Id == "sauron").StepsToWar);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        TurnFlow.SelectNext(state, Side.Shadow);
        LordsFixture.Reject(state, CommandKind.UseCouncilToken, Travel(state));
        state.Turn++;
        TurnFlow.SelectNext(state, Side.Shadow);
        LordsFixture.Reject(state, CommandKind.UseCouncilToken, p);
        state = LordsFixture.Do(state, CommandKind.UseCouncilToken, Travel(state));
        Assert.Equal(2, state.Lords!.Council!.SpentTokens.Count);
        Assert.All(LordsFixture.Views().For(state, Side.FreePeoples).CouncilTokens, t => Assert.True(t.Spent));
    }

    internal static CommandPayload Travel(MatchState state)
    {
        var nazgul = state.Units.First(u => u.Kind == UnitKind.Nazgul && BoardRules.Regions.ContainsKey(u.Location));
        return new() { AbilityId = CouncilTokens.Travel,
            CompanionMoves = [new() { Ids = [nazgul.Id], Route = [nazgul.Location, "orthanc"] }] };
    }

    [Fact]
    public void TravelTokenUsesNormalMinionRestrictionsAndCannotMoveArmies()
    {
        var state = Actions();
        var p = Travel(state);
        p.CompanionMoves[0].Route[1] = "rivendell";
        LordsFixture.Reject(state, CommandKind.UseCouncilToken, p);
        var mouth = LordsCharacterFixture.Put(state, CharacterRules.Mouth, "minas-morgul");
        p.CompanionMoves = [new() { Ids = [mouth.Id], Route = [mouth.Location, "orthanc"] }];
        LordsFixture.Reject(state, CommandKind.UseCouncilToken, p);
        var army = state.Units.First(u => BoardRules.IsArmyUnit(u) && BoardRules.Regions.ContainsKey(u.Location));
        p.CompanionMoves = [new() { Ids = [army.Id], Route = [army.Location, "orthanc"] }];
        LordsFixture.Reject(state, CommandKind.UseCouncilToken, p);
        p = Travel(state);
        state = LordsFixture.Do(state, CommandKind.UseCouncilToken, p);
        Assert.Equal("orthanc", state.Units.Single(u => u.Id == p.CompanionMoves[0].Ids[0]).Location);
    }

    [Theory]
    [InlineData("die")]
    [InlineData("ring")]
    [InlineData("face")]
    [InlineData("both-effects")]
    [InlineData("foreign-nation")]
    [InlineData("ring-pending")]
    [InlineData("missing-token")]
    public void InvalidTokenCostsOrTargetsDoNotSpendAnything(string invalid)
    {
        var state = Actions();
        var p = new CommandPayload { AbilityId = CouncilTokens.Politics, NationId = "sauron" };
        switch (invalid)
        {
            case "die": p.DieId = state.Dice.First(d => d.Owner == Side.Shadow).Id; break;
            case "ring": p.RingId = state.Rings[0].Id; break;
            case "face": p.Face = DieFace.Character; break;
            case "both-effects": p.CompanionMoves = Travel(state).CompanionMoves; break;
            case "foreign-nation": p.NationId = "gondor"; break;
            case "ring-pending": state.Player(Side.Shadow).RingActionPending = true; break;
            case "missing-token": state.Lords!.Council!.GrantedTokens.Clear(); break;
        }
        LordsFixture.Reject(state, CommandKind.UseCouncilToken, p);
    }

    [Fact]
    public void TokenNeverPaysAnyDieAbilityOrRingConversion()
    {
        var state = Actions();
        var ring = state.Rings[0];
        ring.Owner = Side.Shadow;
        LordsFixture.Reject(state, CommandKind.UseRing, new() { RingId = ring.Id, DieId = CouncilTokens.Politics, Face = DieFace.Muster });
        LordsFixture.Reject(state, CommandKind.DiscardDie, new() { DieId = CouncilTokens.Politics });
        LordsFixture.Reject(state, CommandKind.AdvancePolitics, new() { DieId = CouncilTokens.Politics, NationId = "sauron" });
    }

    [Fact]
    public void PassUsesOnlyDiceAndZeroDiceMayUseTokenWhileOpponentHasDice()
    {
        var state = Actions();
        var fp = TurnFlow.Available(state, Side.FreePeoples);
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Skip(fp))
            die.Location = DieLocation.Spent;
        Assert.False(TurnCommands.CanPass(state, Side.Shadow));
        state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Location = DieLocation.Spent;
        Assert.True(TurnCommands.CanPass(state, Side.Shadow));
        foreach (var die in state.Dice.Where(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available))
            die.Location = DieLocation.Spent;
        TurnFlow.SelectNext(state, Side.Shadow);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        state = LordsFixture.Do(state, CommandKind.UseCouncilToken, Travel(state));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Equal(0, TurnFlow.Available(state, Side.Shadow));
    }

    [Fact]
    public void TokensDoNotKeepActionPhaseAliveWhenBothDicePoolsAreEmpty()
    {
        var state = Actions();
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Available))
            die.Location = DieLocation.Spent;
        TurnFlow.SelectNext(state, Side.Shadow);
        Assert.Equal(3, state.Turn);
        Assert.Equal(GamePhase.Fellowship, state.Phase);
        Assert.Equal(2, state.Lords!.Council!.GrantedTokens.Count);
        Assert.Empty(state.Lords.Council.SpentTokens);
    }
}
