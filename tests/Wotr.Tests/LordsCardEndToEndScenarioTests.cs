using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// TEST-12: every Lords card of the decks has an event and a combat scenario in the Lords tab. The event scenario offers
// the card as a legal event paid by a die of the turn, its play runs through the engine and its clauses act on the
// prepared board; the combat scenario waits on the owner's Combat card choice, which offers the card in a battle with the
// figures its effect acts on, and committing it runs through the engine. The follow-up scenarios wait on the decision
// they name and answering it is accepted.
public class LordsCardEndToEndScenarioTests
{
    private static readonly GameEngine Engine = GameRuntime.Engine(EffectCatalog.All);
    private static readonly Projection Views = GameRuntime.Projection(EffectCatalog.All);

    public static TheoryData<string> Cards => new(DeckCards().Select(c => c.Id));

    private static IEnumerable<CardDefinition> DeckCards() =>
        CardCatalog.Lords.Values.Where(c => c.Deck != "special").OrderBy(c => c.Id, StringComparer.Ordinal);

    [Fact]
    public void EveryLordsCardHasAnEventAndACombatScenarioInTheLordsTab()
    {
        Assert.Equal(13, DeckCards().Count());
        foreach (var card in DeckCards())
        {
            var play = Scenario(LordsCardScenarios.EventId(card));
            var combat = Scenario(LordsCardScenarios.CombatId(card));
            Assert.Equal((card.Side, card.Side), (play.StartingSide, combat.StartingSide));
            Assert.Equal(card.Title + " · Event", play.Title);
            Assert.Equal(card.Title + " · Combat: " + CardTextCatalog.CombatTitle(card.Id), combat.Title);
            Assert.Contains(card.Title, play.Description);
            Assert.Contains(card.Title, combat.Description);
        }
        var lords = LordsCardScenarios.All().ToList();
        Assert.Equal(lords.Count, lords.Select(s => s.Id).Distinct().Count());
        Assert.All(lords, s => Assert.StartsWith("lords-card-", s.Id));
        Assert.DoesNotContain(lords, s => TestScenarioCatalog.All.Count(o => o.Id == s.Id) != 1);
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void TheEventScenarioOffersTheCardAndItsClausesAct(string cardId)
    {
        var card = CardCatalog.All[cardId];
        var state = Create(LordsCardScenarios.EventId(card));
        var view = Views.For(state, card.Side);
        var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.PlayEvent);
        var play = option?.Events.FirstOrDefault(e => e.CardId == cardId);
        Assert.True(option is { Enabled: true } && play is not null, $"{cardId}: no legal event play ({option?.Reason})");
        var cost = play!.Costs.First(c => c.AbilityId.Length == 0);
        var after = Run(state, card.Side, CommandKind.PlayEvent, new() { CardId = cardId, DieId = cost.DieId, CardCost = cost.Face },
            new SeededRandom(7));
        EventResult(cardId, state, after);
    }

