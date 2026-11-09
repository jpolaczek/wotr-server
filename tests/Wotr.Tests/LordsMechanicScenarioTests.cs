using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;
using static Wotr.Tests.LordsCardEndToEndScenarioTests;

namespace Wotr.Tests;

// TEST-12: each Lords character, Ring, Sméagol and special-die scenario sits in the Lords tab and offers its mechanic as
// a legal option of its first decision; committing that option (and the moves that lead to it) runs through the engine
// and the mechanic acts: a Keeper enters, Nenya replaces an Eye, a Minion enters or changes version, the Chief reacts,
// the Balrog's die, Fire and Shadow and his deactivation act, Sméagol joins and leaves, a Remove die leaves.
public class LordsMechanicScenarioTests
{
    private const string Elrond = LordsCharacters.Elrond;

    public static TheoryData<string> Ids => new(LordsCharacterScenarios.All().Concat(LordsSmeagolScenarios.All()).Select(s => s.Id));

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheScenarioIsInTheLordsTabAndRepeatable(string id)
    {
        var scenario = Scenario(id);
        Assert.Equal([ExpansionModules.Lords], scenario.Modules);
        Assert.Equal(StateCodec.Write(Create(id)), StateCodec.Write(Create(id)));
        Assert.Single(TestScenarioCatalog.All, s => s.Id == id);
    }

    [Fact]
    public void AKeeperEntersAtHomeWithAMusterDie()
    {
        var state = Create("lords-fp-keeper-entry");
        var entries = Option(state, Side.FreePeoples, CommandKind.EnterCharacter).CharacterActions.Select(a => a.CharacterId).ToList();
        Assert.Contains(Elrond, entries);
        Assert.Contains(LordsCharacters.Galadriel, entries);
        state = Run(state, Side.FreePeoples, CommandKind.EnterCharacter,
            new() { DieId = Die(state, Side.FreePeoples, DieFace.Muster).Id, CharacterId = Elrond, RegionId = "rivendell" }, new SeededRandom(1));
        Assert.Equal("rivendell", CharacterRules.InPlay(state, Elrond)!.Location);
    }

    [Fact]
    public void NenyaRemovesTheDrawnEyeAndDrawsAnotherTile()
    {
        var state = Create("lords-fp-nenya-ring");
        state = Move(state);
        Assert.Equal((DecisionKind.Reaction, Side.FreePeoples, "hunt.replace"), (state.Pending!.Kind, state.Pending.Owner, state.Pending.Window));
        var eye = HuntRuntime.Active(state)!.TileId;
        Assert.Contains(Option(state, Side.FreePeoples, CommandKind.RespondEffect).EffectChoices, c => c.Id == NenyaReaction.Id || c.SourceId == NenyaReaction.Id);
        state = Run(state, Side.FreePeoples, CommandKind.RespondEffect, new() { ReactionId = NenyaReaction.Id }, new HighestFace());
        Assert.Equal("removed", state.HuntTiles.Single(t => t.Id == eye).Location);
        Assert.Equal(Side.Shadow, state.Rings.Single(r => r.Id == LordsDiceSetup.NenyaRing).Owner);
    }

    [Fact]
    public void GaladrielRecruitsInsideTheBesiegedLorien()
    {
        var state = Create("lords-fp-galadriel-siege-muster");
        Assert.True(BoardRules.Besieged(state, "lorien", Side.FreePeoples));
        Option(state, Side.FreePeoples, CommandKind.Recruit);
        var elite = state.Units.First(u => u.Nation == "elves" && u.Kind == UnitKind.Elite && u.Location == "reserve");
        state = Run(state, Side.FreePeoples, CommandKind.Recruit, new()
        {
            DieId = Die(state, Side.FreePeoples, DieFace.Muster).Id,
            Placements = [new() { UnitId = elite.Id, RegionId = "lorien" }]
        }, new SeededRandom(1));
        var recruited = state.Units.Single(u => u.Id == elite.Id);
        Assert.Equal(("lorien", true), (recruited.Location, recruited.Garrison));
    }

    [Fact]
    public void ElrondsHouseholdMakesTheElvenElitesLeaders()
    {
        var state = Create("lords-fp-elrond-household");
        var defender = BattleRuntime.Active(state)!.Defender;
        Assert.Equal("rivendell", defender.Region);
        Assert.Contains(CombatCardFigures.Leaders(state, defender), u => u.Nation == "elves" && u.Kind == UnitKind.Elite);
        Run(state, Side.FreePeoples, CommandKind.CommitCombatCard, new() { CardId = "" }, new SeededRandom(1));
    }

