using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.LordsShadowCardsFixture;

namespace Wotr.Tests;

public class LordsCards02BoundaryTests
{
    public static TheoryData<string> Cards => new()
    {
        LordsShadowCards.Balrog, LordsShadowCards.WeWontGoBack, LordsShadowCards.Lore,
        LordsShadowCards.Gates, LordsShadowCards.Captain, LordsShadowCards.DolGuldur
    };

    internal static MatchState EventState()
    {
        var state = State();
        SmeagolRules.Enter(state);
        LordsCharacterFixture.Put(state, LordsCharacters.Balrog, "moria");
        LordsCharacterFixture.Put(state, LordsCharacters.WitchKing, "dol-guldur");
        return state;
    }

    internal static MatchState CombatState(string card, bool fpAttacks = true)
    {
        var state = Battle(card, fpAttacks);
        var force = BattleRuntime.Active(state)!.Force(Side.Shadow);
        var balrog = LordsCharacterFixture.Put(state, LordsCharacters.Balrog, force.Region);
        force.Participants.Add(balrog.Id);
        var elite = BattleFixture.Put(state, "southrons", UnitKind.Elite, force.Region);
        force.Participants.Add(elite.Id);
        return state;
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void EveryEventValidatesPrintedCostOwnershipWindowAndModule(string card)
    {
        var state = EventState();
        var payload = Cost(state, card, CardCatalog.All[card].EventType);
        var command = BattleFixture.Command(state, CommandKind.PlayEvent, payload);
        Assert.Null(LordsFixture.Engine().Validate(state, Side.Shadow, command));
        Assert.NotNull(LordsFixture.Engine().Validate(state, Side.FreePeoples, command));
        payload.CardCost = DieFace.Muster;
        LordsFixture.Reject(state, command.Kind, payload);
        payload.CardCost = CardCatalog.All[card].EventType;
        state.Configuration.Lords = false;
        LordsFixture.Reject(state, command.Kind, payload);
        state.Configuration.Lords = true;
        state.Player(Side.Shadow).Hand.Remove(card);
        LordsFixture.Reject(state, command.Kind, payload);
        state.Player(Side.Shadow).Hand.Add(card);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Spent;
        LordsFixture.Reject(state, command.Kind, payload);
        state.Dice.Single(d => d.Id == payload.DieId).Location = DieLocation.Available;
        state.Phase = GamePhase.Fellowship;
        LordsFixture.Reject(state, command.Kind, payload);
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void EveryCombatValidatesOwnershipWindowModuleAndParticipatingFigures(string card)
    {
        foreach (var fpAttacks in new[] { false, true })
        {
            var state = CombatState(card, fpAttacks);
            if (state.Pending!.Owner == Side.FreePeoples)
                state = LordsFixture.Do(state, CommandKind.CommitCombatCard);
            var command = BattleFixture.Command(state, CommandKind.CommitCombatCard, new() { CardId = card });
            Assert.Null(LordsFixture.Engine().Validate(state, Side.Shadow, command));
            Assert.NotNull(LordsFixture.Engine().Validate(state, Side.FreePeoples, command));
            state.Configuration.Lords = false;
            LordsFixture.Reject(state, command.Kind, command.Payload);
            state.Configuration.Lords = true;
            state.Player(Side.Shadow).Hand.Remove(card);
            LordsFixture.Reject(state, command.Kind, command.Payload);
            state.Player(Side.Shadow).Hand.Add(card);
            state.Pending!.Kind = DecisionKind.Action;
            LordsFixture.Reject(state, command.Kind, command.Payload);
            state.Pending.Kind = DecisionKind.CombatCard;
            var battle = BattleRuntime.Active(state)!;
            battle.Force(Side.Shadow).Participants.RemoveAll(id =>
                state.Characters.Any(c => c.Id == id) || state.Units.Any(u => u.Id == id && u.Kind is UnitKind.Nazgul or UnitKind.Elite));
            Assert.False(new BattleCards(EffectCatalog.All).CanCommit(state, battle, Side.Shadow, card));
        }
    }

    [Fact]
    public void LordsDeckReplacesBaseBalrogAndKeepsSpecialCardOutsideShuffledDecks()
    {
        var configuration = new MatchConfiguration { Lords = true };
        var state = MatchFactory.Create("lords-composition", new SequenceRandom(new int[104]), configuration);
        var decks = state.Players.SelectMany(p => p.Decks.Values).ToArray();
        Assert.Equal(new[] { 28, 26, 28, 26 }, decks.Select(d => d.Count));
        Assert.Equal(108, decks.SelectMany(d => d).Distinct().Count());
        Assert.DoesNotContain(ShadowCharacterCards03.Balrog, decks.SelectMany(d => d));
        Assert.Single(decks.SelectMany(d => d), id => id == LordsShadowCards.Balrog);
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, decks.SelectMany(d => d));
        Assert.False(CardCatalog.Enabled(state, ShadowCharacterCards03.Balrog));
        configuration.Lords = false;
        Assert.True(state.Configuration.Lords);
        var baseState = MatchFactory.Create("base-composition", new SequenceRandom(new int[92]));
        Assert.Equal(96, baseState.Players.SelectMany(p => p.Decks.Values).Sum(d => d.Count));
        Assert.True(CardCatalog.Enabled(baseState, ShadowCharacterCards03.Balrog));
        Assert.False(CardCatalog.Enabled(baseState, LordsShadowCards.Balrog));
    }
}
