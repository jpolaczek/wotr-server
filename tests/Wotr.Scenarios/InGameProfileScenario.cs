using Wotr.Contracts;
using Wotr.Domain;

// Prepared presentation stress position. Accounts and internal state never enter the export.
internal static class InGameProfileScenario
{
    private static readonly Projection Views = new(new CoreOptions(), new TurnOptions(), new MusterOptions(),
        new MovementOptions(), new EffectOptions(), new FellowshipOptions(), new CharacterOptions(),
        new HuntOptions(), new BattleOptions(), new StrongholdCommands(), new LordsDiceCommands(),
        new KeeperRingCommands(), new LordsSetupCommands(), new CouncilCommands(), new SovereignCommands(),
        new CorruptionCommands(), new KingsDiceCommands(), new DismayCommands());

    public static MatchState Dense(IRandomSource random)
    {
        var state = ExpansionUiMatrix.ResourceState(random);
        // Keep every recovered physical die, ring and the five real sovereigns from the existing resource corpus.
        // These inspections do not resolve the next roll or fabricate an action result.
        Protect(state, FreePeoplesStrategyCards01.PowerTooGreat, "lorien", "rivendell", "grey-havens");
        Protect(state, FreePeoplesStrategyCards01.TomBombadil, "old-forest", "the-shire", "buckland");
        Table(state, FreePeoplesCharacterCards01.Mithril, Side.FreePeoples);
        Lasting(state, FreePeoplesCharacterCards02.Horn, Side.FreePeoples,
            EffectExpiry.FellowshipCompanionsAbsent, "base.character.boromir");
        Lasting(state, ShadowCharacterCards03.Crebain, Side.Shadow, EffectExpiry.FellowshipDeclaresSanctuary);
        Lasting(state, ShadowCharacterCards03.Worn, Side.Shadow, EffectExpiry.FellowshipDeclaresSanctuary);
        return state;
    }

    public static Dictionary<string, PlayerView> ProfileViews(MatchState dense) => new()
    {
        ["effects"] = Views.For(dense, Side.FreePeoples),
        ["resources-fp"] = Views.For(dense, Side.FreePeoples),
        ["resources-sh"] = Dismay(dense),
        ["sovereigns"] = Views.For(dense, Side.FreePeoples),
        ["victory"] = Victory(dense),
        ["help"] = Views.For(dense, Side.Shadow),
        ["dense-fp"] = Views.For(dense, Side.FreePeoples),
        ["dense-sh"] = Views.For(dense, Side.Shadow)
    };

    private static PlayerView Dismay(MatchState dense)
    {
        var state = StateCodec.Copy(dense);
        foreach (var token in state.Kings!.Tokens)
            token.Location = "dismay";
        return Views.For(state, Side.Shadow);
    }

    private static PlayerView Victory(MatchState dense)
    {
        var state = StateCodec.Copy(dense);
        // A separate prepared conquest position; the dense resource input remains unchanged.
        state.Characters.Single(character => character.Id == SovereignCatalog.Theoden).Location = "folde";
        Capture(state, Side.FreePeoples, "angmar", "far-harad", "mount-gundabad", "umbar");
        Capture(state, Side.Shadow, "dol-amroth", "helms-deep", "pelargir", "edoras");
        return Views.For(state, Side.FreePeoples);
    }

    private static void Capture(MatchState state, Side owner, params string[] regions)
    {
        foreach (var unit in state.Units.Where(unit => regions.Contains(unit.Location)))
            BoardRules.ReturnToReserve(unit);
        foreach (var region in regions)
            state.Control[region] = owner;
    }

    private static void Protect(MatchState state, string source, params string[] regions)
    {
        Table(state, source, Side.FreePeoples);
        foreach (var region in regions)
            state.ActiveEffects.Add(new()
            {
                Id = $"{state.Id}:{source}:{region}", SourceId = source, Owner = Side.FreePeoples,
                Modifier = EffectModifier.PreventShadowArmyEntry, TargetId = region,
                Duration = EffectDuration.UntilRemoved, Turn = state.Turn
            });
    }

    private static void Lasting(MatchState state, string source, Side side, EffectExpiry expiry, params string[] companions)
    {
        Table(state, source, side);
        state.ActiveEffects.Add(new()
        {
            Id = $"{state.Id}:{source}", SourceId = source, Owner = side,
            Duration = EffectDuration.UntilRemoved, ExpiresWhen = expiry,
            RequiredCompanions = companions.ToList(), Turn = state.Turn
        });
    }

    private static void Table(MatchState state, string source, Side side)
    {
        foreach (var player in state.Players)
        {
            player.Hand.Remove(source);
            player.Discard.Remove(source);
            player.Table.Remove(source);
            foreach (var deck in player.Decks.Values)
                deck.Remove(source);
        }
        state.Player(side).Table.Add(source);
    }
}