    [Fact]
    public void TheKeeperOfNaryaIsAPrivateSetupChoiceThatBringsHisDie()
    {
        var state = Create("lords-fp-keeper-gandalf-setup");
        Assert.Equal(DecisionKind.CouncilVersions, state.Pending!.Kind);
        Assert.Contains(Option(state, Side.FreePeoples, CommandKind.ChooseCompanionVersions).CompanionVersions,
            v => v.AlternateId == LordsCharacters.Gandalf);
        state = Run(state, Side.FreePeoples, CommandKind.ChooseCompanionVersions,
            new() { GuideId = CharacterRules.Grey, Ids = [LordsCharacters.Gandalf] }, new SeededRandom(1));
        Assert.Equal(LordsCharacters.Gandalf, CharacterCatalog.Companion(state, CharacterRules.Grey).DefinitionId);
        Assert.NotEqual(DieLocation.Removed, state.Dice.Single(d => d.Id == LordsDiceCatalog.Narya).Location);
    }

    [Fact]
    public void TheKeeperBecomesGandalfTheWhiteOnHisFigure()
    {
        var state = Create("lords-fp-keeper-white");
        Assert.Contains(Option(state, Side.FreePeoples, CommandKind.EnterCharacter).CharacterActions, a => a.CharacterId == CharacterRules.White);
        var plan = CharacterEntry.Candidates(state, Side.FreePeoples).First(p => p.DefinitionId == CharacterRules.White);
        state = Run(state, Side.FreePeoples, CommandKind.EnterCharacter, new()
        {
            DieId = Die(state, Side.FreePeoples, DieFace.WillOfTheWest).Id, CharacterId = CharacterRules.White, RegionId = plan.Region
        }, new SeededRandom(1));
        Assert.Equal(CharacterRules.White, state.Characters.Single(c => c.Id == CharacterRules.Grey).DefinitionId);
    }

    [Fact]
    public void GothmogTheBlackNumenoreanAndTheChiefCanEnter()
    {
        var state = Create("lords-sh-minion-entries");
        var entries = Option(state, Side.Shadow, CommandKind.EnterCharacter).CharacterActions.Select(a => a.CharacterId).ToList();
        Assert.Contains(LordsCharacters.Gothmog, entries);
        Assert.Contains(LordsCharacters.Mouth, entries);
        Assert.Contains(LordsCharacters.WitchKing, entries);
        state = Run(state, Side.Shadow, CommandKind.EnterCharacter,
            new() { DieId = Die(state, Side.Shadow, DieFace.Muster).Id, CharacterId = LordsCharacters.Gothmog, RegionId = "minas-morgul" },
            new SeededRandom(1));
        Assert.Equal("minas-morgul", CharacterRules.InPlay(state, LordsCharacters.Gothmog)!.Location);
    }

    [Fact]
    public void TheBlackCaptainBecomesTheChiefInPlace()
    {
        var state = Create("lords-sh-witch-king-switch");
        Assert.Contains(Option(state, Side.Shadow, CommandKind.EnterCharacter).CharacterActions, a => a.CharacterId == LordsCharacters.WitchKing);
        state = Run(state, Side.Shadow, CommandKind.EnterCharacter,
            new() { DieId = Die(state, Side.Shadow, DieFace.Muster).Id, CharacterId = LordsCharacters.WitchKing, RegionId = "minas-morgul" },
            new SeededRandom(1));
        var witchKing = state.Characters.Single(c => c.Id == CharacterRules.WitchKing);
        Assert.Equal((LordsCharacters.WitchKing, "minas-morgul"), (witchKing.DefinitionId, witchKing.Location));
    }

