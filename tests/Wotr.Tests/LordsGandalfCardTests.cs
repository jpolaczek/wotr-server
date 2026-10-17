using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.LordsFixture;

namespace Wotr.Tests;

public class LordsGandalfCardTests
{
    [Fact]
    public void KeeperGreyCanPlayWizardsStaffAndItExpiresWhenHeLeavesTheFellowship()
    {
        var state = Actions();
        var gandalf = new CharacterState { Id = CharacterRules.Grey, Location = "fellowship" };
        CharacterLifecycle.ApplyVersion(gandalf, CharacterCatalog.Lords[LordsCharacters.Gandalf]);
        state.Characters.Add(gandalf);
        EventTests.GiveCard(state, Side.FreePeoples, FreePeoplesCharacterCards02.Staff);
        state = Do(state, CommandKind.PlayEvent, EventTests.Play(state, FreePeoplesCharacterCards02.Staff, DieFace.Character).Payload);
        Assert.Contains(FreePeoplesCharacterCards02.Staff, state.Player(Side.FreePeoples).Table);
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var payload = CharacterFixture.Cost(state, DieFace.Character);
        payload.Ids = [CharacterRules.Grey];
        payload.Route = [state.Fellowship.RegionId];
        state = Do(state, CommandKind.SeparateCompanions, payload);
        Assert.DoesNotContain(FreePeoplesCharacterCards02.Staff, state.Player(Side.FreePeoples).Table);
        Assert.Contains(FreePeoplesCharacterCards02.Staff, state.Player(Side.FreePeoples).Discard);
    }

    [Fact]
    public void KeeperGreyCanUseServantWithoutInheritingTheCaptainStrengthBonus()
    {
        var state = CardPackBattleFixture.BeforeCards(FreePeoplesCharacterCards02.Staff, CharacterRules.Grey);
        state.Configuration.Lords = true;
        CharacterLifecycle.ApplyVersion(state.Characters.Single(c => c.Id == CharacterRules.Grey), CharacterCatalog.Lords[LordsCharacters.Gandalf]);
        state = CardPackBattleFixture.Reveal(state, FreePeoplesCharacterCards02.Staff);
        var force = BattleRuntime.Active(state)!.Attacker;
        Assert.Equal(1, force.Round.CombatModifier);
        Assert.Equal(4, force.Round.Dice.Count); // Three Regulars plus Gimli, without Grey's base Captain.
    }

    [Fact]
    public void BesiegedKeeperGreyAllowsDenethorRemovalWithAnOrdinaryDie()
    {
        var state = LordsCharacterFixture.State();
        var gandalf = LordsCharacterFixture.Put(state, LordsCharacters.Gandalf, DenethorRules.Region, CharacterRules.Grey);
        gandalf.Garrison = true;
        state.Player(Side.Shadow).Table.Add(ShadowStrategyCards01.Denethor);
        var payload = CharacterFixture.Cost(state, DieFace.Muster);
        Assert.Contains(payload.DieId, DenethorRemoval.Option(state, Side.FreePeoples)!.EligibleDice);
        state = Do(state, CommandKind.DiscardDenethor, payload);
        Assert.Contains(ShadowStrategyCards01.Denethor, state.Player(Side.Shadow).Discard);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == payload.DieId).Location);
    }
}
