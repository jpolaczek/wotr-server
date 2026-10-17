using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsCharacterFixture;

namespace Wotr.Tests;

public class LordsBalrogActionTests
{
    internal static DieState BalrogDie(MatchState state)
    {
        var die = LordsFixture.Die(state, LordsDiceCatalog.Balrog);
        die.Location = DieLocation.Available;
        die.Face = DieFace.UseBalrog;
        die.Special!.Chosen = true;
        return die;
    }

    private static CommandPayload Ability(MatchState state, CharacterState character, string ability, string die) => new()
    {
        CharacterId = character.Id, AbilityId = ability, DieId = die, Ids = [character.Location]
    };

    [Theory]
    [InlineData(null)]
    [InlineData(UnitKind.Regular)]
    [InlineData(UnitKind.Elite)]
    public void EntryOptionallyRecruitsOneRealSauronUnitAndAdvancesTwoNationsWithOneCost(UnitKind? kind)
    {
        var state = State(Side.Shadow);
        var payload = Entry(state, LordsCharacters.Balrog, "moria");
        if (kind.HasValue)
            payload.Placements = [new() { UnitId = state.Units.First(u => u.Nation == "sauron" && u.Kind == kind && u.Location == "reserve").Id, RegionId = "moria" }];
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, BattleFixture.Command(state, CommandKind.EnterCharacter, payload));
        Assert.True(preview.Legal);
        Assert.Equal(kind.HasValue ? 2 : 1, preview.Placements.Count);
        Assert.Contains(preview.PoliticsChanges, n => n.Id == "elves" && n.StepsToWar == 2);
        Assert.Contains(preview.PoliticsChanges, n => n.Id == "dwarves" && n.StepsToWar == 2);
        Assert.Equal(before, StateCodec.Write(state));
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        Assert.Equal(0, CharacterRules.InPlay(state, LordsCharacters.Balrog)!.Level);
        Assert.False(state.MinionEverEntered);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        if (kind.HasValue)
            Assert.Equal("moria", state.Units.Single(u => u.Id == payload.Placements[0].UnitId).Location);
        Assert.Equal(DieLocation.Removed, LordsFixture.Die(state, LordsDiceCatalog.Balrog).Location);
        DiceRules.Recover(state);
        Assert.Equal(DieLocation.Unrolled, LordsFixture.Die(state, LordsDiceCatalog.Balrog).Location);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnemyControlOrOwnSiegeBlocksBonusRecruitButNotBalrogsEntry(bool siege)
    {
        var state = State(Side.Shadow, emptyBoard: true);
        if (siege)
            BattleFixture.Put(state, "elves", UnitKind.Regular, "moria");
        else
            state.Control["moria"] = Side.FreePeoples;
        var payload = Entry(state, LordsCharacters.Balrog, "moria");
        payload.Placements = [new() { UnitId = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular).Id, RegionId = "moria" }];
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
        payload.Placements.Clear();
        state = LordsFixture.Do(state, CommandKind.EnterCharacter, payload);
        Assert.NotNull(CharacterRules.InPlay(state, LordsCharacters.Balrog));
    }

    [Fact]
    public void ActivationSpendsChosenBalrogResultWithoutMovingAndInactiveBalrogCannotTravel()
    {
        var state = State(Side.Shadow, emptyBoard: true);
        var balrog = Put(state, LordsCharacters.Balrog, "moria");
        var die = BalrogDie(state);
        var travel = new CommandPayload
        {
            DieId = die.Id, AbilityId = LordsArmyCosts.UseBalrog,
            CompanionMoves = [new() { Ids = [balrog.Id], Route = ["moria", "dimrill-dale"] }]
        };
        LordsFixture.Reject(state, CommandKind.MoveShadowCharacters, travel);
        var activation = Ability(state, balrog, LordsCharacterAbilities.ActivateBalrog, die.Id);
        var original = StateCodec.Write(state);
        var command = BattleFixture.Command(state, CommandKind.UseCharacterAbility, activation);
        var direct = LordsFixture.Engine().Execute(state, Side.Shadow, command, new SequenceRandom());
        var restored = LordsFixture.Engine().Execute(StateCodec.Read(original), Side.Shadow, command, new SequenceRandom());
        Assert.True(direct.Receipt.Accepted);
        Assert.Equal(StateCodec.Write(direct.State), StateCodec.Write(restored.State));
        state = direct.State;
        balrog = CharacterRules.InPlay(state, LordsCharacters.Balrog)!;
        Assert.Equal(2, balrog.Level);
        Assert.Equal("moria", balrog.Location);
        Assert.Equal(DieLocation.Spent, LordsFixture.Die(state, die.Id).Location);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
    }

    [Theory]
    [InlineData("nation")]
    [InlineData("kind")]
    [InlineData("region")]
    [InlineData("reserve")]
    [InlineData("count")]
    public void EntryRejectsForgedBonusRecruitWithoutSpendingItsDie(string invalid)
    {
        var state = State(Side.Shadow);
        var payload = Entry(state, LordsCharacters.Balrog, "moria");
        var unit = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        if (invalid == "nation")
            unit = state.Units.First(u => u.Nation == "isengard" && u.Kind == UnitKind.Regular && u.Location == "reserve");
        if (invalid == "kind")
            unit = state.Units.First(u => u.Kind == UnitKind.Nazgul && u.Location == "reserve");
        if (invalid == "reserve")
            unit.Location = "minas-morgul";
        payload.Placements = [new() { UnitId = unit.Id, RegionId = invalid == "region" ? "lorien" : "moria" }];
        if (invalid == "count")
            payload.Placements.Add(new() { UnitId = unit.Id, RegionId = "moria" });
        LordsFixture.Reject(state, CommandKind.EnterCharacter, payload);
    }

    [Theory]
    [InlineData("travel")]
    [InlineData("army")]
    [InlineData("attack")]
    public void ActiveBalrogResultAllowsExactlyHisOwnTravelOrOneArmyMoveOrAttack(string action)
    {
        var state = State(Side.Shadow, emptyBoard: true);
        War(state, "sauron", "elves");
        var balrog = Put(state, LordsCharacters.Balrog, "moria");
        balrog.Level = 2;
        var gothmog = Put(state, LordsCharacters.Gothmog, "moria");
        var die = BalrogDie(state);
        var payload = new CommandPayload { DieId = die.Id, AbilityId = LordsArmyCosts.UseBalrog, MovementCost = DieFace.Character };
        CommandKind command;
        if (action == "travel")
        {
            command = CommandKind.MoveShadowCharacters;
            payload.CompanionMoves = [new() { Ids = [balrog.Id, gothmog.Id], Route = ["moria", "dimrill-dale"] }];
            LordsFixture.Reject(state, command, payload);
            payload.CompanionMoves[0].Ids = [balrog.Id];
        }
        else
        {
            command = action == "army" ? CommandKind.MoveArmies : CommandKind.AttackArmy;
            var unit = BattleFixture.Put(state, "sauron", UnitKind.Regular, "moria");
            payload.Moves = [new() { From = "moria", To = "dimrill-dale", UnitIds = [unit.Id], CharacterIds = [gothmog.Id] }];
            if (action == "attack")
                BattleFixture.Put(state, "elves", UnitKind.Regular, "dimrill-dale");
            LordsFixture.Reject(state, command, payload);
            payload.Moves[0].CharacterIds.Add(balrog.Id);
        }
        state = LordsFixture.Do(state, command, payload);
        Assert.Equal(action == "attack" ? DieLocation.Resolving : DieLocation.Spent, LordsFixture.Die(state, die.Id).Location);
        Assert.Equal(action == "attack" ? "moria" : "dimrill-dale", CharacterRules.InPlay(state, LordsCharacters.Balrog)!.Location);
    }

    [Theory]
    [InlineData(CharacterRules.Grey)]
    [InlineData(LordsCharacters.Gandalf)]
    [InlineData(CharacterRules.White)]
    public void AnyGandalfVersionAllowsCharacterDeactivationInMoriaAndWillEliminatesOutside(string gandalf)
    {
        var state = State(emptyBoard: true);
        var balrog = Put(state, LordsCharacters.Balrog, "moria");
        balrog.Level = 2;
        var die = CharacterFixture.Cost(state, DieFace.Character).DieId;
        var payload = Ability(state, balrog, LordsCharacterAbilities.DeactivateBalrog, die);
        LordsFixture.Reject(state, CommandKind.UseCharacterAbility, payload);
        Put(state, gandalf, "moria");
        state = LordsFixture.Do(state, CommandKind.UseCharacterAbility, payload);
        balrog = CharacterRules.InPlay(state, LordsCharacters.Balrog)!;
        Assert.Equal(0, balrog.Level);
        balrog.Level = 2;
        balrog.Location = "dimrill-dale";
        TurnFlow.SelectNext(state, Side.FreePeoples);
        die = CharacterFixture.Cost(state, DieFace.WillOfTheWest).DieId;
        payload = Ability(state, balrog, LordsCharacterAbilities.DeactivateBalrog, die);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.FreePeoples, BattleFixture.Command(state, CommandKind.UseCharacterAbility, payload));
        Assert.Contains(balrog.Id, preview.RemovedIds);
        state = LordsFixture.Do(state, CommandKind.UseCharacterAbility, payload);
        Assert.Equal("casualties", state.Characters.Single(c => c.Id == balrog.Id).Location);
        Assert.DoesNotContain(CharacterEntry.Candidates(state, Side.Shadow), e => e.DefinitionId == LordsCharacters.Balrog);
    }
}