    [Theory]
    [MemberData(nameof(Cards))]
    public void TheCombatScenarioOffersTheCardInABattleWithItsTargets(string cardId)
    {
        var card = CardCatalog.All[cardId];
        var state = Create(LordsCardScenarios.CombatId(card));
        Assert.Equal((DecisionKind.CombatCard, card.Side), (state.Pending!.Kind, state.Pending.Owner));
        Assert.Contains(cardId, Views.For(state, card.Side).Options.Single(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds);
        BattleTargets(cardId, state);
        Run(state, card.Side, CommandKind.CommitCombatCard, new() { CardId = cardId }, new SeededRandom(7));
    }

    [Fact]
    public void ThreeRingsRecoversNaryaFromOutOfPlayAndARingFromTheShadow()
    {
        var state = Create("lords-card-three-rings-recover");
        Assert.Equal("ElvenRings", state.Pending!.Presentation);
        Assert.Equal(2, Option(state, Side.FreePeoples, CommandKind.ChooseEffect).Minimum);
        state = Run(state, Side.FreePeoples, CommandKind.ChooseEffect,
            new() { Ids = [LordsDiceSetup.NaryaRing, LordsDiceSetup.NenyaRing] }, new SeededRandom(1));
        Assert.Equal(Side.FreePeoples, Ring(state, LordsDiceSetup.NaryaRing).Owner);
        Assert.Equal(Side.FreePeoples, Ring(state, LordsDiceSetup.NenyaRing).Owner);
        Assert.Equal(Side.Shadow, Ring(state, LordsDiceSetup.VilyaRing).Owner);
    }

    [Fact]
    public void BladeOfWesternesseSpendsAReRollHitToEliminateAMinion()
    {
        var state = Create("lords-card-blade-minion");
        Assert.Equal("Minion", state.Pending!.Presentation);
        var saruman = state.Characters.Single(c => c.DefinitionId == CharacterRules.Saruman);
        Assert.Equal(new[] { saruman.Id, CharacterRules.WitchKing }.Order(), Choices(state, Side.FreePeoples).Order());
        state = Run(state, Side.FreePeoples, CommandKind.ChooseEffect, new() { Ids = [saruman.Id] }, new SeededRandom(1));
        Assert.False(CharacterRules.OnBoard(state.Characters.Single(c => c.Id == saruman.Id)));
    }

    [Fact]
    public void TheLoreChangesOneShadowResultAfterTheKeeperEye()
    {
        var state = Create("lords-card-lore-change-die");
        Assert.Equal("DieAndResult", state.Pending!.Presentation);
        var vilya = state.Dice.Single(d => d.Id == LordsDiceCatalog.Vilya);
        Assert.Equal((DieFace.Eye, DieLocation.Hunt), (vilya.Face, vilya.Location));
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Source == "standard");
        var result = LoreOfTheRings.Faces(die).First(p => p.Value.Face != DieFace.Eye);
        state = Run(state, Side.Shadow, CommandKind.ChooseEffect, new() { Ids = [die.Id, result.Key] }, new SeededRandom(1));
        Assert.Equal(result.Value.Face, state.Dice.Single(d => d.Id == die.Id).Face);
        Assert.Equal((DecisionKind.Action, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
    }

    [Fact]
    public void TheFreePeoplesDiscardTheLoreWithAnyDieAndAnElvenRing()
    {
        var state = Create("lords-card-lore-discard");
        var option = Option(state, Side.FreePeoples, CommandKind.DiscardLore);
        Assert.Contains(LordsDiceSetup.VilyaRing, option.EligibleIds);
        var army = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Army);
        Assert.Contains(army.Id, option.EligibleDice);
        Assert.NotEmpty(option.DiceWithoutRing);
        state = Run(state, Side.FreePeoples, CommandKind.DiscardLore, new() { DieId = army.Id, RingId = LordsDiceSetup.VilyaRing },
            new SeededRandom(1));
        Assert.DoesNotContain(LordsShadowCards.Lore, state.Player(Side.Shadow).Table);
        Assert.Equal(Side.Shadow, Ring(state, LordsDiceSetup.VilyaRing).Owner);
    }

    [Fact]
    public void GandalfAsGuideLetsACharacterDieDiscardTheGates()
    {
        var state = Create("lords-card-gates-discard");
        Assert.Equal(CharacterRules.Grey, state.Fellowship.Guide);
        var option = Option(state, Side.FreePeoples, CommandKind.DiscardGates);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character);
        Assert.Contains(die.Id, option.EligibleDice);
        state = Run(state, Side.FreePeoples, CommandKind.DiscardGates, new() { DieId = die.Id }, new SeededRandom(1));
        Assert.DoesNotContain(LordsShadowCards.Gates, state.Player(Side.Shadow).Table);
    }

