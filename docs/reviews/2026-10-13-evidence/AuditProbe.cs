using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using System.Collections.Concurrent;
using Npgsql;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.Content;
using Wotr.Server;

var mode = args.FirstOrDefault() ?? "rules";
var output = Path.GetFullPath(args.ElementAtOrDefault(1) ?? ".artifacts/audit/probe.json");
var engine = GameRuntime.Engine(EffectCatalog.All);
var projection = GameRuntime.Projection(EffectCatalog.All);
var preview = new PreviewService(engine);
var zero = new ZeroRandom();
if (mode == "rules")
{
    var state = MatchFactory.Create("audit-ring-pass", zero);
    TurnFlow.Start(state);
    state.Phase = GamePhase.ActionResolution;
    foreach (var die in state.Dice.Where(d => d.Location == DieLocation.Unrolled))
        die.Location = DieLocation.Spent;
    foreach (var side in Enum.GetValues<Side>())
        foreach (var die in state.Dice.Where(d => d.Owner == side).Take(side == Side.FreePeoples ? 2 : 3))
        { die.Location = DieLocation.Available; die.Face = DieFace.Army; }
    TurnFlow.SelectNext(state, Side.FreePeoples);
    var before = TurnCommands.CanPass(state, Side.FreePeoples);
    var command = Command(state, CommandKind.UseRing);
    command.Payload.RingId = state.Rings.First().Id;
    command.Payload.DieId = state.Dice.First(d => d.Owner == Side.FreePeoples).Id;
    command.Payload.Face = DieFace.Muster;
    var result = engine.Execute(state, Side.FreePeoples, command, zero);
    var pass = Command(result.State, CommandKind.Pass);
    var findings = new List<object>();
    var cases = 0; var mutations = 0; var throws = 0;
    using var document = JsonDocument.Parse(File.ReadAllText("tests/fixtures/base-regression/manifest.json"));
    foreach (var entry in document.RootElement.GetProperty("cases").EnumerateArray())
    {
        if (entry.GetProperty("command").ValueKind == JsonValueKind.Null) continue;
        var original = StateCodec.Read(File.ReadAllText(Path.Combine("tests/fixtures/base-regression", entry.GetProperty("file").GetString()!)));
        var baseline = JsonSerializer.Deserialize<GameCommand>(entry.GetProperty("command").GetRawText(), BoardCatalog.Json)!;
        cases++;
        foreach (var property in typeof(CommandPayload).GetProperties().Where(p => p.PropertyType == typeof(string)))
        {
            var changed = JsonSerializer.Deserialize<GameCommand>(JsonSerializer.Serialize(baseline, BoardCatalog.Json), BoardCatalog.Json)!;
            property.SetValue(changed.Payload, null);
            foreach (var operation in new[] { "validate", "preview", "execute" })
            {
                mutations++;
                try
                {
                    if (operation == "validate") engine.Validate(original, original.Pending!.Owner, changed);
                    else if (operation == "preview") preview.Preview(original, original.Pending!.Owner, changed);
                    else engine.Execute(original, original.Pending!.Owner, changed, zero);
                }
                catch (Exception error)
                {
                    throws++;
                    findings.Add(new { Case = entry.GetProperty("name").GetString(), Field = property.Name, Operation = operation,
                        Type = error.GetType().Name, error.Message, Stack = error.StackTrace, Command = changed });
                }
            }
        }
    }
    var report = new { RingPass = new { Before = before, RingAccepted = result.Receipt.Accepted,
        After = TurnCommands.CanPass(result.State, Side.FreePeoples), PassError = engine.Validate(result.State, Side.FreePeoples, pass),
        FreeDice = TurnFlow.Available(result.State, Side.FreePeoples), ShadowDice = TurnFlow.Available(result.State, Side.Shadow) },
        Cases = cases, Mutations = mutations, Throws = throws, Findings = findings };
    File.WriteAllText(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"ring accepted={result.Receipt.Accepted}; pass before={before}, after={TurnCommands.CanPass(result.State, Side.FreePeoples)}; null mutations={mutations}, throws={throws}");
}
else if (mode == "micro")
{
    var reports = new List<object>();
    foreach (var size in new[] { 0, 500, 2000, 10000 })
    {
        var state = MatchFactory.Create("audit-history", zero); TurnFlow.Start(state);
        for (var i = 0; i < size; i++) state.Events.Add(new() { Revision = 0, Kind = "AuditSyntheticHistory", Text = new string('x', 160) });
        var command = Command(state, CommandKind.ContinueFellowship);
        foreach (var operation in new[] { "copy", "projection", "execute" })
        {
            var elapsed = new List<double>(); var allocated = new List<long>();
            for (var n = 0; n < 35; n++)
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread(); var watch = Stopwatch.StartNew();
                if (operation == "copy") StateCodec.Copy(state);
                else if (operation == "projection") projection.For(state, Side.FreePeoples);
                else engine.Execute(state, Side.FreePeoples, command, zero);
                watch.Stop();
                if (n >= 10) { elapsed.Add(watch.Elapsed.TotalMilliseconds); allocated.Add(GC.GetAllocatedBytesForCurrentThread() - bytes); }
            }
            elapsed.Sort();
            reports.Add(new { Events = size, Operation = operation, StateBytes = System.Text.Encoding.UTF8.GetByteCount(StateCodec.Write(state)),
                MedianMs = elapsed[elapsed.Count / 2], P95Ms = elapsed[(int)Math.Ceiling(elapsed.Count * .95) - 1],
                MeanAllocatedBytes = allocated.Average() });
        }
    }
    File.WriteAllText(output, JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(JsonSerializer.Serialize(reports));
}
else if (mode == "seed")
{
    var connection = Environment.GetEnvironmentVariable("WOTR_AUDIT_DB") ?? throw new InvalidOperationException("WOTR_AUDIT_DB required");
    if (new NpgsqlConnectionStringBuilder(connection).Database != "audit_load_test") throw new InvalidOperationException("Audit DB guard");
    await using var source = NpgsqlDataSource.Create(connection);
    var db = new Database(source); await db.Migrate();
    var accounts = new List<SessionView>();
    await using var sql = await source.OpenConnectionAsync();
    for (var i = 0; i < 300; i++)
    {
        var id = Guid.NewGuid().ToString("N"); var username = "audit-" + id[..20];
        await using var insert = Database.Command(sql, null, "INSERT INTO accounts(id,username,normalized_name,password_hash) VALUES($1,$2,$3,$4)",id,username,username.ToUpperInvariant(),"audit-no-login");
        await insert.ExecuteNonQueryAsync();
        accounts.Add(await SessionStore.Issue(sql, null, id, username, CancellationToken.None));
    }
    var matches = new List<object>();
    for (var i = 0; i < 150; i++)
    {
        var state = MatchFactory.Create("audit-load-" + i, zero); TurnFlow.Start(state);
        await new MatchStore(db,engine,zero).Create(state, accounts[2*i].AccountId, accounts[2*i+1].AccountId);
        matches.Add(new { Id = state.Id, Fp = accounts[2*i].Token, Sh = accounts[2*i+1].Token });
    }
    File.WriteAllText(output,JsonSerializer.Serialize(matches)); Console.WriteLine("150 isolated matches and 300 sessions seeded; private tokens only in ignored fixture.");
}
else if (mode == "load")
{
    var fixtures = JsonSerializer.Deserialize<List<LoadFixture>>(File.ReadAllText(".artifacts/audit/load-private.json"))!;
    using var client = new HttpClient(new SocketsHttpHandler { MaxConnectionsPerServer = 600 })
        { BaseAddress = new Uri("http://127.0.0.1:50973"), Timeout = TimeSpan.FromSeconds(20) };
    var stages = new List<object>();
    var pid = int.Parse(Environment.GetEnvironmentVariable("WOTR_AUDIT_SERVER_PID")!);
    foreach (var count in (Environment.GetEnvironmentVariable("WOTR_AUDIT_TABLES") ?? "1,50,150").Split(',').Select(int.Parse))
    {
        var sockets = new List<ClientWebSocket>();
        using var cancellation = new CancellationTokenSource();
        var socketTasks = new List<Task>();
        foreach (var fixture in fixtures.Take(count))
            foreach (var token in new[] { fixture.Fp, fixture.Sh })
            {
                var socket = new ClientWebSocket();
                socket.Options.SetRequestHeader("Authorization", "Bearer " + token);
                socket.Options.SetRequestHeader(ClientCompatibility.ClientVersionHeader, "0.1.0");
                socket.Options.SetRequestHeader(ClientCompatibility.ProtocolVersionHeader, "2");
                await socket.ConnectAsync(new Uri($"ws://127.0.0.1:50973/api/matches/{fixture.Id}/updates"), cancellation.Token);
                sockets.Add(socket);
                socketTasks.Add(Task.Run(async () => {
                    var buffer = new byte[1024];
                    try { while (!cancellation.IsCancellationRequested) await socket.ReceiveAsync(buffer, cancellation.Token); }
                    catch (OperationCanceledException) { } catch (WebSocketException) { }
                }));
            }
        var samples = new ConcurrentBag<LoadSample>();
        var errors = new ConcurrentBag<string>();
        var resources = new List<object>();
        var watch = Stopwatch.StartNew();
        var process = Process.GetProcessById(pid);
        var initialCpu = process.TotalProcessorTime;
        var operations = fixtures.Take(count).Select((fixture,index) => Task.Run(async () =>
        {
            await Task.Delay(index * 3000 / Math.Max(1,count));
            while (watch.Elapsed.TotalSeconds < 75)
            {
                var cycle = Stopwatch.StartNew();
                try
                {
                    var views = await Task.WhenAll(Read(fixture,fixture.Fp),Read(fixture,fixture.Sh));
                    var view = views.Single(v => v.Decision != null);
                    var token = view.You == Side.FreePeoples ? fixture.Fp : fixture.Sh;
                    var command = new GameCommand { MatchId = fixture.Id, CommandId = Guid.NewGuid().ToString("N"),
                        ExpectedRevision = view.Revision, DecisionId = view.Decision!.Id };
                    command.Kind = view.Decision.Kind switch
                    {
                        DecisionKind.FellowshipPhase => CommandKind.ContinueFellowship,
                        DecisionKind.AllocateHunt => CommandKind.AllocateHunt,
                        DecisionKind.DiscardCards => CommandKind.DiscardCards,
                        DecisionKind.Action => CommandKind.DiscardDie,
                        _ => throw new InvalidOperationException("Unexpected load decision " + view.Decision.Kind)
                    };
                    if (command.Kind == CommandKind.AllocateHunt)
                    {
                        var option = view.Options.Single(o => o.Kind == command.Kind);
                        command.Payload.Ids = option.EligibleIds.Take(option.Minimum).ToList();
                    }
                    if (command.Kind == CommandKind.DiscardCards) command.Payload.Ids = view.Hand.Take(view.Decision.RequiredCount).ToList();
                    if (command.Kind == CommandKind.DiscardDie) command.Payload.DieId = view.Dice.First(d => d.Owner == view.You && d.Location == DieLocation.Available).Id;
                    using var request = Request(fixture,token,HttpMethod.Post,"/commands");
                    request.Content = JsonContent.Create(command,options:BoardCatalog.Json);
                    var clock = Stopwatch.StartNew();
                    using var response = await client.SendAsync(request);
                    var receipt = await response.Content.ReadFromJsonAsync<CommandReceipt>(BoardCatalog.Json);
                    clock.Stop();
                    if (!response.IsSuccessStatusCode || receipt?.Accepted != true) errors.Add($"command HTTP {(int)response.StatusCode}: {receipt?.Error}");
                    if (watch.Elapsed.TotalSeconds >= 15) samples.Add(new("command",clock.Elapsed.TotalMilliseconds));
                }
                catch (Exception error) { errors.Add(error.GetType().Name+":"+error.Message); }
                var delay = 5000 - (int)cycle.ElapsedMilliseconds;
                if (delay > 0) await Task.Delay(delay);
            }
        })).ToArray();
        while (operations.Any(t => !t.IsCompleted))
        {
            process.Refresh();
            resources.Add(new { Seconds = watch.Elapsed.TotalSeconds, RssBytes = process.WorkingSet64,
                CpuSeconds = (process.TotalProcessorTime-initialCpu).TotalSeconds, Threads = process.Threads.Count });
            await Task.Delay(1000);
        }
        await Task.WhenAll(operations);
        var cpu = (process.TotalProcessorTime-initialCpu).TotalSeconds;
        cancellation.Cancel();
        foreach (var socket in sockets) { socket.Abort(); socket.Dispose(); }
        await Task.WhenAll(socketTasks);
        var summary = new { Tables = count, Players = 2*count, Seconds = watch.Elapsed.TotalSeconds,
            ServerCpuCoreEquivalents = cpu/watch.Elapsed.TotalSeconds, Errors = errors.ToArray(),
            Requests = samples.GroupBy(s=>s.Kind).Select(g=>new { Kind=g.Key, Count=g.Count(),
                P50Ms=Percentiles.Value(g.Select(x=>x.Milliseconds),.50),P95Ms=Percentiles.Value(g.Select(x=>x.Milliseconds),.95),P99Ms=Percentiles.Value(g.Select(x=>x.Milliseconds),.99),MaxMs=g.Max(x=>x.Milliseconds) }),
            Resources=resources };
        stages.Add(summary);
        File.WriteAllText(output,JsonSerializer.Serialize(stages,new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"tables={count}, players={2*count}, measured_requests={samples.Count}, errors={errors.Count}, server_cpu_cores={cpu/watch.Elapsed.TotalSeconds:F2}");

        HttpRequestMessage Request(LoadFixture fixture,string token,HttpMethod method,string suffix="")
        {
            var request = new HttpRequestMessage(method,$"api/matches/{fixture.Id}{suffix}");
            request.Headers.Authorization = new("Bearer",token);
            request.Headers.Add(ClientCompatibility.ClientVersionHeader,"0.1.0");
            request.Headers.Add(ClientCompatibility.ProtocolVersionHeader,"2");
            return request;
        }
        async Task<PlayerView> Read(LoadFixture fixture,string token)
        {
            using var request = Request(fixture,token,HttpMethod.Get);
            var clock = Stopwatch.StartNew();
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var view = (await response.Content.ReadFromJsonAsync<PlayerView>(BoardCatalog.Json))!;
            clock.Stop();
            if (watch.Elapsed.TotalSeconds>=15) samples.Add(new("get-view",clock.Elapsed.TotalMilliseconds));
            return view;
        }
    }
}
else if (mode == "reconnect")
{
    var fixtures = JsonSerializer.Deserialize<List<LoadFixture>>(File.ReadAllText(".artifacts/audit/load-private.json"))!;
    using var client = new HttpClient(new SocketsHttpHandler { MaxConnectionsPerServer = 600 })
        { BaseAddress = new Uri("http://127.0.0.1:50973"), Timeout = TimeSpan.FromSeconds(20) };
    var samples = new ConcurrentBag<double>(); var errors = new ConcurrentBag<string>();
    var watch = Stopwatch.StartNew();
    await Task.WhenAll(fixtures.SelectMany(f => new[] { (Fixture:f,Token:f.Fp),(Fixture:f,Token:f.Sh) }).Select(async player =>
    {
        var clock = Stopwatch.StartNew();
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var request = new HttpRequestMessage(HttpMethod.Get,$"api/matches/{player.Fixture.Id}");
            request.Headers.Authorization = new("Bearer",player.Token);
            request.Headers.Add(ClientCompatibility.ClientVersionHeader,"0.1.0");
            request.Headers.Add(ClientCompatibility.ProtocolVersionHeader,"2");
            using var response = await client.SendAsync(request,deadline.Token); response.EnsureSuccessStatusCode();
            await response.Content.ReadAsByteArrayAsync(deadline.Token);
            using var socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("Authorization","Bearer "+player.Token);
            socket.Options.SetRequestHeader(ClientCompatibility.ClientVersionHeader,"0.1.0");
            socket.Options.SetRequestHeader(ClientCompatibility.ProtocolVersionHeader,"2");
            await socket.ConnectAsync(new Uri($"ws://127.0.0.1:50973/api/matches/{player.Fixture.Id}/updates"),deadline.Token);
            var message = await socket.ReceiveAsync(new byte[1024],deadline.Token);
            if (message.MessageType != WebSocketMessageType.Text) throw new InvalidOperationException("Missing initial revision");
            samples.Add(clock.Elapsed.TotalMilliseconds);
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,"audit complete",deadline.Token);
        }
        catch (Exception error) { errors.Add(error.GetType().Name+":"+error.Message); }
    }));
    var result = new { Players = 300, Successful = samples.Count, Errors = errors.ToArray(), Seconds = watch.Elapsed.TotalSeconds,
        P50Ms = Percentiles.Value(samples,.5), P95Ms = Percentiles.Value(samples,.95), P99Ms = Percentiles.Value(samples,.99), MaxMs = samples.IsEmpty?0:samples.Max() };
    File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(JsonSerializer.Serialize(result));
}

static GameCommand Command(MatchState state, CommandKind kind) => new() { MatchId = state.Id, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = state.Revision, DecisionId = state.Pending!.Id, Kind = kind };
sealed class ZeroRandom : IRandomSource { public int Next(int exclusiveMaximum) => 0; }
sealed record LoadFixture(string Id,string Fp,string Sh);
sealed record LoadSample(string Kind,double Milliseconds);
static partial class Percentiles
{
    public static double Value(IEnumerable<double> values,double fraction)
    {
        var sorted=values.Order().ToArray();
        return sorted.Length==0?0:sorted[(int)Math.Ceiling(sorted.Length*fraction)-1];
    }
}