    [Fact]
    public void SpearOfTerrorFollowsACharacterEventAtTheFellowship()
    {
        var state = Create("lords-sh-spear-of-terror");
        state = Run(state, Side.Shadow, CommandKind.PlayEvent, new()
        {
            CardId = ShadowCharacterCards03.Lidless, DieId = Die(state, Side.Shadow, DieFace.Character).Id, CardCost = DieFace.Character
        }, new SeededRandom(1));
        Assert.Equal((DecisionKind.Reaction, Side.Shadow), (state.Pending!.Kind, state.Pending.Owner));
        var hand = state.Player(Side.Shadow).Hand.Count;
        state = Run(state, Side.Shadow, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Spear }, new SeededRandom(1));
        Assert.Contains(state.Events, e => e.Text.StartsWith("Spear of Terror", StringComparison.Ordinal));
        Assert.True(state.Player(Side.Shadow).Hand.Count >= hand);
    }

    [Fact]
    public void TheChiefMayFollowTheDeclaredFellowship()
    {
        var state = Create("lords-sh-he-sees-he-knows");
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        state = Run(state, Side.FreePeoples, CommandKind.DeclareFellowship, new() { Route = ["hollin"] }, new SeededRandom(1));
        Assert.Equal((DecisionKind.Reaction, Side.Shadow, "fellowship.declared"), (state.Pending!.Kind, state.Pending.Owner, state.Pending.Window));
        state = Run(state, Side.Shadow, CommandKind.RespondEffect, new() { ReactionId = ChiefReactions.Follow }, new SeededRandom(1));
        Assert.Equal("hollin", state.Characters.Single(c => c.Id == CharacterRules.WitchKing).Location);
    }

    [Fact]
    public void GreatSorceryMovesTheBlackNumenoreansArmyWithAMusterDie()
    {
        var state = Create("lords-sh-great-sorcery");
        var mouth = state.Characters.Single(c => c.DefinitionId == LordsCharacters.Mouth);
        var cost = Option(state, Side.Shadow, CommandKind.MoveArmies).MovementCosts.First(c => c.AbilityId == LordsCharacters.GreatSorcery);
        Assert.Equal(mouth.Id, cost.RequiredCharacterId);
        var units = state.Units.Where(u => u.Location == mouth.Location && u.Nation == "sauron").Select(u => u.Id).ToList();
        state = Run(state, Side.Shadow, CommandKind.MoveArmies, new()
        {
            DieId = cost.DieId, AbilityId = LordsCharacters.GreatSorcery, MovementCost = DieFace.Character,
            Moves = [new() { From = mouth.Location, To = "dagorlad", UnitIds = units, CharacterIds = [mouth.Id] }]
        }, new SeededRandom(1));
        Assert.Equal("dagorlad", state.Characters.Single(c => c.Id == mouth.Id).Location);
        Assert.Equal(state.Turn, state.Characters.Single(c => c.Id == mouth.Id).LastArmyConversionTurn);
    }

    [Fact]
    public void TheActiveBalrogsDieMovesHimAlone()
    {
        var state = Create("lords-sh-use-balrog-active");
        var balrog = CharacterRules.InPlay(state, LordsCharacters.Balrog)!;
        Assert.Contains(Option(state, Side.Shadow, CommandKind.AttackArmy).MovementCosts, c => c.AbilityId == LordsArmyCosts.UseBalrog);
        state = Run(state, Side.Shadow, CommandKind.MoveShadowCharacters, new()
        {
            DieId = LordsDiceCatalog.Balrog, AbilityId = LordsArmyCosts.UseBalrog, MovementCost = DieFace.Character,
            CompanionMoves = [new() { Ids = [balrog.Id], Route = ["moria", "hollin"] }]
        }, new SeededRandom(1));
        Assert.Equal("hollin", CharacterRules.InPlay(state, LordsCharacters.Balrog)!.Location);
    }

    [Fact]
    public void FireAndShadowHuntsTheFellowshipDeclaredInTheBalrogsRegion()
    {
        var state = Create("lords-sh-fire-and-shadow");
        Assert.Equal(DecisionKind.FellowshipPhase, state.Pending!.Kind);
        state = Run(state, Side.FreePeoples, CommandKind.DeclareFellowship, new() { Route = ["hollin", "moria"] }, new SeededRandom(1));
        Assert.True(HuntRuntime.Active(state)!.FireAndShadow);
        Assert.Equal(LordsCharacters.Balrog, HuntRuntime.Active(state)!.SourceId);
    }

    [Fact]
    public void GandalfDeactivatesAndSoEliminatesTheBalrogOutsideMoria()
    {
        var state = Create("lords-fp-balrog-deactivate");
        var balrog = CharacterRules.InPlay(state, LordsCharacters.Balrog)!;
        var action = Option(state, Side.FreePeoples, CommandKind.UseCharacterAbility).CharacterActions
            .Single(a => a.AbilityId == LordsCharacterAbilities.DeactivateBalrog);
        var die = Die(state, Side.FreePeoples, DieFace.Character);
        Assert.Contains(die.Id, action.Dice);
        state = Run(state, Side.FreePeoples, CommandKind.UseCharacterAbility, new()
        {
            DieId = die.Id, CharacterId = balrog.Id, AbilityId = LordsCharacterAbilities.DeactivateBalrog, Ids = [balrog.Location]
        }, new SeededRandom(1));
        Assert.Null(CharacterRules.InPlay(state, LordsCharacters.Balrog));
    }

    [Fact]
    public void AChosenRemoveDieLeavesAtTheNextRecovery()
    {
        var state = Create("lords-dice-removed-at-recovery");
        Assert.True(state.Dice.Single(d => d.Id == LordsDiceCatalog.Vilya).Special!.RemoveSymbol);
        state = Run(state, Side.FreePeoples, CommandKind.DrawCard,
            new() { DieId = Die(state, Side.FreePeoples, DieFace.Event).Id, Deck = "character" }, new SeededRandom(1));
        if (state.Pending?.Kind == DecisionKind.KeepActionDie)
            state = Run(state, Side.FreePeoples, CommandKind.DeclineVilya, new(), new SeededRandom(1));
        var vilya = state.Dice.Single(d => d.Id == LordsDiceCatalog.Vilya);
        Assert.Equal(2, state.Turn);
        Assert.Equal(DieLocation.Removed, vilya.Location);
        Assert.True(vilya.Special!.PermanentlyRemoved);
    }

    [Fact]
    public void SmeagolsTileBringsHimInAsGuide()
    {
        var state = Move(Create("lords-smeagol-joins"));
        Assert.True(SmeagolRules.Present(state));
        Assert.Equal(LordsCharacters.Smeagol, state.Fellowship.Guide);
        Assert.Contains(state.HuntTiles, t => t.Location == "removed" && HuntPool.Definitions[t.Id].Pool == SmeagolTiles.Pool);
    }

    [Fact]
    public void SmeagolTakenAsTheCasualtyAbsorbsTheDamageAndEntersTheSpecialCard()
    {
        var state = Move(Create("lords-smeagol-casualty"));
        Assert.Equal(DecisionKind.HuntDamage, state.Pending!.Kind);
        state = Run(state, Side.FreePeoples, CommandKind.ResolveHuntDamage, new() { HuntResponse = HuntResponse.Guide }, new SeededRandom(1));
        Assert.False(SmeagolRules.Present(state));
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Equal(0, HuntRuntime.Active(state)?.Damage ?? 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SmeagolLeavesWithTheGroupOrAsTheLastCompanion(bool withHim)
    {
        var state = Create("lords-smeagol-departure");
        var ids = state.Fellowship.Companions.Where(id => withHim || id != LordsCharacters.Smeagol).ToList();
        state = Run(state, Side.FreePeoples, CommandKind.SeparateCompanions,
            new() { DieId = Die(state, Side.FreePeoples, DieFace.Character).Id, Ids = ids, Route = ["hollin"] }, new SeededRandom(1));
        Assert.Equal(CharacterRules.Gollum, state.Fellowship.Guide);
        Assert.Equal(withHim, state.Player(Side.Shadow).Table.Contains(SmeagolRules.WeShallGetIt));
    }

    [Fact]
    public void DeclaringInLorienDiscardsSmeagol()
    {
        var state = Create("lords-smeagol-declared");
        state = Run(state, Side.FreePeoples, CommandKind.DeclareFellowship, new() { Route = ["dimrill-dale", "lorien"] }, new SeededRandom(1));
        Assert.False(SmeagolRules.Present(state));
        Assert.Contains(SmeagolRules.WeShallGetIt, state.Player(Side.Shadow).Table);
        Assert.Equal(DecisionKind.Guide, state.Pending!.Kind);
    }

    // The Free Peoples move the Fellowship with their Character die and the Hunt hits, drawing the pool's last tile.
    private static MatchState Move(MatchState state) =>
        Run(state, Side.FreePeoples, CommandKind.MoveFellowship, new() { DieId = Die(state, Side.FreePeoples, DieFace.Character).Id }, new HighestFace());

    internal static DieState Die(MatchState state, Side side, DieFace face) =>
        state.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available && d.Face == face);
}