    [Fact]
    public void DeclaringInMoriaOffersTheGatesForAnAdditionalHuntTile()
    {
        var state = Create("lords-card-gates-declaration");
        Assert.Equal((DecisionKind.FellowshipPhase, 2), (state.Pending!.Kind, state.Turn));
        Assert.Contains(LordsShadowCards.Gates, state.Player(Side.Shadow).Table);
        state = Run(state, Side.FreePeoples, CommandKind.DeclareFellowship, new() { Route = ["hollin", "moria"] }, new SeededRandom(1));
        Assert.Equal((DecisionKind.Reaction, Side.Shadow, "fellowship.declared"), (state.Pending!.Kind, state.Pending.Owner, state.Pending.Window));
        Assert.Contains(Option(state, Side.Shadow, CommandKind.RespondEffect).EffectChoices, c => c.SourceId == LordsShadowCards.Gates);
        state = Run(state, Side.Shadow, CommandKind.RespondEffect, new() { ReactionId = LordsShadowCards.GatesReaction + ".declaration" },
            new SeededRandom(1));
        Assert.DoesNotContain(LordsShadowCards.Gates, state.Player(Side.Shadow).Table);
        Assert.NotNull(HuntRuntime.Active(state));
    }

    [Fact]
    public void WeShallGetItReplacesTheDrawnTileWithGollumAsGuide()
    {
        var state = Create("lords-card-we-shall-get-it");
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Equal(WeShallGetIt.Window, state.Pending!.Window);
        var first = HuntRuntime.Active(state)!.TileId;
        state = Run(state, Side.Shadow, CommandKind.RespondEffect, new() { ReactionId = WeShallGetIt.Reaction }, new SeededRandom(1));
        Assert.DoesNotContain(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.NotEqual(first, HuntRuntime.Active(state)?.TileId);
    }

    private static void EventResult(string cardId, MatchState before, MatchState after)
    {
        switch (cardId)
        {
            case LordsFreePeoplesCards.YouKnowTheWay:
                Assert.Equal(2, before.HuntTiles.Count(t => t.Location == "set-aside" && SmeagolTile(t.Id)));
                Assert.All(SmeagolTiles.Definitions.Skip(2), tile => Assert.Equal("pool", Tile(after, tile.Id).Location));
                break;
            case LordsFreePeoplesCards.SafePaths:
                Assert.Equal(1, after.Fellowship.Corruption);
                Assert.DoesNotContain(LordsShadowCards.WeWontGoBack, after.Player(Side.Shadow).Table);
                break;
            case LordsFreePeoplesCards.Wind:
                Assert.Equal(new[] { LordsShadowCards.Gates, LordsShadowCards.Lore }.Order(), Choices(after, Side.FreePeoples).Order());
                break;
            case LordsFreePeoplesCards.ThreeRings:
                Assert.Contains(after.Events, e => e.Kind == "RingRecoveryRoll");
                Assert.Equal(3, before.Rings.Count(r => r.Owner != Side.FreePeoples));
                break;
            case LordsFreePeoplesCards.EveryLittle:
                foreach (var region in new[] { "pelargir", "lossarnach", "lamedon" })
                    Assert.Equal(Gondor(before, region) + 1, Gondor(after, region));
                Assert.Equal(before.Player(Side.FreePeoples).Decks["strategy"].Count - 1, after.Player(Side.FreePeoples).Decks["strategy"].Count);
                break;
            case LordsFreePeoplesCards.Rangers:
                Assert.Contains(after.Events, e => e.Kind == "EventArmyRoll" && e.Text.Contains("weather-hills"));
                break;
            case LordsShadowCards.Balrog:
                Assert.Equal(0, Balrog(before).Level);
                Assert.Equal(2, Balrog(after).Level);
                break;
            case LordsShadowCards.WeWontGoBack:
                SmeagolTileCost(after);
                break;
            case LordsShadowCards.Lore:
                Assert.Contains(LordsShadowCards.Lore, after.Player(Side.Shadow).Table);
                Assert.Equal("rivendell", after.Characters.Single(c => c.DefinitionId == LordsCharacters.Elrond).Location);
                break;
            case LordsShadowCards.Gates:
                Assert.All(PoolTiles(after), tile => Assert.True(tile.Reveal));
                GatesAtTheReveal(after);
                break;
            case LordsShadowCards.Captain:
                CaptainTurnsTheNextCharacterDieIntoAnEye(after);
                break;
            case LordsShadowCards.DolGuldur:
                Assert.Equal(Sauron(before, "dol-guldur") + 3, Sauron(after, "dol-guldur"));
                break;
            case LordsShadowCards.Madness:
                Assert.Equal(["mount-gundabad", "north-ithilien"], Choices(after, Side.Shadow).Order());
                break;
        }
    }

    // We Won't Go Back on the table: the Free Peoples' move meets the Hunt, which draws the Sméagol tile, and the Shadow
    // may discard a Character card or the table card for 2 Corruption.
    private static void SmeagolTileCost(MatchState state)
    {
        Assert.Contains(LordsShadowCards.WeWontGoBack, state.Player(Side.Shadow).Table);
        Assert.Equal(Side.FreePeoples, state.Pending!.Owner);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character);
        state = Run(state, Side.FreePeoples, CommandKind.MoveFellowship, new() { DieId = die.Id }, new HighestFace());
        Assert.Equal((DecisionKind.EffectChoice, Side.Shadow, "SmeagolCorruption"),
            (state.Pending!.Kind, state.Pending.Owner, state.Pending.Presentation));
        Assert.Contains(LordsShadowCards.WeWontGoBack, Choices(state, Side.Shadow));
        Assert.Contains(Choices(state, Side.Shadow), id => id != LordsShadowCards.WeWontGoBack && CardCatalog.All.TryGetValue(id, out var c) && c.Deck == "character");
    }

