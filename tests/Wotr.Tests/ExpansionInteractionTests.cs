using Wotr.Contracts;
using Wotr.Domain;
using Xunit;

namespace Wotr.Tests;

public class ExpansionInteractionTests
{
    internal static MatchState Actions(bool cities = false, bool visible = false, bool sequential = false, bool removing = false) =>
        ExpansionFixture.CompleteSetup(ExpansionFixture.Start(new()
        {
            Lords = true, Kings = true, Fate = true, Council = true, NewCities = cities,
            VisibleCorruption = visible, SequentialCorruption = sequential, RemovingRulerTokens = removing
        }));

    internal static MatchState Book(bool cities) => FateCardsFixture.Play(Actions(cities), FateCards.Book);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FateCouncilGimliEnablesBookAndItsGrantedTokenPaysAKingsChieftainWithoutDainOrBrand(bool cities)
    {
        var state = Book(cities);
        var gimli = state.Characters.Single(character => character.Id == FateGimli.Figure);
        Assert.Equal(FateGimli.Alternate, gimli.DefinitionId);
        Assert.Equal("iron-hills", gimli.Location);
        Assert.Empty(gimli.NationIcon);
        state = KingsFixture.Do(ExpansionFixture.Reload(state), CommandKind.MoveCompanions);
        Assert.Equal(0, state.Nations.Single(nation => nation.Id == "dwarves").StepsToWar);
        // Iron Hills is the only legal region and is resolved automatically; the unit remains a choice.
        var elite = FateCardsFixture.Option(state).EligibleIds.Single(id => state.Units.Single(unit => unit.Id == id).Kind == UnitKind.Elite);
        state = FateCardsFixture.Choose(ExpansionFixture.Reload(state), elite);
        Assert.Equal("iron-hills", state.Units.Single(unit => unit.Id == elite).Location);
        Assert.Equal(Side.Shadow, state.Pending!.Owner);
        Assert.False(KingsChieftains.Prerequisite(state, KingsChieftains.Serpent));
        var payload = KingsCouncilIntegrationTests.Payload(state);
        state.Dice.Single(die => die.Id == payload.DieId).Face = DieFace.Muster;
        state = KingsFixture.Do(ExpansionFixture.Reload(state), CommandKind.EnterCharacter, payload);
        Assert.Equal("near-harad", state.Characters.Single(character => character.Id == KingsChieftains.Serpent).Location);
        Assert.Equal(new[] { CouncilTokens.Politics }, state.Lords!.Council!.SpentTokens);
        Assert.Equal(DieLocation.Spent, state.Dice.Single(die => die.Id == payload.DieId).Location);
        Assert.Equal(DieLocation.Removed, KingsDice.Ruler(state, Side.Shadow)!.Location);
        Assert.DoesNotContain(state.Kings!.Sovereigns, sovereign => sovereign.Id is SovereignCatalog.Dain or SovereignCatalog.Brand);
        DiceRules.Recover(state);
        Assert.Equal(DieLocation.Unrolled, KingsDice.Ruler(state, Side.Shadow)!.Location);
        ExpansionFixture.Reload(state);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FullCompositionCorruptionSkipsSmeagolExcludesCaptainAndPreservesSpecialEyeCost(bool visible, bool sequential)
    {
        var state = Corruption(visible, sequential);
        Assert.Equal(2, CorruptionRuntime.Current(state)!.DrawLimit);
        Assert.Equal(CorruptionStage.Drawing, CorruptionRuntime.Current(state)!.Stage);
        Assert.Empty(CorruptionRuntime.Current(state)!.Drawn);
        Assert.DoesNotContain(state.Characters, character => character.DefinitionId == LordsCharacters.Smeagol);
        state = KingsIntegrationFixture.Draw(ExpansionFixture.Reload(state), HuntTests.Three);
        // UI-141: the pool composition is public, so both worlds hold the same tiles: One waits in the pool.
        var seen = StateCodec.Copy(state);
        seen.HuntTiles.Single(tile => tile.Id == KingsCorruptionTests.One).Location = "pool";
        var fp = ExpansionFixture.Json(seen, Side.FreePeoples);
        var changed = StateCodec.Copy(seen);
        CorruptionRuntime.Current(changed)!.Drawn = [KingsCorruptionTests.One];
        changed.Pending!.PrivateChoices = sequential ? [KingsCorruptionTests.One] : [];
        changed.HuntTiles.Single(tile => tile.Id == HuntTests.Three).Location = "pool";
        changed.HuntTiles.Single(tile => tile.Id == KingsCorruptionTests.One).Location = "corruption-draw";
        Assert.Equal(fp, ExpansionFixture.Json(changed, Side.FreePeoples));
        state = KingsIntegrationFixture.Draw(ExpansionFixture.Reload(state), KingsCorruptionTests.One);
        if (!sequential)
            state = KingsFixture.Do(state, CommandKind.ChooseCorruptionTile, new() { Ids = [KingsCorruptionTests.One] });
        var sovereign = KingsFixture.Sovereign(state, SovereignCatalog.Thranduil);
        Assert.Equal(new[] { KingsCorruptionTests.One }, sovereign.Tiles);
        var view = ExpansionFixture.Views.For(state, Side.FreePeoples).Sovereigns.Single(item => item.Id == sovereign.Id);
        Assert.Equal(visible ? KingsCorruptionTests.One : "", Assert.Single(view.Tiles).TileId);
        Assert.Equal(Side.FreePeoples, LordsFixture.Die(state, LordsDiceCatalog.Nenya).SpentWith);
        Assert.Equal(DieLocation.Spent, LordsFixture.Die(state, LordsDiceCatalog.Nenya).Location);
        Assert.Contains(state.Dice, die => die.HuntOrigin == "captain-of-despair" && die.Location == DieLocation.Hunt);
        Assert.Equal("pool", state.HuntTiles.Single(tile => tile.Id == "lords.hunt.smeagol.1").Location);
        Assert.Empty(state.Resolution.Frames);
        ExpansionFixture.Reload(state);
    }

    internal static MatchState Corruption(bool visible, bool sequential)
    {
        var state = Actions(visible: visible, sequential: sequential);
        state = KingsFixture.Do(state, CommandKind.DiscardDie, new() { DieId = KingsFixture.Die(state).Id });
        KingsFixture.Eyes(state, 2);
        LordsCharacterFixture.Put(state, LordsDiceCatalog.Definitions[LordsDiceCatalog.Nenya].CharacterId, "lorien");
        KingsIntegrationFixture.SpecialEye(state, LordsDiceCatalog.Nenya);
        EventTests.GiveCard(state, Side.Shadow, LordsShadowCards.Captain);
        state.Player(Side.Shadow).Hand.Remove(LordsShadowCards.Captain);
        state.Player(Side.Shadow).Table.Add(LordsShadowCards.Captain);
        var captain = KingsFixture.Die(state, Side.Shadow);
        CaptainOfDespair.RecordCost(state, captain);
        Assert.True(CaptainOfDespair.CompleteCost(state, captain));
        Assert.Equal(3, CorruptionRules.Eyes(state).Count());
        KingsFixture.Pool(state, "lords.hunt.smeagol.1");
        return KingsFixture.Do(state, CommandKind.AttemptCorruption,
            new() { CharacterId = SovereignCatalog.Thranduil, DieId = LordsDiceCatalog.Nenya }, 0);
    }
}
