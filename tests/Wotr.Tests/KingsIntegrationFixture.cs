using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

namespace Wotr.Tests;

internal static class KingsIntegrationFixture
{
    public static MatchState Actions(MatchConfiguration? configuration = null)
    {
        var config = ConfigurationRules.Normalize(configuration ?? new() { Lords = true, Kings = true, Council = true });
        var cards = CardCatalog.ConfiguredDeck(config).Count();
        var state = MatchFactory.Create("kings-integration", new SequenceRandom(new int[cards - 4]), config);
        if (config.Lords)
        {
            LordsDiceSetup.Initialize(state);
            SmeagolTiles.Initialize(state);
            if (config.Council)
                state.Lords!.Council = new() { HomelandsChosen = true, GrantedTokens = [.. CouncilTokens.All] };
        }
        KingsSetup.Initialize(state);
        state.Turn = 2;
        state.Phase = GamePhase.ActionResolution;
        state.Fellowship.Guide = CharacterRules.Strider;
        foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        {
            die.Location = DieLocation.Available;
            die.Face = DieFace.Muster;
        }
        TurnFlow.SelectNext(state, Side.Shadow);
        return state;
    }

    public static DieState SpecialEye(MatchState state, string id)
    {
        var die = LordsFixture.Die(state, id);
        die.Face = DieFace.Eye;
        die.Location = DieLocation.Hunt;
        die.Special!.Chosen = true;
        die.Special.RemoveSymbol = true;
        die.HuntOrigin = die.Owner == Side.FreePeoples ? LordsDiceCatalog.KeeperEye : LordsDiceCatalog.LesserEye;
        return die;
    }

    public static MatchState DismayWindow(bool lore = false)
    {
        var state = Actions();
        state.Phase = GamePhase.ActionRoll;
        foreach (var side in Enum.GetValues<Side>())
        {
            var die = KingsDice.Ruler(state, side)!;
            die.Location = DieLocation.Available;
            die.Face = DieFace.Character;
        }
        state.Kings!.Tokens.Add(new() { SovereignId = SovereignCatalog.Brand, Location = KingsDismay.Location });
        state.Kings.Tokens.Add(new() { SovereignId = SovereignCatalog.Theoden, Location = KingsDismay.Location });
        if (lore)
        {
            state.Player(Side.Shadow).Table.Add(LordsShadowCards.Lore);
            SpecialEye(state, LordsDiceCatalog.Nenya);
        }
        TurnFlow.BeginActions(state);
        return state;
    }

    public static CommandPayload Dismay(MatchState state, Side side = Side.Shadow) => new()
    {
        DieId = KingsDice.Ruler(state, side)!.Id,
        Ids = [KingsRulerTokens.Id(KingsDismay.Tokens(state).First().SovereignId)]
    };

    public static MatchState Sequential(bool visible = false, int eyes = 4)
    {
        var state = Actions(new() { Kings = true, Lords = true, SequentialCorruption = true, VisibleCorruption = visible });
        KingsFixture.Eyes(state, eyes);
        KingsFixture.Pool(state, HuntTests.Three);
        return KingsFixture.Do(state, CommandKind.AttemptCorruption, new()
        {
            DieId = CorruptionRules.Eyes(state).First().Id, CharacterId = SovereignCatalog.Thranduil
        }, 0);
    }

    public static MatchState Draw(MatchState state, string tile)
    {
        state.HuntTiles.Single(t => t.Id == tile).Location = "pool";
        return KingsFixture.Do(state, CommandKind.DrawCorruptionTile, new(), 0);
    }
}
