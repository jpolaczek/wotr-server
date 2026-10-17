namespace Wotr.Domain;

// The complete set of command handlers and option providers. The server and the test-scenario checks
// build the engine and the projection from here, so a scenario is validated by exactly what players use.
public static class GameRuntime
{
    public static GameEngine Engine(EffectCatalog effects) => new(
        new DeclineReactionHandler(),
        new TurnCommands(),
        new MusterCommands(),
        new MovementCommands(),
        new EffectCommands(effects),
        new FellowshipCommands(),
        new CharacterCommands(),
        new HuntCommands(effects),
        new BattleCommands(effects),
        new StrongholdCommands(),
        new LordsDiceCommands(),
        new KeeperRingCommands(),
        new LordsSetupCommands(),
        new CouncilCommands(),
        new SovereignCommands(),
        new CorruptionCommands(),
        new KingsDiceCommands(),
        new DismayCommands());

    public static Projection Projection(EffectCatalog effects) => new(
        new CoreOptions(),
        new TurnOptions(),
        new MusterOptions(),
        new MovementOptions(),
        new EffectOptions(effects),
        new FellowshipOptions(),
        new CharacterOptions(),
        new HuntOptions(),
        new BattleOptions(effects),
        new StrongholdCommands(),
        new LordsDiceCommands(),
        new KeeperRingCommands(),
        new LordsSetupCommands(),
        new CouncilCommands(),
        new SovereignCommands(),
        new CorruptionCommands(),
        new KingsDiceCommands(),
        new DismayCommands());
}
