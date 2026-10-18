using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Server;

if (args.Contains("--health-check", StringComparer.Ordinal))
{
    Environment.ExitCode = await ServerHealth.CheckLocal();
    return;
}
var setEmail = OperatorCommands.SetEmail(args);
var rename = OperatorCommands.Rename(args);
var builder = WebApplication.CreateBuilder(OperatorCommands.HostArguments(args));
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 65536);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(DatabaseConfiguration.Read(builder.Configuration)));
builder.Services.AddServerTransport(builder.Configuration);
builder.Services.AddSingleton<Database>();
builder.Services.AddSingleton<IRandomSource, ServerRandom>();
builder.Services.AddSingleton(EffectCatalog.All);
builder.Services.AddSingleton(services => GameRuntime.Engine(services.GetRequiredService<EffectCatalog>()));
builder.Services.AddSingleton(services => GameRuntime.Projection(services.GetRequiredService<EffectCatalog>()));
builder.Services.AddSingleton<PreviewService>();
builder.Services.AddSingleton<MatchStore>();
builder.Services.AddSingleton<MatchRemoval>();
builder.Services.AddSingleton<ReplayExporter>();
builder.Services.AddSingleton<MatchIndexStore>();
builder.Services.AddSingleton<CorrespondenceStore>();
builder.Services.AddSingleton<ActionLogStore>();
builder.Services.AddSingleton<BattleReportStore>();
builder.Services.AddSingleton<RevisionHub>();
builder.Services.AddSingleton<MatchSocket>();
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton<AccountRename>();
builder.Services.AddSingleton<AccountRecovery>();
builder.Services.AddSingleton(services => EmailOptions.Read(services.GetRequiredService<IConfiguration>(), services.GetRequiredService<IHostEnvironment>()));
builder.Services.AddSingleton<IEmailSender>(EmailSenders.Create);
builder.Services.AddSingleton<EmailOutbox>();
builder.Services.AddHostedService(services => services.GetRequiredService<EmailOutbox>());
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<ISessionResolver>(services => services.GetRequiredService<SessionStore>());
builder.Services.AddSingleton(services => ReleaseScope.Read(services.GetRequiredService<IConfiguration>(), services.GetRequiredService<IHostEnvironment>()));
builder.Services.AddSingleton(services => ClientReleaseOptions.Read(services.GetRequiredService<IConfiguration>(), services.GetRequiredService<IHostEnvironment>()));
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ReleaseCatalogStore>();
builder.Services.AddSingleton<ClientCompatibilityPolicy>();
builder.Services.AddSingleton(services => EmptyRoomOptions.Read(services.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<EmptyRoomSweeper>();
builder.Services.AddHostedService(services => services.GetRequiredService<EmptyRoomSweeper>());
builder.Services.AddSingleton(services => ObserverOptions.Read(services.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<RoomStore>();
builder.Services.AddSingleton<RoomChat>();
builder.Services.AddSingleton(services => LobbyOptions.Read(services.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<LobbyPresence>();
builder.Services.AddSingleton<LobbyChat>();
builder.Services.AddSingleton<LobbyChallenges>();
builder.Services.AddSingleton<ChallengeAcceptance>();
builder.Services.AddSingleton<Lobby>();
builder.Services.AddSingleton(services => TestScenarioOptions.Read(services.GetRequiredService<IConfiguration>(), services.GetRequiredService<IHostEnvironment>()));
builder.Services.AddSingleton<TestSessionStore>();
builder.Services.AddSingleton<DemoSessionStore>();
builder.Services.AddSingleton(services => AdminOptions.Read(services.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<AdminAccess>();
builder.Services.AddSingleton<AdminStatisticsStore>();
builder.Services.AddSingleton(services => DemoOptions.Read(services.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton(services => new DemoBot(services.GetRequiredService<GameEngine>(), services.GetRequiredService<Projection>()));
builder.Services.AddSingleton<DemoBotRunner>();
builder.Services.AddHostedService(services => services.GetRequiredService<DemoBotRunner>());
builder.Services.AddAuthenticationRateLimit();
builder.Services.AddGameRateLimits();
builder.Services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, BearerAuthentication>("Bearer", _ => { });
builder.Services.AddAuthorization();
var app = builder.Build();
await app.Services.GetRequiredService<Database>().Migrate();
app.UseSaveFailureHandling();
app.UseDatabaseOverloadHandling();
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    Console.WriteLine("Database migrations complete.");
    await app.DisposeAsync();
    return;
}
app.UseServerTransport(app.Environment.IsDevelopment());
// AUTH-02: an email address for an account from before AUTH-02; the confirmation link is sent at once.
if (setEmail is not null)
{
    Environment.ExitCode = await OperatorCommands.RunSetEmail(app.Services, setEmail.Value.Username, setEmail.Value.Email);
    await app.DisposeAsync();
    return;
}
// AUTH-06: the operator renames an account, e.g. its owner's.
if (rename is not null)
{
    Environment.ExitCode = await OperatorCommands.RunRename(app.Services, rename.Value.Current, rename.Value.Name);
    await app.DisposeAsync();
    return;
}
// AUTH-02: a broken email configuration stops the server at start.
app.Services.GetRequiredService<EmailOptions>();
// DIST-05: fails fast on a broken release configuration, then stops incompatible clients before anything else.
app.Services.GetRequiredService<ClientReleaseOptions>();
// QUAL-02: a broken budget configuration stops the server at start.
app.Services.GetRequiredService<GameRateLimitOptions>();
// ADMIN-01: a malformed list of administrators stops the server at start.
app.Services.GetRequiredService<AdminOptions>();
app.UseClientCompatibility();
app.UseWebSockets();
app.UseAuthentication();
// QUAL-02: after authentication, so signed-in budgets follow the account.
app.UseRateLimiter();
app.UseTestSeatGate();
app.UseAuthorization();
ServerHealth.Map(app);
ClientReleaseEndpoints.Map(app);
MatchEndpoints.Map(app);
CorrespondenceEndpoints.Map(app);
ActionLogEndpoints.Map(app);
BattleReportEndpoints.Map(app);
ObserverEndpoints.Map(app);
ReplayEndpoints.Map(app);
LobbyEndpoints.Map(app);
LobbyChatEndpoints.Map(app);
RoomChatEndpoints.MapGames(app);
TestScenarioEndpoints.Map(app);
AccountEndpoints.Map(app);
DemoEndpoints.Map(app);
AdminEndpoints.Map(app);
app.Run();

public partial class Program
{
}
