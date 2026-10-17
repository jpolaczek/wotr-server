using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Tests.LordsCharacterFixture;

namespace Wotr.Tests;

public class LordsMouthAbilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GreatSorceryPaysForOnlyMouthsArmyOncePerTurnAndCannotPayForAnEvent(bool attack)
    {
        var state = State(Side.Shadow, emptyBoard: true);
        War(state, "sauron", "gondor");
        var mouth = Put(state, LordsCharacters.Mouth, "osgiliath", CharacterRules.Mouth);
        var unit = BattleFixture.Put(state, "sauron", UnitKind.Regular, "osgiliath");
        if (attack)
            BattleFixture.Put(state, "gondor", UnitKind.Regular, "south-ithilien");
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        payload.AbilityId = LordsCharacters.GreatSorcery;
        payload.MovementCost = DieFace.Character;
        payload.Moves = [new() { From = "osgiliath", To = "south-ithilien", UnitIds = [unit.Id] }];
        var kind = attack ? CommandKind.AttackArmy : CommandKind.MoveArmies;
        LordsFixture.Reject(state, kind, payload);
        payload.Moves[0].CharacterIds = [mouth.Id];
        Assert.False(CharacterCosts.CanPay(state, Side.Shadow, payload.DieId, DieFace.Character, LordsCharacters.GreatSorcery));
        Assert.Equal(mouth.Id, CharacterCosts.ArmyOptions(state, Side.Shadow, 2).Single(o => o.AbilityId == LordsCharacters.GreatSorcery).RequiredCharacterId);
        payload.MovementCost = DieFace.Army;
        LordsFixture.Reject(state, kind, payload);
        payload.MovementCost = DieFace.Character;
        state = LordsFixture.Do(state, kind, payload);
        Assert.Equal(state.Turn, state.Characters.Single(c => c.Id == mouth.Id).LastArmyConversionTurn);
        Assert.Equal(attack ? DieLocation.Resolving : DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
        state = StateCodec.Read(StateCodec.Write(state));
        var another = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
        another.Face = DieFace.Muster;
        Assert.False(LordsArmyCosts.CanPay(state, Side.Shadow, another.Id, LordsCharacters.GreatSorcery));
        state.Turn++;
        Assert.True(LordsArmyCosts.CanPay(state, Side.Shadow, another.Id, LordsCharacters.GreatSorcery));
    }

    internal static MatchState Siege(bool elite = false)
    {
        var state = State(Side.Shadow, emptyBoard: true);
        War(state, "sauron", "gondor");
        foreach (var die in state.Dice.Where(d => d.Source == "standard"))
            die.Face = DieFace.Army;
        var mouth = Put(state, LordsCharacters.Mouth, "minas-tirith", CharacterRules.Mouth);
        var unit = BattleFixture.Put(state, "sauron", elite ? UnitKind.Elite : UnitKind.Regular, "minas-tirith");
        BattleFixture.Put(state, "gondor", UnitKind.Regular, "minas-tirith").Garrison = true;
        state = LordsFixture.Do(state, CommandKind.AttackArmy, BattleFixture.AttackPayload(state, [unit.Id, mouth.Id], "minas-tirith", "minas-tirith"));
        return RoundWithoutHits(state);
    }

    internal static MatchState RoundWithoutHits(MatchState state)
    {
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard);
        state = LordsFixture.Do(state, CommandKind.CommitCombatCard, random: new SequenceRandom(new int[10]));
        while (state.Pending?.Kind == DecisionKind.BattleReroll)
            state = LordsFixture.Do(state, CommandKind.RerollBattle);
        return state;
    }

    [Fact]
    public void EvilKnowledgeOffersSiegeRoundWithoutElitePersistsUsageAndResetsOnlyAfterAction()
    {
        var state = Siege();
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        Assert.Contains(CommandKind.UseEvilKnowledge, state.Pending.Commands);
        var command = BattleFixture.Command(state, CommandKind.UseEvilKnowledge);
        var before = StateCodec.Write(state);
        var preview = new PreviewService(LordsFixture.Engine()).Preview(state, Side.Shadow, command);
        Assert.True(preview.Legal);
        Assert.Empty(preview.RemovedIds);
        Assert.Equal([LordsCharacters.EvilKnowledge], preview.Costs);
        Assert.Equal(before, StateCodec.Write(state));
        state = LordsFixture.Do(StateCodec.Read(before), CommandKind.UseEvilKnowledge);
        Assert.Equal(2, BattleRuntime.Active(state)!.Round);
        Assert.True(state.Lords!.EvilKnowledgeUsed);
        Assert.False(EvilKnowledge.Available(state, BattleRuntime.Active(state)!));
        var secondBattle = new BattleState
        {
            Mode = BattleMode.Siege, Attacker = BattleRuntime.Active(state)!.Attacker, Defender = BattleRuntime.Active(state)!.Defender
        };
        Assert.False(EvilKnowledge.Available(StateCodec.Read(StateCodec.Write(state)), secondBattle));
        state = RoundWithoutHits(state);
        Assert.Empty(state.Resolution.Frames);
        Assert.False(state.Lords!.EvilKnowledgeUsed);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void EvilKnowledgeAndEliteReductionAreIndependentChoicesAndRepeatIsRejected()
    {
        var state = Siege(elite: true);
        var elites = StrongholdRules.Elites(state, BattleRuntime.Active(state)!).ToList();
        Assert.Single(elites);
        state = LordsFixture.Do(state, CommandKind.UseEvilKnowledge);
        state = RoundWithoutHits(state);
        Assert.Equal(DecisionKind.SiegeContinuation, state.Pending!.Kind);
        Assert.DoesNotContain(CommandKind.UseEvilKnowledge, state.Pending.Commands);
        LordsFixture.Reject(state, CommandKind.UseEvilKnowledge, new());
        Assert.Equal(elites, StrongholdRules.Elites(state, BattleRuntime.Active(state)!).ToList());
        state = LordsFixture.Do(state, CommandKind.ExtendSiege, new() { Ids = elites });
        Assert.Equal(3, BattleRuntime.Active(state)!.Round);
        Assert.True(state.Lords!.EvilKnowledgeUsed);
    }
}
