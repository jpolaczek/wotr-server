using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

public class TurnTests
{
    private static readonly GameEngine Engine = new(new TurnCommands(), new DeclineReactionHandler());
    private static readonly Projection Projection = new(new TurnOptions(), new CoreOptions());

    private static MatchState Execute(MatchState state, CommandKind kind, CommandPayload? payload = null, IRandomSource? random = null)
    {
        var command = KernelTests.Command(state, kind);
        command.Payload = payload ?? new();
        var result = Engine.Execute(state, state.Pending!.Owner, command, random ?? new SequenceRandom());
        Assert.True(result.Receipt.Accepted, result.Receipt.Error);
        return result.State;
    }

    public static MatchState Actions(int fp = 4, int sh = 7, Side actor = Side.FreePeoples)
    {
        var state = KernelTests.NewGame();
        state.Turn = 1;
        state.Phase = GamePhase.ActionResolution;
        foreach (var side in Enum.GetValues<Side>())
        {
            var index = 0;
            foreach (var die in state.Dice.Where(d => d.Owner == side))
            {
                die.Location = index++ < (side == Side.FreePeoples ? fp : sh) ? DieLocation.Available : DieLocation.Removed;
                die.Face = DieFace.Character;
            }
        }
        TurnFlow.SelectNext(state, actor);
        return state;
    }

    [Fact]
    public void FullTurnFollowsSixPhasesAndStartsWithFreePeoples()
    {
        var state = KernelTests.NewGame();
        TurnFlow.Start(state);
        Assert.Equal(GamePhase.Fellowship, state.Phase);
        Assert.Equal(2, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(2, state.Player(Side.Shadow).Hand.Count);
        state = Execute(state, CommandKind.ContinueFellowship);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        var allocation = DiceRules.HuntCandidates(state).Take(2).ToList();
        var random = new SequenceRandom(0, 1, 2, 5, 0, 1, 2, 3, 5);
        state = Execute(state, CommandKind.AllocateHunt, new()
        {
            Ids = allocation
        }, random);
        Assert.Equal(9, random.Calls);
        Assert.Equal(3, state.Dice.Count(d => d.Location == DieLocation.Hunt));
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.False(TurnCommands.CanPass(state, Side.FreePeoples));
        while (state.Turn == 1)
        {
            var actor = state.Pending!.Owner;
            var die = state.Dice.First(d => d.Owner == actor && d.Location == DieLocation.Available);
            state = Execute(state, CommandKind.DiscardDie, new()
            {
                DieId = die.Id
            });
        }
        Assert.Equal(GamePhase.Fellowship, state.Phase);
        Assert.Equal(4, state.Player(Side.FreePeoples).Hand.Count);
        Assert.All(state.Dice.Where(d => d.Location != DieLocation.Removed), d => Assert.Equal(DieLocation.Unrolled, d.Location));
        Assert.Equal(new[] { "RecoverDraw", "Fellowship", "HuntAllocation", "ActionRoll", "ActionResolution", "VictoryCheck", "RecoverDraw", "Fellowship" },
            state.Events.Where(e => e.Kind == "PhaseChanged").Select(e => e.Text.Split(": ")[1].TrimEnd('.')));
    }

    [Theory]
    [InlineData(4, 7, true)]
    [InlineData(4, 4, false)]
    [InlineData(4, 3, false)]
    public void V01PassRequiresStrictlyFewerUnusedDice(int fp, int sh, bool allowed)
    {
        var state = Actions(fp, sh);
        var command = KernelTests.Command(state, CommandKind.Pass);
        var result = Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom());
        Assert.Equal(allowed, result.Receipt.Accepted);
        Assert.Equal(fp, TurnFlow.Available(result.State, Side.FreePeoples));
        if (allowed)
            Assert.Equal(Side.Shadow, result.State.Pending!.Owner);
    }

    [Fact]
    public void DiscardIsDistinctFromPassingAndOnlyRemainingSideKeepsActing()
    {
        var state = Actions(2, 0);
        Assert.False(TurnCommands.CanPass(state, Side.FreePeoples));
        var id = state.Dice[0].Id;
        state = Execute(state, CommandKind.DiscardDie, new()
        {
            DieId = id
        });
        Assert.Equal(DieLocation.Discarded, state.Dice.Single(d => d.Id == id).Location);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        Assert.Single(state.Dice, d => d.Location == DieLocation.Available);
    }

