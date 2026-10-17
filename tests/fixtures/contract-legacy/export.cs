using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;

var directory = Path.GetFullPath(args[0]);
Directory.CreateDirectory(directory);
var engine = new GameEngine(new TurnCommands(), new HuntCommands(), new DeclineReactionHandler(), new LordsDiceCommands());
var state = MatchFactory.Create("legacy-base-move", new ZeroRandom());
TurnFlow.Start(state);
state = Apply(state, Make(state, CommandKind.ContinueFellowship)).State;
var allocate = Make(state, CommandKind.AllocateHunt);
allocate.Payload.Ids.Add(DiceRules.HuntCandidates(state).First());
state = Apply(state, allocate).State;
var move = Make(state, CommandKind.MoveFellowship);
move.Payload.DieId = state.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character).Id;
Write("before.json", StateCodec.Write(state));
var serialized = JsonSerializer.Serialize(move, BoardCatalog.Json);
Write("command.json", serialized);
var result = Apply(state, move);
Write("after.json", StateCodec.Write(result.State));
Write("receipt.json", JsonSerializer.Serialize(result.Receipt, BoardCatalog.Json));
Write("fingerprint.txt", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized))));

var lords = MatchFactory.Create("legacy-lords-action", new ZeroRandom());
lords.Configuration.Lords = true;
LordsDiceSetup.Initialize(lords);
lords.Phase = GamePhase.ActionResolution;
lords.Turn = 1;
foreach (var die in lords.Dice.Where(d => d.Special == null))
{
    die.Location = DieLocation.Available;
    die.Face = DieFace.Character;
}
TurnFlow.SelectNext(lords, Side.FreePeoples);
Write("action.json", StateCodec.Write(lords));
lords.Resolution.Suspend(lords, "legacy-continuation", 1);
lords.Open(DecisionKind.Reaction, Side.Shadow, [CommandKind.DeclineReaction]);
Write("suspended-action.json", StateCodec.Write(lords));
Console.WriteLine($"Historical export: {result.State.Revision} revisions, {result.State.RandomSamples.Count - state.RandomSamples.Count} new RNG samples, {result.Receipt.Accepted}.");

GameCommand Make(MatchState source, CommandKind kind) => new()
{
    MatchId = source.Id,
    CommandId = $"legacy-{source.Revision}-{kind}",
    DecisionId = source.Pending!.Id,
    ExpectedRevision = source.Revision,
    Kind = kind
};
EngineResult Apply(MatchState source, GameCommand command)
{
    var applied = engine.Execute(source, source.Pending!.Owner, command, new ZeroRandom());
    if (!applied.Receipt.Accepted)
        throw new InvalidOperationException(applied.Receipt.Error);
    return applied;
}
void Write(string name, string value) => File.WriteAllText(Path.Combine(directory, name), value);
sealed class ZeroRandom : IRandomSource
{
    public int Next(int exclusiveMaximum) => 0;
}
