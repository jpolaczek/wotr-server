using System.Text.Json;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;

if (args is ["--list-base-corpus"])
{
    foreach (var pair in BaseCorpusSeeds.Create())
        Console.WriteLine($"{pair.Key}: {pair.Value.Pending?.Kind} {pair.Value.Pending?.Owner} {pair.Value.Victory}");
    return;
}
if (args is ["--export-base-corpus", var destination])
{
    BaseCorpusExport.Write(destination);
    return;
}

// Explicit test seed, outside the server executable and its HTTP surface.
var connectionString = Environment.GetEnvironmentVariable("WOTR_TEST_DB")
    ?? throw new InvalidOperationException("WOTR_TEST_DB is required.");
var settings = new NpgsqlConnectionStringBuilder(connectionString);
if (settings.Database?.EndsWith("_test", StringComparison.Ordinal) != true)
    throw new InvalidOperationException("Scenario seeding requires a database whose name ends with _test.");
if (args.Length != 1)
    throw new ArgumentException("Pass the output JSON path for the Unity integration fixture.");

await using var source = NpgsqlDataSource.Create(connectionString);
// Opt-in for a dedicated disposable database used by sequential client batches.
// The _test guard above applies before any schema can be removed.
if (Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_RESET_DATABASE") == "1")
{
    await using var reset = source.CreateCommand("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
    await reset.ExecuteNonQueryAsync();
}
var database = new Database(source);
await database.Migrate();
// AUTH-02: accounts carry an address on the reserved .test domain; nothing is ever sent.
var accounts = AccountService.WithoutEmail(database);
var password = "Scenario-" + Guid.NewGuid().ToString("N");
async Task<SessionView> Register()
{
    var username = "scenario-" + Guid.NewGuid().ToString("N")[..20];
    return (await accounts.Register(new RegisterRequest
    {
        Username = username,
        Email = username + "@example.test",
        Password = password
    })).Session ?? throw new InvalidOperationException("Scenario account registration failed.");
}
var fp = await Register();
var sh = await Register();
var random = new ServerRandom();
var state = MatchFactory.Create(Guid.NewGuid().ToString("N"), random);
TurnFlow.Start(state);
state.Phase = GamePhase.ActionResolution;
foreach (var nation in state.Nations)
{
    nation.Active = true;
    nation.StepsToWar = 0;
}
foreach (var side in Enum.GetValues<Side>())
{
    var dice = state.Dice.Where(d => d.Owner == side && d.Location == DieLocation.Unrolled).ToArray();
    for (var i = 0; i < dice.Length; i++)
    {
        dice[i].Location = DieLocation.Available;
        dice[i].Face = i == 0 ? DieFace.Army : i == 1 ? DieFace.Muster : DieFace.Character;
    }
}
TurnFlow.SelectNext(state, Side.FreePeoples);
await new MatchStore(database, new GameEngine(), random).Create(state, fp.AccountId, sh.AccountId);
var cards = CardScenario.Create(random);
await new MatchStore(database, new GameEngine(), random).Create(cards, fp.AccountId, sh.AccountId);
var fellowship = FellowshipScenario.Actions(random);
var declaration = FellowshipScenario.Declaration(random);
var hunt = FellowshipScenario.Hunt(random);
var entry = FellowshipScenario.Entry(random);
var mordor = new[] { FellowshipScenario.Mordor(random, 10), FellowshipScenario.Mordor(random, 11) };
foreach (var scenario in new[] { fellowship, declaration, hunt, entry }.Concat(mordor))
    await new MatchStore(database, new GameEngine(), random).Create(scenario, fp.AccountId, sh.AccountId);
var battles = BattleScenario.Create(random);
foreach (var battle in battles.Values)
    await new MatchStore(database, new GameEngine(), random).Create(battle, fp.AccountId, sh.AccountId);
var characters = CharacterScenario.Create(random);
foreach (var character in characters.Values)
    await new MatchStore(database, new GameEngine(), random).Create(character, fp.AccountId, sh.AccountId);
var cardPacks = CardPackScenario.Create(random);
foreach (var cardPack in cardPacks.Values)
    await new MatchStore(database, new GameEngine(), random).Create(cardPack, fp.AccountId, sh.AccountId);
var interactions = InteractionScenario.Create(random);
foreach (var interaction in interactions.Values)
    await new MatchStore(database, new GameEngine(), random).Create(interaction, fp.AccountId, sh.AccountId);
var baseAcceptance = BaseTurnScenario.Create(random)
    .Concat(BaseAbilityScenario.Create(random))
    .Concat(BaseBoundaryScenario.Create(random))
    .Concat(BaseInformationScenario.Create(state))
    .Concat(BaseBoardScenario.Create(state))
    .Concat(DiceArmyScenario.Create(state, random))
    .Concat(MultiUseDiceScenario.Create(state))
    .Concat(CardGestureScenario.Create(random))
    .Concat(ActionLogScenario.Create(random))
    .Concat(ActionContextScenario.Create(random))
    .Concat(EffectsScenario.Create(random))
    .Concat(VictoryScenario.Create(random))
    .ToDictionary(scenario => scenario.Key, scenario => scenario.Value);
foreach (var scenario in baseAcceptance.Values)
    await new MatchStore(database, new GameEngine(), random).Create(scenario, fp.AccountId, sh.AccountId);
var returnFp = await Register();
var returnSh = await Register();
var returns = ReturnScenario.Create(random);
foreach (var scenario in returns.Values)
    await new MatchStore(database, new GameEngine(), random).Create(scenario, returnFp.AccountId, returnSh.AccountId);
var fixture = new
{
    Address = "http://127.0.0.1:5088",
    // AUTH-02: accounts for the network tests instead of account invitations (the client signs in, it no longer registers).
    AccountA = (await Register()).Username,
    AccountB = (await Register()).Username,
    AccountPassword = password,
    MovementMatch = state.Id,
    CardsMatch = cards.Id,
    FellowshipMatch = fellowship.Id,
    DeclarationMatch = declaration.Id,
    HuntMatch = hunt.Id,
    MordorEntryMatch = entry.Id,
    MordorMatches = mordor.Select(s => s.Id).ToArray(),
    BattleMatches = battles.ToDictionary(p => p.Key, p => p.Value.Id),
    CharacterMatches = characters.ToDictionary(p => p.Key, p => p.Value.Id),
    CardPackMatches = cardPacks.ToDictionary(p => p.Key, p => p.Value.Id),
    InteractionMatches = interactions.ToDictionary(p => p.Key, p => p.Value.Id),
    BaseAcceptanceMatches = baseAcceptance.ToDictionary(p => p.Key, p => p.Value.Id),
    ReturnMatches = returns.ToDictionary(p => p.Key, p => p.Value.Id),
    ReturnFreePeoplesUser = returnFp.Username,
    ReturnShadowUser = returnSh.Username,
    FreePeoplesUser = fp.Username,
    ShadowUser = sh.Username,
    Password = password
};
await File.WriteAllTextAsync(args[0], JsonSerializer.Serialize(fixture));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "shadow03-layout.json"),
    JsonSerializer.Serialize(Shadow03Scenario.LayoutViews(cardPacks)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "shadow04-layout.json"),
    JsonSerializer.Serialize(Shadow04Scenario.LayoutViews(cardPacks)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "shadow-strategy01-layout.json"),
    JsonSerializer.Serialize(ShadowStrategy01Scenario.LayoutViews(cardPacks)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "shadow-strategy03-layout.json"),
    JsonSerializer.Serialize(ShadowStrategy03Scenario.LayoutViews(cardPacks)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "shadow-strategy04-layout.json"),
    JsonSerializer.Serialize(ShadowStrategy04Scenario.LayoutViews(cardPacks)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "shadow-strategy02-layout.json"),
    JsonSerializer.Serialize(ShadowStrategy02Scenario.LayoutViews(cardPacks)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "ui09-matrix.json"),
    JsonSerializer.Serialize(ExpansionUiMatrix.Create(random)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "ui09-resources.json"),
    JsonSerializer.Serialize(ExpansionUiMatrix.Resources(random)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "map17-profile.json"),
    JsonSerializer.Serialize(MapAcceptanceScenario.ProfileViews(interactions)));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "ui63-profile-views.json"),
    JsonSerializer.Serialize(InGameProfileScenario.ProfileViews(interactions["ui63-dense"])));
await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[0])!, "ui72-profile-views.json"),
    JsonSerializer.Serialize(BoardProfileScenario.ProfileViews(BoardProfileScenario.Inputs(random,
        interactions["ui66-full"], interactions["ui69-layout"], battles["no-regular"], interactions["ui34-hunt-reroll"]))));
Console.WriteLine("Created test-only accounts and movement scenario; credentials saved to the ignored fixture file.");
