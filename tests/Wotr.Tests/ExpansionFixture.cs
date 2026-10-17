using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

internal static class ExpansionFixture
{
    public static Projection Views { get; } = new(new CoreOptions(), new TurnOptions(), new MusterOptions(),
        new MovementOptions(), new EffectOptions(), new FellowshipOptions(), new CharacterOptions(),
        new HuntOptions(), new BattleOptions(), new StrongholdCommands(), new LordsDiceCommands(),
        new KeeperRingCommands(), new LordsSetupCommands(), new CouncilCommands(), new SovereignCommands(),
        new CorruptionCommands(), new KingsDiceCommands(), new DismayCommands());

    public static MatchState Start(MatchConfiguration configuration)
    {
        var state = MatchFactory.Create("expansion-matrix", new BaseCorpusFixture.ZeroRandom(), configuration);
        TurnFlow.Start(state);
        return state;
    }

    public static CommandPayload Versions(MatchState state) => new()
    {
        GuideId = CharacterRules.Strider,
        Ids = state.Configuration.Council
            ? [CharacterRules.Grey, state.Configuration.Fate ? FateGimli.Alternate : "lords.character.gimli"]
            : [CharacterRules.Grey]
    };

    public static (CommandKind Kind, CommandPayload Payload) NextSetupChoice(MatchState state) => state.Pending!.Kind switch
    {
        DecisionKind.CouncilVersions => (CommandKind.ChooseCompanionVersions, Versions(state)),
        DecisionKind.CouncilHomelands => (CommandKind.ChooseCouncilHomelands, new()
        {
            Placements = [new() { UnitId = FateGimli.Figure, RegionId = state.Configuration.Fate ? "iron-hills" : "erebor" }]
        }),
        DecisionKind.CouncilToken => (CommandKind.ChooseCouncilToken, new() { Ids = [CouncilTokens.Politics] }),
        DecisionKind.FellowshipPhase => (CommandKind.ContinueFellowship, new()),
        DecisionKind.AllocateHunt => (CommandKind.AllocateHunt, new()),
        _ => throw new InvalidOperationException($"Unexpected setup decision: {state.Pending.Kind}.")
    };

    public static MatchState CompleteSetup(MatchState state)
    {
        for (var step = 0; state.Pending!.Kind != DecisionKind.Action; step++)
        {
            Assert.True(step < 6, "Setup failed to reach the first action.");
            var choice = NextSetupChoice(state);
            var samples = choice.Kind == CommandKind.AllocateHunt
                ? new int[state.Dice.Count(die => die.Location == DieLocation.Unrolled)] : [];
            var restored = Reload(state);
            var expected = KingsFixture.Do(state, choice.Kind, choice.Payload, samples);
            state = KingsFixture.Do(restored, choice.Kind, choice.Payload, samples);
            Assert.Equal(StateCodec.Write(expected), StateCodec.Write(state));
        }
        return state;
    }

    public static MatchState Reload(MatchState state)
    {
        var saved = StateCodec.Write(state);
        var restored = StateCodec.Read(saved);
        Assert.Equal(saved, StateCodec.Write(restored));
        foreach (var side in Enum.GetValues<Side>())
            Assert.Equal(Json(state, side), Json(restored, side));
        return restored;
    }

    public static string Json(MatchState state, Side side) => JsonSerializer.Serialize(Views.For(state, side), BoardCatalog.Json);
}
