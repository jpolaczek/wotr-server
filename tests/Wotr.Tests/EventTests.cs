using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class EventTests
{
    public static readonly GameEngine Engine = new(new TurnCommands(), new MusterCommands(),
        new DeclineReactionHandler(), new EffectCommands());
    public static readonly Projection Views = new(new CoreOptions(), new TurnOptions(), new MusterOptions(), new EffectOptions());

    internal static void GiveCard(MatchState state, Side side, string card)
    {
        var player = state.Player(side);
        foreach (var deck in player.Decks.Values)
            deck.Remove(card);
        player.Hand.Add(card);
    }

    public static GameCommand Play(MatchState state, string card, DieFace face, string nation = "")
    {
        var command = KernelTests.Command(state, CommandKind.PlayEvent);
        command.Payload = new()
        {
            CardId = card,
            CardCost = face,
            NationId = nation,
            DieId = state.Dice.First(d => d.Owner == state.Pending!.Owner && DiceRules.CanPay(d, face)).Id
        };
        return command;
    }

    public static MatchState Execute(MatchState state, GameCommand command, GameEngine? engine = null)
    {
        var result = (engine ?? Engine).Execute(state, state.Pending!.Owner, command, new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    [Theory]
    [InlineData(false, 3, 2)]
    [InlineData(true, 3, 1)]
    [InlineData(true, 1, 0)]
    [InlineData(false, 0, 0)]
    public void BilbosSongHealsAccordingToGuideWithoutNegativeCorruption(bool gollum, int before, int after)
    {
        var state = TurnTests.Actions();
        GiveCard(state, Side.FreePeoples, "base.fp.character.12");
        state.Fellowship.Corruption = before;
        if (gollum)
            state.Fellowship.Guide = "base.character.gollum";
        var command = Play(state, "base.fp.character.12", DieFace.Character);
        var result = Execute(state, command);
        Assert.Equal(after, result.Fellowship.Corruption);
        Assert.Equal(before, state.Fellowship.Corruption);
        Assert.Contains(command.Payload.CardId, result.Player(Side.FreePeoples).Discard);
        Assert.Empty(result.Player(Side.FreePeoples).Hand);
        Assert.Equal(DieLocation.Spent, result.Dice.Single(d => d.Id == command.Payload.DieId).Location);
        Assert.Empty(result.Resolution.Frames);
        Assert.Equal(Side.Shadow, result.Pending!.Owner);
    }

    [Fact]
    public void WisdomActivatesChosenNationBeforeAdvancingAndAllowsPartialAtWarEffect()
    {
        var state = TurnTests.Actions();
        state.Dice[0].Face = DieFace.Muster;
        GiveCard(state, Side.FreePeoples, "base.fp.strategy.08");
        var north = state.Nations.Single(n => n.Id == "north");
        north.Active = false;
        north.StepsToWar = 1;
        state = Execute(state, Play(state, "base.fp.strategy.08", DieFace.Muster, "north"));
        Assert.True(state.Nations.Single(n => n.Id == "north").Active);
        Assert.Equal(0, state.Nations.Single(n => n.Id == "north").StepsToWar);
        Assert.False(state.Nations.Single(n => n.Id == "dwarves").Active);

        // At War is not a printed play restriction; the mandatory advance may have no effect.
        state = TurnTests.Actions();
        state.Dice[0].Face = DieFace.Event;
        GiveCard(state, Side.FreePeoples, "base.fp.strategy.08");
        state.Nations.Single(n => n.Id == "elves").StepsToWar = 0;
        state = Execute(state, Play(state, "base.fp.strategy.08", DieFace.Event, "elves"));
        Assert.Equal(0, state.Nations.Single(n => n.Id == "elves").StepsToWar);
    }

    [Theory]
    [InlineData("unimplemented")]
    [InlineData("not-in-hand")]
    [InlineData("wrong-type")]
    [InlineData("wrong-nation")]
    [InlineData("spent-die")]
    public void IllegalEventDoesNotSpendRevealOrSample(string reason)
    {
        var state = TurnTests.Actions();
        state.Dice[0].Face = DieFace.Event;
        GiveCard(state, Side.FreePeoples, "base.fp.strategy.08");
        var command = Play(state, "base.fp.strategy.08", DieFace.Event, "north");
        if (reason == "not-in-hand")
            state.Player(Side.FreePeoples).Hand.Clear();
        if (reason == "wrong-type")
            command.Payload.CardCost = DieFace.Character;
        if (reason == "wrong-nation")
            command.Payload.NationId = "sauron";
        if (reason == "spent-die")
            state.Dice[0].Location = DieLocation.Spent;
        var before = StateCodec.Write(state);
        var random = new SequenceRandom();
        var engine = reason == "unimplemented" ? new GameEngine(new EffectCommands(new EffectCatalog([], []))) : Engine;
        var result = engine.Execute(state, Side.FreePeoples, command, random);
        Assert.False(result.Receipt.Accepted);
        Assert.Equal("IllegalChoice", result.Receipt.Error);
        Assert.Equal(before, StateCodec.Write(result.State));
        Assert.Equal(0, random.Calls);
    }

    [Fact]
    public void WillOfTheWestExposesDistinctEventAndPrintedTypeCosts()
    {
        var state = TurnTests.Actions();
        state.Dice[0].Face = DieFace.WillOfTheWest;
        GiveCard(state, Side.FreePeoples, "base.fp.strategy.15");
        var option = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.PlayEvent).Events.Single();
        Assert.Equal(new[] { DieFace.Event, DieFace.Muster }, option.Costs.Where(c => c.DieId == state.Dice[0].Id).Select(c => c.Face));
        var result = Execute(state, Play(state, "base.fp.strategy.15", DieFace.Muster));
        Assert.Equal(DieFace.Muster, result.Dice[0].Face);
        Assert.Equal(DieFace.Muster, EffectRuntime.Current(result)!.Effect!.PaidFace);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CelebornDrawsEvenWhenRecruitmentCannotHappen(bool enemyControlsLorien)
    {
        var state = CelebornState();
        if (enemyControlsLorien)
            state.Control["lorien"] = Side.Shadow;
        else
            foreach (var unit in state.Units.Where(u => u.Nation == "elves" && u.Location == "reserve"))
                unit.Location = "casualties";
        var drawn = state.Player(Side.FreePeoples).Decks["strategy"][0];
        state = Execute(state, Play(state, "base.fp.strategy.15", DieFace.Muster));
        Assert.Contains(drawn, state.Player(Side.FreePeoples).Hand);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.DoesNotContain(state.Events, e => e.Kind == "EventRecruited");
    }

    public static MatchState CelebornState()
    {
        var state = TurnTests.Actions();
        GiveCard(state, Side.FreePeoples, "base.fp.strategy.15");
        state.Dice[0].Face = DieFace.Muster;
        return state;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CardRecruitsBeforeWarOnCorrectSideOfSiege(bool freePeoplesBesiege)
    {
        var state = CelebornState();
        var elves = state.Units.Where(u => u.Location == "lorien").ToArray();
        foreach (var elf in elves)
            elf.Garrison = !freePeoplesBesiege;
        var enemy = state.Units.First(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular);
        enemy.Location = "lorien";
        enemy.Garrison = freePeoplesBesiege;
        state.Control["lorien"] = freePeoplesBesiege ? Side.Shadow : Side.FreePeoples;
        Assert.True(state.Nations.Single(n => n.Id == "elves").StepsToWar > 0);
        state = Execute(state, Play(state, "base.fp.strategy.15", DieFace.Muster));
        var elite = state.Pending!.PrivateChoices.Single(id => state.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
        var choice = KernelTests.Command(state, CommandKind.ChooseEffect);
        choice.Payload.Ids = [elite];
        state = Execute(state, choice);
        Assert.Equal("lorien", state.Units.Single(u => u.Id == elite).Location);
        Assert.Equal(!freePeoplesBesiege, state.Units.Single(u => u.Id == elite).Garrison);
    }

    [Fact]
    public void EffectContinuesThroughDiscardAndStackLimitWithoutRepaying()
    {
        var state = CelebornState();
        var player = state.Player(Side.FreePeoples);
        foreach (var card in player.Decks["character"].Take(6).ToArray())
            GiveCard(state, Side.FreePeoples, card);
        var elves = state.Units.Where(u => u.Nation == "elves" && u.Kind == UnitKind.Regular).Take(3)
            .Concat(state.Units.Where(u => u.Nation == "elves" && u.Kind == UnitKind.Elite).Take(7)).ToArray();
        foreach (var unit in state.Units.Where(u => u.Location == "lorien"))
            unit.Location = "reserve";
        foreach (var elf in elves)
            elf.Location = "lorien";
        var play = Play(state, "base.fp.strategy.15", DieFace.Muster);
        state = Execute(state, play);
        var choice = KernelTests.Command(state, CommandKind.ChooseEffect);
        choice.Payload.Ids = [state.Pending!.PrivateChoices.First()];
        state = Execute(StateCodec.Copy(state), choice);
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        var discard = KernelTests.Command(state, CommandKind.DiscardCards);
        discard.Payload.Ids = [state.Player(Side.FreePeoples).Hand[0]];
        state = Execute(StateCodec.Copy(state), discard);
        Assert.Equal(DecisionKind.RemoveExcess, state.Pending!.Kind);
        Assert.Equal(1, state.Pending.RequiredCount);
        var remove = KernelTests.Command(state, CommandKind.RemoveExcess);
        remove.Payload.Ids = [elves[0].Id];
        state = Execute(StateCodec.Copy(state), remove);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Spent);
        Assert.Empty(state.Resolution.PaidDieIds);
        Assert.Empty(state.Resolution.Frames);
        Assert.Equal(6, state.Player(Side.FreePeoples).Hand.Count);
    }

    [Fact]
    public void OnlyAvailableRecruitmentTypeCompletesWithoutRedundantChoice()
    {
        var state = CelebornState();
        foreach (var unit in state.Units.Where(u => u.Nation == "elves" && u.Kind == UnitKind.Elite && u.Location == "reserve"))
            unit.Location = "casualties";
        var before = state.Units.Count(u => u.Location == "lorien" && u.Kind == UnitKind.Regular);
        state = Execute(state, Play(state, "base.fp.strategy.15", DieFace.Muster));
        Assert.Equal(before + 1, state.Units.Count(u => u.Location == "lorien" && u.Kind == UnitKind.Regular));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void ThreatsBlocksPassiveMusterButExpiresOnlyAfterQualifyingAdvance()
    {
        var state = TurnTests.Actions(actor: Side.Shadow);
        var die = state.Dice.First(d => d.Owner == Side.Shadow);
        die.Face = DieFace.Muster;
        GiveCard(state, Side.Shadow, "base.sh.strategy.05");
        state = Execute(state, Play(state, "base.sh.strategy.05", DieFace.Muster));
        var effect = Assert.Single(Views.For(state, Side.FreePeoples).Effects);
        Assert.Equal("base.sh.strategy.05", effect.SourceId);
        Assert.Equal("UntilRemoved", effect.Duration);
        var muster = Views.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.AdvancePolitics);
        Assert.DoesNotContain("north", muster.Targets);
        Assert.Contains("elves", muster.Targets);
        state.Dice[0].Face = DieFace.Muster;
        var forbidden = KernelTests.Command(state, CommandKind.AdvancePolitics);
        forbidden.Payload = new() { NationId = "north", DieId = state.Dice[0].Id };
        Assert.False(Engine.Execute(state, Side.FreePeoples, forbidden, new SequenceRandom()).Receipt.Accepted);
        PoliticalRules.Advance(state, "north", PoliticalAdvanceCause.Event);
        Assert.Single(state.ActiveEffects);
        PoliticalRules.SettlementCaptured(state, "dale", Side.FreePeoples, Side.Shadow);
        Assert.Single(state.ActiveEffects);
        PoliticalRules.Activate(state, "north");
        Assert.Single(state.ActiveEffects);
        PoliticalRules.Advance(state, "north", PoliticalAdvanceCause.CompanionAbility);
        Assert.Empty(state.ActiveEffects);
        Assert.Empty(state.Player(Side.Shadow).Table);
        Assert.Contains("base.sh.strategy.05", state.Player(Side.Shadow).Discard);
    }
}
