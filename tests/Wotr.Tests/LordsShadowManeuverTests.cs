using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsShadowCardsFixture;

namespace Wotr.Tests;

public class LordsShadowManeuverTests
{
    internal static MatchState DolGuldurAttackChoice()
    {
        var state = State();
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "dol-guldur");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "dol-guldur");
        BattleFixture.Put(state, "elves", UnitKind.Regular, "western-brown-lands");
        return Play(state, LordsShadowCards.DolGuldur);
    }

    [Fact]
    public void DolGuldurCanAttackOnlyWithItsOwnArmyAndNoAdditionalCost()
    {
        var state = DolGuldurAttackChoice();
        var regular = state.Units.First(u => u.Location == "dol-guldur" && u.Kind == UnitKind.Regular);
        var payload = new CommandPayload
        {
            Moves = [new() { From = "dol-guldur", To = "western-brown-lands", UnitIds = [regular.Id] }],
            DieId = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id
        };
        LordsFixture.Reject(state, CommandKind.AttackArmy, payload);
        payload.DieId = "";
        var other = BattleFixture.Put(state, "sauron", UnitKind.Regular, "southern-mirkwood");
        payload.Moves[0].From = other.Location;
        payload.Moves[0].UnitIds = [other.Id];
        LordsFixture.Reject(state, CommandKind.AttackArmy, payload);
        payload.Moves[0].From = "dol-guldur";
        payload.Moves[0].UnitIds = [regular.Id];
        state = LordsFixture.Do(state, CommandKind.AttackArmy, payload);
        Assert.Equal(EffectReturn.Parent, BattleRuntime.Active(state)!.Return);
        Assert.Single(state.Resolution.PaidDieIds);
        Assert.Single(BattleRuntime.Active(state)!.Attacker.Participants);
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(1, false, 1)]
    [InlineData(3, true, 1)]
    public void DolGuldurRespectsPartialReserveAndGarrisonLimit(int reserve, bool siege, int recruits)
    {
        var state = State();
        for (var i = 0; i < 4; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "dol-guldur").Garrison = siege;
        for (var i = 0; i < 3; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "dol-guldur").Garrison = siege;
        if (siege)
            BattleFixture.Put(state, "elves", UnitKind.Regular, "dol-guldur");
        foreach (var unit in state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve").Skip(reserve))
            unit.Location = "casualties";
        state = Play(state, LordsShadowCards.DolGuldur);
        var army = state.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "dol-guldur").ToArray();
        Assert.Equal(4 + recruits, army.Length);
        Assert.All(army, u => Assert.Equal(siege, u.Garrison));
    }

    [Fact]
    public void InactiveBalrogActivatesWithoutMovingAndAbsentBalrogRejectsEvent()
    {
        var state = State();
        var cost = Cost(state, LordsShadowCards.Balrog, DieFace.Character);
        LordsFixture.Reject(state, CommandKind.PlayEvent, cost);
        LordsCharacterFixture.Put(state, LordsCharacters.Balrog, "moria");
        state = LordsFixture.Do(state, CommandKind.PlayEvent, cost);
        Assert.Equal(2, CharacterRules.InPlay(state, LordsCharacters.Balrog)!.Level);
        Assert.Equal("moria", CharacterRules.InPlay(state, LordsCharacters.Balrog)!.Location);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == cost.DieId).Location);
    }

    internal static MatchState BalrogChoice()
    {
        var state = State();
        LordsCharacterFixture.Put(state, LordsCharacters.Balrog, "moria").Level = 2;
        BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
        return Play(state, LordsShadowCards.Balrog);
    }

    [Fact]
    public void ActiveBalrogCanTravelAloneWithNoExtraDieAndRejectsOtherFigures()
    {
        var state = LordsCardsFixture.Choose(BalrogChoice(), BalrogEventStep.Alone);
        var payload = new CommandPayload { CompanionMoves = [new() { Ids = [LordsCharacters.Balrog], Route = ["moria", "dimrill-dale"] }] };
        payload.DieId = state.Dice.First(d => d.Location == DieLocation.Available).Id;
        LordsFixture.Reject(state, CommandKind.MoveShadowCharacters, payload);
        payload.DieId = "";
        payload.CompanionMoves[0].Ids.Add(CharacterRules.WitchKing);
        LordsFixture.Reject(state, CommandKind.MoveShadowCharacters, payload);
        payload.CompanionMoves[0].Ids.Remove(CharacterRules.WitchKing);
        state = LordsFixture.Do(state, CommandKind.MoveShadowCharacters, payload);
        Assert.Equal("dimrill-dale", CharacterRules.InPlay(state, LordsCharacters.Balrog)!.Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Contains(state.Units, u => u.Location == "moria" && BoardRules.IsArmyUnit(u));
    }

    [Fact]
    public void BalrogArmyMustIncludeBalrogAndDeclineEndsTheSameAction()
    {
        var state = LordsCardsFixture.Choose(BalrogChoice(), BalrogEventStep.Army);
        var move = new ArmyMoveChoice { From = "moria", To = "dimrill-dale", UnitIds = state.Units.Where(u => u.Location == "moria").Select(u => u.Id).ToList() };
        LordsFixture.Reject(state, CommandKind.MoveArmies, new() { Moves = [move] });
        move.CharacterIds = [LordsCharacters.Balrog];
        state = LordsFixture.Do(state, CommandKind.MoveArmies, new() { Moves = [move] });
        Assert.Equal("dimrill-dale", CharacterRules.InPlay(state, LordsCharacters.Balrog)!.Location);
        Assert.Equal(DecisionKind.Action, state.Pending!.Kind);
        state = LordsCardsFixture.Choose(BalrogChoice(), BalrogEventStep.Decline);
        Assert.Equal("moria", CharacterRules.InPlay(state, LordsCharacters.Balrog)!.Location);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(0, 3, 3)]
    [InlineData(9, 3, 1)]
    [InlineData(10, 3, 0)]
    [InlineData(0, 6, 5)]
    public void DolGuldurRecruitsLeadershipUpToFiveAndChecksStackBeforeOptionalMove(int army, int nazgul, int recruits)
    {
        var state = State();
        for (var i = 0; i < army; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Regular, "dol-guldur");
        for (var i = 0; i < nazgul; i++)
            BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "dol-guldur");
        state = Play(state, LordsShadowCards.DolGuldur);
        Assert.Equal(army + recruits, state.Units.Count(u => u.Location == "dol-guldur" && BoardRules.IsArmyUnit(u)));
        Assert.Equal(DecisionKind.ArmyEvent, state.Pending!.Kind);
        Assert.All(LordsFixture.Views().For(state, Side.Shadow).Options.SelectMany(o => o.Armies), a => Assert.Equal("dol-guldur", a.RegionId));
        state = LordsFixture.Do(state, CommandKind.FinishArmyEvent);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Theory]
    [InlineData(CharacterRules.WitchKing, 2)]
    [InlineData(LordsCharacters.WitchKing, 1)]
    public void WitchKingCountsLeadershipAndAbsentNazgulIsNotPlayable(string version, int leadership)
    {
        var state = State();
        var cost = Cost(state, LordsShadowCards.DolGuldur, DieFace.Army);
        LordsFixture.Reject(state, CommandKind.PlayEvent, cost);
        LordsCharacterFixture.Put(state, version, "dol-guldur");
        state = LordsFixture.Do(state, CommandKind.PlayEvent, cost);
        Assert.Equal(leadership, state.Units.Count(u => u.Location == "dol-guldur" && u.Kind == UnitKind.Regular));
    }
}