    [Theory]
    [InlineData(0, false, 0, 1)]
    [InlineData(0, true, 1, 1)]
    [InlineData(2, false, 0, 2)]
    [InlineData(2, true, 1, 2)]
    public void HuntMinimumUsesRecoveredMovementDieAndMaximumUsesCompanions(int companions, bool moved, int minimum, int maximum)
    {
        var state = KernelTests.NewGame();
        state.Fellowship.Companions = state.Fellowship.Companions.Take(companions).ToList();
        state.Fellowship.Guide = CompanionRules.Guides(state)[0];
        state.Dice[0].Location = DieLocation.Hunt;
        state.Dice[0].HuntOrigin = moved ? DiceRules.FellowshipMove : "keeper-eye";
        TurnFlow.Start(state);
        state = Execute(state, CommandKind.ContinueFellowship);
        var option = Assert.Single(Projection.For(state, Side.Shadow).Options);
        Assert.Equal(minimum, option.Minimum);
        Assert.Equal(maximum, option.Maximum);
        var command = KernelTests.Command(state, CommandKind.AllocateHunt);
        command.Payload.Ids = option.EligibleIds.Take(minimum).ToList();
        Assert.Null(Engine.Validate(state, Side.Shadow, command));
        command.Payload.Ids = option.EligibleIds.Take(maximum + 1).ToList();
        Assert.Equal("IllegalChoice", Engine.Validate(state, Side.Shadow, command));
        if (moved)
        {
            command.Payload.Ids.Clear();
            Assert.Equal("IllegalChoice", Engine.Validate(state, Side.Shadow, command));
        }
    }

    [Fact]
    public void DrawBothCardsBeforeFaceDownDiscardAndResumeBothPlayersAfterRestart()
    {
        var state = KernelTests.NewGame();
        foreach (var player in state.Players)
        {
            player.Hand.AddRange(player.Decks["character"].Take(6));
            player.Decks["character"].RemoveRange(0, 6);
        }
        TurnFlow.Start(state);
        Assert.Equal(GamePhase.RecoverDraw, state.Phase);
        Assert.Equal(8, state.Player(Side.FreePeoples).Hand.Count);
        Assert.Equal(8, state.Player(Side.Shadow).Hand.Count);
        Assert.Equal(2, state.Pending!.RequiredCount);
        var privateCards = state.Player(Side.FreePeoples).Hand.ToList();
        var shadowJson = JsonSerializer.Serialize(Projection.For(state, Side.Shadow), BoardCatalog.Json);
        Assert.All(privateCards, id => Assert.DoesNotContain(id, shadowJson));
        var bad = KernelTests.Command(state, CommandKind.DiscardCards);
        bad.Payload.Ids = [privateCards[0], privateCards[0]];
        Assert.Equal("IllegalChoice", Engine.Validate(state, Side.FreePeoples, bad));
        state = Execute(StateCodec.Copy(state), CommandKind.DiscardCards, new()
        {
            Ids = privateCards.Take(2).ToList()
        });
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.Equal(8, state.Player(Side.Shadow).Hand.Count);
        state = Execute(StateCodec.Copy(state), CommandKind.DiscardCards, new()
        {
            Ids = state.Player(Side.Shadow).Hand.Take(2).ToList()
        });
        Assert.Equal(GamePhase.Fellowship, state.Phase);
        Assert.All(state.Players, p => Assert.Equal(6, p.Hand.Count));
    }

