using Wotr.Contracts;
using Wotr.Domain;

internal static class ExpansionUiMatrix
{
    private static readonly Projection Views = new(new CoreOptions(), new TurnOptions(), new MusterOptions(),
        new MovementOptions(), new EffectOptions(), new FellowshipOptions(), new CharacterOptions(),
        new HuntOptions(), new BattleOptions(), new StrongholdCommands(), new LordsDiceCommands(),
        new KeeperRingCommands(), new LordsSetupCommands(), new CouncilCommands(), new SovereignCommands(),
        new CorruptionCommands(), new KingsDiceCommands(), new DismayCommands());

    public static Dictionary<string, PlayerView> Create(IRandomSource random)
    {
        var result = new Dictionary<string, PlayerView>();
        for (var flags = 0; flags < 512; flags++)
        {
            var configuration = Configuration(flags);
            if (!ConfigurationRules.IsValid(configuration))
                continue;
            var state = ExpansionUiFixture.Actions(random, configuration);
            foreach (var side in Enum.GetValues<Side>())
                result.Add($"{flags}-{side}", Views.For(state, side));
        }
        if (result.Count != 186)
            throw new InvalidOperationException("UI matrix must contain both perspectives of all 93 configurations.");
        return result;
    }

    private static MatchConfiguration Configuration(int flags) => new()
    {
        Lords = (flags & 1) != 0,
        Kings = (flags & 2) != 0,
        Fate = (flags & 4) != 0,
        Council = (flags & 8) != 0,
        Dismay = (flags & 16) != 0,
        VisibleCorruption = (flags & 32) != 0,
        SequentialCorruption = (flags & 64) != 0,
        RemovingRulerTokens = (flags & 128) != 0,
        NewCities = (flags & 256) != 0
    };

    public static Dictionary<string, PlayerView> Resources(IRandomSource random)
    {
        var state = ResourceState(random);
        var result = new Dictionary<string, PlayerView>();
        foreach (var side in Enum.GetValues<Side>())
            result.Add("unrolled-" + side, Views.For(state, side));
        foreach (var token in state.Kings!.Tokens)
            token.Location = "dismay";
        foreach (var side in Enum.GetValues<Side>())
            result.Add("dismay-" + side, Views.For(state, side));
        return result;
    }

    public static MatchState ResourceState(IRandomSource random)
    {
        var state = ExpansionUiFixture.Actions(random, new() { Lords = true, Kings = true, Council = true });
        LordsCompanionSetup.Choose(state, [LordsCharacters.Gandalf], CharacterRules.Grey);
        foreach (var (id, region) in new[] { ("lords.character.elrond", "rivendell"), ("lords.character.galadriel", "lorien"),
            ("lords.character.balrog", "moria"), ("lords.character.gothmog", "minas-morgul"),
            ("base.character.aragorn", "minas-tirith"), ("base.character.saruman", "orthanc"),
            ("base.character.mouth-of-sauron", "barad-dur"), ("base.character.witch-king", "barad-dur"),
            (KingsChieftains.Serpent, "near-harad") })
            ExpansionUiFixture.Put(state, id, region);
        foreach (var sovereign in state.Kings!.Sovereigns)
            SovereignLifecycle.Awaken(state, sovereign, SovereignCatalog.All[sovereign.Id].Home);
        state.Turn++;
        var fellowshipDie = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Source == "standard");
        fellowshipDie.Location = DieLocation.Hunt;
        fellowshipDie.HuntOrigin = DiceRules.FellowshipMove;
        DiceRules.Recover(state);
        state.Lords!.Council!.GrantedTokens = [CouncilTokens.Politics, CouncilTokens.Travel];
        return state;
    }
}
