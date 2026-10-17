using Wotr.Contracts;
using Wotr.Domain;
using Xunit;
using static Wotr.Domain.ShadowCharacterCards01;
using static Wotr.Domain.FreePeoplesCharacterCards01;
using static Wotr.Domain.FreePeoplesCharacterCards02;
using static Wotr.Tests.CardPackPersistenceTests;
using static Wotr.Tests.CharacterFixture;
using static Wotr.Tests.Shadow01Fixture;

namespace Wotr.Tests;

public class Shadow01PersistenceTests
{
    [PostgresFact]
    public async Task EveryEventRestartsBeforePaymentWithoutRepeatingItsTileOrHunt()
    {
        foreach (var card in ShadowCharacterCards01.SpecialTiles.Select(p => p.Card).Concat([OrcPatrol, IsildursBane]))
        {
            var state = BeforeEvent(card);
            var hunt = card is OrcPatrol or IsildursBane;
            if (hunt)
                Pool(state, HuntTests.Three);
            state = await RestartDecision(state, CommandKind.PlayEvent, EventCost(state, card), new SequenceRandom(hunt ? [0] : []));
            if (card == OrcPatrol)
                state = await RestartDecision(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom());
            Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
            Assert.Single(state.Player(Side.Shadow).Discard, id => id == card);
            Assert.Equal(hunt ? 3 : 0, state.Fellowship.Corruption);
            Assert.Equal(hunt ? 1 : 0, state.RandomSamples.Count);
        }
    }

    [PostgresFact]
    public async Task PatrolRestoresDrawReplacementProtectionCasualtyAndGuideWindows()
    {
        var state = BeforeEvent(OrcPatrol);
        state.Fellowship.Guide = "base.character.legolas";
        state.Player(Side.FreePeoples).Table.AddRange([Staff, Mithril, Axe]);
        Pool(state, RevealOne, HuntTests.Three);
        state = Play(state, OrcPatrol);
        state = await RestartDecision(state, CommandKind.DeclineReaction, new(), new SequenceRandom(0));
        Assert.Equal("hunt.replace", state.Pending!.Window);
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = ReplaceReaction }, new SequenceRandom(0));
        Assert.Equal("hunt.table", state.Pending!.Window);
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide }, new SequenceRandom());
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
        state = await RestartDecision(state, CommandKind.SelectGuide, new() { GuideId = CharacterRules.Grey }, new SequenceRandom());
        Assert.Equal(0, state.Fellowship.Corruption);
        Assert.Equal("pool", state.HuntTiles.Single(t => t.Id == RevealOne).Location);
        Assert.Equal("discard", state.HuntTiles.Single(t => t.Id == HuntTests.Three).Location);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Contains(Staff, state.Player(Side.FreePeoples).Table);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task BaneRestoresDirectDamageBeforeRevealAndIndependentExtraHunt()
    {
        var state = BeforeEvent(IsildursBane, "moria");
        state.Fellowship.Progress = 1;
        state.Player(Side.FreePeoples).Table.AddRange([Mithril, Axe]);
        Pool(state, RevealOne, HuntTests.Three);
        state = Play(state, IsildursBane, 0);
        state = await RestartDecision(state, CommandKind.DeclineReaction, new(), new SequenceRandom());
        Assert.Equal(1, state.Fellowship.Corruption);
        Assert.True(HuntOptions.View(state)!.CorruptionOnly);
        state = await RestartDecision(state, CommandKind.RevealFellowship, new() { Route = ["moria", "dimrill-dale"] }, new SequenceRandom(0));
        Assert.False(HuntOptions.View(state)!.CorruptionOnly);
        Assert.Equal("hunt.replace", state.Pending!.Window);
        state = await RestartDecision(state, CommandKind.DeclineReaction, new(), new SequenceRandom());
        state = await RestartDecision(state, CommandKind.RespondEffect, new() { ReactionId = ProtectReaction }, new SequenceRandom());
        state = await RestartDecision(state, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new SequenceRandom());
        Assert.Equal(3, state.Fellowship.Corruption);
        Assert.Equal(2, state.RandomSamples.Count);
        Assert.Empty(state.Resolution.Frames);
    }

    [PostgresFact]
    public async Task CruelRestoresIndividualLeadershipCostAndBothPrivateRerolls()
    {
        foreach (var card in new[] { OrcPatrol, IsildursBane })
        {
            var state = Reveal(BeforeCombat(card), card);
            var selected = Choices(state).EffectChoices.Where(c => c.SourceId == CharacterRules.WitchKing).Select(c => c.Id).ToList();
            state = await RestartDecision(state, CommandKind.ChooseEffect, new() { Ids = selected }, new SequenceRandom(new int[8]));
            state = await RestartDecision(state, CommandKind.RerollBattle, new(), new SequenceRandom());
            var id = BattleRuntime.Active(state)!.Force(Side.Shadow).Round.Dice[0].Id;
            state = await RestartDecision(state, CommandKind.RerollBattle, new() { Ids = [id] }, new SequenceRandom(3));
            var round = BattleRuntime.Active(state)!.Force(Side.Shadow).Round;
            Assert.Equal(2, round.ForfeitedLeadership[CharacterRules.WitchKing]);
            Assert.Equal(2, BattleProjection.View(state, Side.FreePeoples)!.Defender.ForfeitedLeadership[CharacterRules.WitchKing]);
            Assert.Equal(1, round.Dice[0].Modifier);
            Assert.Equal(0, round.Dice[0].RerollModifier);
            Assert.Single(state.Events, e => e.Kind == "NazgulLeadershipForfeited");
        }
    }
}