    // The Gates on the table: the Free Peoples' move meets the Hunt, which reveals the Fellowship; its route into Moria, an
    // unconquered Shadow Stronghold, offers the Shadow the Gates' additional Hunt tile.
    private static void GatesAtTheReveal(MatchState state)
    {
        Assert.Contains(LordsShadowCards.Gates, state.Player(Side.Shadow).Table);
        var die = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character);
        state = Run(state, Side.FreePeoples, CommandKind.MoveFellowship, new() { DieId = die.Id }, new HighestFace());
        if (state.Pending?.Kind == DecisionKind.HuntDamage)
            state = Run(state, Side.FreePeoples, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Ring }, new HighestFace());
        Assert.Equal((DecisionKind.FellowshipRoute, Side.FreePeoples), (state.Pending!.Kind, state.Pending.Owner));
        state = Run(state, Side.FreePeoples, CommandKind.RevealFellowship, new() { Route = ["hollin", "moria"] }, new HighestFace());
        Assert.Equal((DecisionKind.Reaction, Side.Shadow, "fellowship.revealed"), (state.Pending!.Kind, state.Pending.Owner, state.Pending.Window));
        Assert.Contains(Option(state, Side.Shadow, CommandKind.RespondEffect).EffectChoices, c => c.SourceId == LordsShadowCards.Gates);
    }

    // The Captain on the table: after the Free Peoples' action, the Shadow moves a Nazgûl with its second Character die,
    // which then joins the Hunt Box as an Eye.
    private static void CaptainTurnsTheNextCharacterDieIntoAnEye(MatchState state)
    {
        Assert.Contains(LordsShadowCards.Captain, state.Player(Side.Shadow).Table);
        var draw = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Event);
        state = Run(state, Side.FreePeoples, CommandKind.DrawCard, new() { DieId = draw.Id, Deck = "strategy" }, new SeededRandom(1));
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        var die = state.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Character);
        var nazgul = state.Units.First(u => u.Kind == UnitKind.Nazgul && u.Location == "minas-morgul");
        state = Run(state, Side.Shadow, CommandKind.MoveShadowCharacters,
            new() { DieId = die.Id, CompanionMoves = [new() { Ids = [nazgul.Id], Route = ["minas-morgul", "osgiliath"] }] }, new SeededRandom(1));
        var eye = state.Dice.Single(d => d.Id == die.Id);
        Assert.Equal((DieFace.Eye, DieLocation.Hunt, CaptainOfDespair.HuntOrigin), (eye.Face, eye.Location, eye.HuntOrigin));
    }

    private static void BattleTargets(string cardId, MatchState state)
    {
        var battle = BattleRuntime.Active(state)!;
        var attackers = BattleRules.Characters(state, battle.Attacker).Select(c => c.DefinitionId).ToList();
        var defenders = BattleRules.Characters(state, battle.Defender).Select(c => c.DefinitionId).ToList();
        if (cardId == LordsShadowCards.Balrog)
        {
            Assert.Contains(LordsCharacters.Balrog, attackers);
            Assert.False(new NearMoriaBattle().CanPlay(state, battle, Side.Shadow));
        }
        else if (CardCatalog.All[cardId].Side == Side.FreePeoples)
        {
            Assert.Contains(CharacterRules.Saruman, attackers);
            Assert.Contains(CharacterRules.WitchKing, attackers);
            Assert.Contains("base.character.merry", defenders);
            Assert.Equal(state.Fellowship.RegionId, battle.Region);
        }
        else
        {
            Assert.Contains(CharacterRules.WitchKing, attackers);
            Assert.Contains("base.character.legolas", defenders);
            Assert.Contains(BattleRules.Units(state, battle.Attacker), u => u.Nation == "southrons" && u.Kind == UnitKind.Elite);
            Assert.Equal(2, BattleRules.Units(state, battle.Attacker).Count(u => u.Kind == UnitKind.Nazgul));
        }
    }

    private static IEnumerable<HuntTileDefinition> PoolTiles(MatchState state) =>
        state.HuntTiles.Where(t => t.Location == "pool").Select(t => HuntPool.Definitions[t.Id]);

    private static bool SmeagolTile(string id) => HuntPool.Definitions[id].Pool == SmeagolTiles.Pool;

    private static TileState Tile(MatchState state, string id) => state.HuntTiles.Single(t => t.Id == id);

    private static CharacterState Balrog(MatchState state) => state.Characters.Single(c => c.DefinitionId == LordsCharacters.Balrog);

    private static int Gondor(MatchState state, string region) =>
        state.Units.Count(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == region);

    private static int Sauron(MatchState state, string region) =>
        state.Units.Count(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == region);

    internal static List<string> Choices(MatchState state, Side side) => Option(state, side, CommandKind.ChooseEffect).EligibleIds;

    internal static ActionOption Option(MatchState state, Side side, CommandKind kind)
    {
        var option = Views.For(state, side).Options.FirstOrDefault(o => o.Kind == kind && o.Enabled);
        Assert.True(option is not null, $"{kind} is not offered to {side}.");
        return option!;
    }

    private static RingState Ring(MatchState state, string id) => state.Rings.Single(r => r.Id == id);

    internal static TestScenario Scenario(string id)
    {
        var scenario = TestScenarioCatalog.Find(id);
        Assert.True(scenario is not null, $"{id} is not in the catalog.");
        Assert.Equal(ExpansionModules.Lords, scenario!.Modules[0]);
        Assert.StartsWith("Lords · ", scenario.Group);
        return scenario;
    }

    internal static MatchState Create(string id)
    {
        var scenario = Scenario(id);
        var state = scenario.Create("test12-" + id);
        Assert.True(state.Configuration.Lords);
        Assert.Equal(scenario.Modules.Order(), ExpansionModules.Of(state.Configuration).Order());
        Assert.Equal(scenario.StartingSide, state.Pending?.Owner);
        return state;
    }

    internal static MatchState Run(MatchState state, Side side, CommandKind kind, CommandPayload payload, IRandomSource random)
    {
        var command = new GameCommand
        {
            MatchId = state.Id,
            CommandId = "test12-" + kind + "-" + state.Revision,
            ExpectedRevision = state.Revision,
            DecisionId = state.Pending!.Id,
            Kind = kind,
            Payload = payload
        };
        var error = Engine.Validate(state, side, command);
        Assert.True(error is null, $"{kind}: {error}");
        var result = Engine.Execute(state, side, command, random);
        Assert.True(result.Receipt.Accepted, $"{kind}: {result.Receipt.Error}");
        return result.State;
    }

    // Every die rolls a 6 and every draw takes the last tile.
    internal sealed class HighestFace : IRandomSource
    {
        public int Next(int exclusiveMaximum) => exclusiveMaximum - 1;
    }
}