    [Fact]
    public void EmptyDeckDoesNotReshuffleAndCannotPayToDrawFromIt()
    {
        var state = KernelTests.NewGame();
        var player = state.Player(Side.FreePeoples);
        player.Discard.AddRange(player.Decks["character"]);
        player.Decks["character"].Clear();
        TurnFlow.Start(state);
        Assert.Single(player.Hand);
        Assert.Equal(24, player.Discard.Count);
        Assert.Empty(player.Decks["character"]);
        state.Phase = GamePhase.ActionResolution;
        state.Dice[0].Location = DieLocation.Available;
        state.Dice[0].Face = DieFace.Event;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        var command = KernelTests.Command(state, CommandKind.DrawCard);
        command.Payload = new()
        {
            DieId = state.Dice[0].Id,
            Deck = "character"
        };
        Assert.False(Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom()).Receipt.Accepted);
        Assert.Equal(DieLocation.Available, state.Dice[0].Location);
        Assert.Equal(new[] { "strategy" }, Projection.For(state, Side.FreePeoples).Options.Single(o => o.Kind == CommandKind.DrawCard).Targets);
    }

    [Fact]
    public void EventDrawKeepsPaidDieResolvingUntilDiscardAndWildCostWorks()
    {
        var state = Actions();
        var player = state.Player(Side.FreePeoples);
        player.Hand.AddRange(player.Decks["character"].Take(6));
        player.Decks["character"].RemoveRange(0, 6);
        state.Dice[0].Face = DieFace.WillOfTheWest;
        var id = state.Dice[0].Id;
        state = Execute(state, CommandKind.DrawCard, new()
        {
            DieId = id,
            Deck = "strategy"
        });
        Assert.Equal(DecisionKind.DiscardCards, state.Pending!.Kind);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        Assert.Equal(DieFace.Event, state.Dice[0].Face);
        Assert.Equal(new[] { id }, state.Resolution.PaidDieIds);
        state = Execute(StateCodec.Copy(state), CommandKind.DiscardCards, new()
        {
            Ids = [state.Player(Side.FreePeoples).Hand[0]]
        });
        Assert.Equal(DieLocation.Spent, state.Dice.Single(d => d.Id == id).Location);
        Assert.Empty(state.Resolution.PaidDieIds);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void V02RingChangesOneDieThenAnotherPaysAndSecondRingIsRejectedAfterRestart()
    {
        var state = Actions();
        state.Dice[1].Face = DieFace.Event;
        var pending = state.Pending!.Id;
        state = Execute(state, CommandKind.UseRing, new()
        {
            RingId = state.Rings[0].Id,
            DieId = state.Dice[0].Id,
            Face = DieFace.Muster
        });
        state = StateCodec.Copy(state);
        Assert.Equal(pending, state.Pending!.Id);
        Assert.Equal(Side.Shadow, state.Rings[0].Owner);
        // QUAL-04: four unused dice against seven still allow a pass after the Ring (BASE p. 21).
        Assert.True(TurnCommands.CanPass(state, Side.FreePeoples));
        var secondRing = KernelTests.Command(state, CommandKind.UseRing);
        secondRing.Payload = new()
        {
            RingId = state.Rings[1].Id,
            DieId = state.Dice[0].Id,
            Face = DieFace.Army
        };
        Assert.False(Engine.Execute(state, Side.FreePeoples, secondRing, new SequenceRandom()).Receipt.Accepted);
        state = Execute(state, CommandKind.DrawCard, new()
        {
            DieId = state.Dice[1].Id,
            Deck = "character"
        });
        Assert.Equal(DieLocation.Available, state.Dice[0].Location);
        Assert.Equal(DieFace.Muster, state.Dice[0].Face);
        Assert.Equal(DieLocation.Spent, state.Dice[1].Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
    }

    [Fact]
    public void V02ShadowEyeFromRingKeepsItsActionAndRingIsRemoved()
    {
        var state = Actions(actor: Side.Shadow);
        state.Rings[0].Owner = Side.Shadow;
        var dice = state.Dice.Where(d => d.Owner == Side.Shadow).Take(2).ToList();
        var decision = state.Pending!.Id;
        state = Execute(state, CommandKind.UseRing, new()
        {
            RingId = state.Rings[0].Id,
            DieId = dice[0].Id,
            Face = DieFace.Eye
        });
        Assert.Equal(decision, state.Pending!.Id);
        Assert.Equal(Side.Shadow, state.Pending.Owner);
        Assert.Null(state.Rings[0].Owner);
        Assert.Equal(DieLocation.Hunt, state.Dice.Single(d => d.Id == dice[0].Id).Location);
        state = Execute(state, CommandKind.DiscardDie, new()
        {
            DieId = dice[1].Id
        });
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
    }

    [Theory]
    [InlineData(DieFace.WillOfTheWest)]
    [InlineData(DieFace.Eye)]
    public void FreePeoplesCannotObtainForbiddenRingFaces(DieFace face)
    {
        var state = Actions();
        var command = KernelTests.Command(state, CommandKind.UseRing);
        command.Payload = new()
        {
            RingId = state.Rings[0].Id,
            DieId = state.Dice[0].Id,
            Face = face
        };
        Assert.False(Engine.Execute(state, Side.FreePeoples, command, new SequenceRandom()).Receipt.Accepted);
    }

    [Fact]
    public void CharacterContributionChangesOnlyAtNextRecoveryAndMovementCostHasCorrectOrigin()
    {
        var state = Actions();
        state.Player(Side.FreePeoples).ExtraDiceNextRecovery = 1;
        Assert.Equal(4, TurnFlow.Available(state, Side.FreePeoples));
        DiceRules.Pay(state, Side.FreePeoples, state.Dice[0].Id, DieFace.Character);
        state = StateCodec.Copy(state);
        Assert.Equal(DieLocation.Resolving, state.Dice[0].Location);
        TurnFlow.FinishAction(state, Side.FreePeoples, fellowshipMove: true);
        Assert.Equal(DiceRules.FellowshipMove, state.Dice[0].HuntOrigin);
        Assert.Equal(DieLocation.Hunt, state.Dice[0].Location);
        TurnFlow.BeginTurn(state);
        Assert.True(state.MandatoryHuntAllocation);
        Assert.Equal(5, state.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Unrolled));
        state.Player(Side.FreePeoples).ExtraDiceNextRecovery = 0;
        Assert.Equal(5, state.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Unrolled));
        TurnFlow.BeginTurn(state);
        Assert.Equal(4, state.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Unrolled));
    }

    [Fact]
    public void ReactionReturnsToSameActorAndOnlyMilitaryCheckEndsAtPhaseSix()
    {
        var state = Actions();
        var decision = state.Pending!.Id;
        state.Resolution.Suspend(state, "test-reaction", 1);
        state.Open(DecisionKind.Reaction, Side.Shadow, [CommandKind.DeclineReaction]);
        state = Execute(StateCodec.Copy(state), CommandKind.DeclineReaction);
        Assert.Equal(decision, state.Pending!.Id);
        Assert.Equal(Side.FreePeoples, state.Pending.Owner);
        foreach (var region in BoardCatalog.LoadBase().Regions.Where(r => r.OriginalOwner == Side.Shadow && r.VictoryPoints == 2).Take(2))
            state.Control[region.Id] = Side.FreePeoples;
        Assert.Null(state.Winner);
        foreach (var die in state.Dice)
            die.Location = DieLocation.Spent;
        TurnFlow.SelectNext(state, Side.FreePeoples);
        Assert.Equal(Side.FreePeoples, state.Winner);
        Assert.Equal(GamePhase.Finished, state.Phase);
        Assert.Null(state.Pending);
    }
    [Theory]
    [InlineData(9, 3, null)]
    [InlineData(10, 3, Side.Shadow)]
    [InlineData(9, 4, Side.FreePeoples)]
    [InlineData(10, 4, Side.Shadow)]
    public void MilitaryThresholdsAndSimultaneousPriorityMatchPhaseSix(int shadowPoints, int freePoints, Side? winner)
    {
        var state = KernelTests.NewGame();
        SetPoints(state, Side.Shadow, shadowPoints);
        SetPoints(state, Side.FreePeoples, freePoints);
        Assert.Equal(shadowPoints, MilitaryVictory.Points(state, Side.Shadow));
        Assert.Equal(freePoints, MilitaryVictory.Points(state, Side.FreePeoples));
        state.Phase = GamePhase.VictoryCheck;
        Assert.Equal(winner.HasValue, MilitaryVictory.Check(state));
        Assert.Equal(winner, state.Winner);
    }

    private static void SetPoints(MatchState state, Side side, int points)
    {
        foreach (var region in BoardCatalog.LoadBase().Regions
            .Where(r => r.OriginalOwner == TurnFlow.Other(side) && r.VictoryPoints > 0)
            .OrderByDescending(r => r.VictoryPoints))
        {
            if (region.VictoryPoints > points)
                continue;
            state.Control[region.Id] = side;
            points -= region.VictoryPoints;
            if (points == 0)
                break;
        }
        Assert.Equal(0, points);
    }

}
