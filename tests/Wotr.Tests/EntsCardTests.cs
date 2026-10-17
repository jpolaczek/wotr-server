using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Domain.FreePeoplesCharacterCards04;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.FellowshipEventCardTests;

namespace Wotr.Tests;

public class EntsCardTests
{
    internal static MatchState BeforeAttack(bool followUp = false, int regulars = 3)
    {
        var state = BattleFixture.State();
        var white = Put(state, CharacterRules.Grey, followUp ? "fangorn" : "minas-tirith");
        CharacterLifecycle.ApplyVersion(white, CharacterCatalog.Base[CharacterRules.White]);
        Put(state, "base.character.gimli", "fangorn");
        Put(state, CharacterRules.Saruman, "orthanc");
        Put(state, CharacterRules.WitchKing, "orthanc");
        BattleFixture.Put(state, "sauron", UnitKind.Nazgul, "orthanc");
        for (var i = 0; i < regulars; i++)
            BattleFixture.Put(state, "isengard", UnitKind.Regular, "orthanc");
        var isengard = state.Nations.Single(n => n.Id == "isengard");
        isengard.StepsToWar = 2;
        CharacterLifecycle.ReconcileDice(state);
        return state;
    }

    [Theory]
    [InlineData(Treebeard)]
    [InlineData(Huorns)]
    [InlineData(Entmoot)]
    public void AllEntsRequireWhiteAndCompanionButNotSaruman(string card)
    {
        var state = BeforeAttack();
        EventTests.GiveCard(state, Side.FreePeoples, card);
        var payload = Cost(state, DieFace.Character);
        payload.CardId = card;
        payload.CardCost = DieFace.Character;
        state.Characters.Single(c => c.DefinitionId == CharacterRules.White).Location = "casualties";
        Reject(state, CommandKind.PlayEvent, payload);
        state.Characters.Single(c => c.DefinitionId == CharacterRules.White).Location = "minas-tirith";
        var companion = state.Characters.Single(c => c.DefinitionId == "base.character.gimli");
        companion.Location = "edoras";
        Reject(state, CommandKind.PlayEvent, payload);
        companion.Location = "fangorn";
        state.Characters.Single(c => c.DefinitionId == CharacterRules.Saruman).Location = "reserve";
        state = Do(state, CommandKind.PlayEvent, payload, new SequenceRandom(0, 2, 3));
        Assert.Equal(1, EventLosses.Current(state)!.Hits);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(3, state.RandomSamples.Count);
        Assert.Equal(2, state.Nations.Single(n => n.Id == "isengard").StepsToWar);
        Assert.Equal(DieLocation.Resolving, state.Dice.Single(d => d.Id == payload.DieId).Location);
        Assert.Null(BattleRuntime.Active(state));
        Assert.Null(HuntTests.Views().For(state, Side.FreePeoples).Decision);
        state = Do(state, CommandKind.TakeEventLosses, new()
        {
            Losses = [new() { UnitId = state.Units.First(u => u.Location == "orthanc" && u.Kind == UnitKind.Regular).Id }]
        });
        Assert.Equal(1, state.Nations.Single(n => n.Id == "isengard").StepsToWar);
    }

    [Fact]
    public void EmptyOrthancKillsOnlySarumanAndDoesNotAdvancePolitics()
    {
        var state = Play(BeforeAttack(regulars: 0), Treebeard, 5, 5, 5);
        Assert.Equal("casualties", state.Characters.Single(c => c.DefinitionId == CharacterRules.Saruman).Location);
        Assert.Equal("orthanc", state.Characters.Single(c => c.DefinitionId == CharacterRules.WitchKing).Location);
        Assert.Contains(state.Units, u => u.Kind == UnitKind.Nazgul && u.Location == "orthanc");
        Assert.Equal(2, state.Nations.Single(n => n.Id == "isengard").StepsToWar);
        Assert.Equal(1, state.Player(Side.Shadow).ExtraDiceNextRecovery);
        Assert.Empty(state.Resolution.Frames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArmyDestructionRemovesCommandersAndReconcilesSiege(bool shadowInside)
    {
        var state = BeforeAttack();
        foreach (var unit in state.Units.Where(u => u.Location == "orthanc"))
            unit.Garrison = shadowInside;
        foreach (var character in state.Characters.Where(c => c.Location == "orthanc"))
            character.Garrison = shadowInside;
        BattleFixture.Put(state, "rohan", UnitKind.Regular, "orthanc").Garrison = !shadowInside;
        state.Control["orthanc"] = shadowInside ? Side.Shadow : Side.FreePeoples;
        state = Play(state, Treebeard, 3, 4, 5);
        var losses = EventLosses.Current(state)!.Target.Units(state).Where(BoardRules.IsArmyUnit).Select(u => BattleFixture.Loss(u.Id)).ToList();
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = losses });
        Assert.Equal(Side.FreePeoples, state.Control["orthanc"]);
        Assert.All(state.Characters.Where(c => c.Side == Side.Shadow), c => Assert.Equal("casualties", c.Location));
        Assert.DoesNotContain(state.Units, u => u.Location == "orthanc" && u.Garrison);
        Assert.Equal(0, state.Player(Side.Shadow).ExtraDiceNextRecovery);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }

    [Fact]
    public void LossSelectionRejectsWrongForceTooFewTooManyAndSupportsEliteDowngrade()
    {
        var state = BeforeAttack(regulars: 1);
        var elite = BattleFixture.Put(state, "isengard", UnitKind.Elite, "orthanc");
        var outsider = BattleFixture.Put(state, "isengard", UnitKind.Regular, "north-dunland");
        state = Play(state, Treebeard, 3, 4, 0);
        var regular = state.Units.Single(u => u.Location == "orthanc" && u.Kind == UnitKind.Regular);
        Reject(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(outsider.Id)] });
        Reject(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(regular.Id)] });
        Reject(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(regular.Id), BattleFixture.Loss(elite.Id)] });
        state = Do(state, CommandKind.TakeEventLosses, new() { Losses = [BattleFixture.Loss(regular.Id), BattleFixture.Loss(elite.Id, BattleLossKind.Downgrade)] });
        Assert.Single(state.Units, u => u.Location == "orthanc" && BoardRules.IsArmyUnit(u));
        Assert.Equal("orthanc", state.Characters.Single(c => c.DefinitionId == CharacterRules.Saruman).Location);
        Assert.Equal(3, state.RandomSamples.Count);
    }

    [Fact]
    public void ThreeEntsChainIntoOneCharacterEventUsingOnlyOneDie()
    {
        var state = BeforeAttack(true, 0);
        foreach (var card in new[] { Huorns, Entmoot, FreePeoplesCharacterCards02.Bilbo })
            EventTests.GiveCard(state, Side.FreePeoples, card);
        state.Fellowship.Corruption = 2;
        state = Play(state, Treebeard, 0, 0, 0);
        foreach (var card in new[] { Huorns, Entmoot, FreePeoplesCharacterCards02.Bilbo })
        {
            Assert.Equal("ents.follow-up", state.Pending!.Window);
            var option = Assert.Single(new EffectOptions().GetOptions(state, Side.FreePeoples), o => o.Kind == CommandKind.PlayEvent);
            Assert.All(option.Events.SelectMany(e => e.Costs), cost => Assert.Equal("", cost.DieId));
            Reject(state, CommandKind.PlayEvent, new() { CardId = card, DieId = state.Dice[1].Id, CardCost = DieFace.Character });
            state = Do(state, CommandKind.PlayEvent, new() { CardId = card, CardCost = DieFace.Character }, new SequenceRandom(0, 0, 0));
        }
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(9, state.RandomSamples.Count);
    }

    [Fact]
    public void EmptyOrIneligibleHandStillOffersPrivateFollowUpAndDecline()
    {
        var state = BeforeAttack(true, 0);
        state.Player(Side.FreePeoples).Hand.Clear();
        EventTests.GiveCard(state, Side.FreePeoples, "base.fp.strategy.08");
        state = Play(state, Entmoot, 0, 0, 0);
        Assert.Equal("ents.follow-up", state.Pending!.Window);
        Assert.Empty(new EffectOptions().GetOptions(state, Side.FreePeoples));
        var shadow = HuntTests.Views().For(state, Side.Shadow);
        Assert.Null(shadow.Decision);
        Assert.Empty(shadow.Options);
        Reject(state, CommandKind.PlayEvent, new() { CardId = "base.fp.strategy.08", CardCost = DieFace.Character });
        state = Do(state, CommandKind.DeclineReaction);
        Assert.Empty(state.Resolution.Frames);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
    }
}
