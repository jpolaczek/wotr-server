using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.BotProtocol;
using Wotr.Domain.Content;

namespace Wotr.Server;

// What one protocol decision came to: the command the bot's answers complete, or why there is none (the decision then
// waits and is asked again; no other engine answers it).
public sealed record ProtocolSeatResult(GameCommand? Command, string Outcome, int Questions, double Seconds);

// BOT-225: the CPU seats that play through the WotR Bot Protocol. One session per game and side with the operator's
// sidecar, kept open between decisions and opened again (new_game, then the turn's start) after a restart or a lost
// connection. The questions are built from the seat's own projection and judged by the engine on the match's state;
// only the seat's projection, options and its own events leave the server (R11).
public sealed class ProtocolSeatHost(ProtocolSeatOptions options, GameEngine engine, ILogger<ProtocolSeatHost> logger) : IDisposable
{
    private readonly ConcurrentDictionary<(string Match, Side Side), Seat> seats = new();
    private readonly ConcurrentDictionary<(string Match, Side Side), int> failures = new();

    private sealed class Seat(ProtocolSession session, ProtocolMemory memory) : IDisposable
    {
        public ProtocolSession Session { get; } = session;
        public ProtocolMemory Memory { get; } = memory;
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public int Turn { get; set; }
        public DateTimeOffset Used { get; set; } = DateTimeOffset.UtcNow;

        public void Dispose()
        {
            Session.Dispose();
            Gate.Dispose();
        }
    }

    public const string AlphaWotr = "alphawotr";

    public bool Enabled => options.Enabled;

    // BOT-224: 5 s after a first failure, doubling while failures repeat, at most a minute; a success resets it.
    public TimeSpan RetryDelay(string match, Side side) =>
        TimeSpan.FromSeconds(Math.Min(60, 5 * Math.Pow(2, Math.Max(0, failures.GetValueOrDefault((match, side)) - 1))));

    // A trace of every question and answer (the arena's audit; null on a game server).
    public Action<JsonObject>? Trace { get; set; }

    // A base game only (no expansion, no variant): protocol 0.1 knows nothing else.
    public bool Handles(PlayerView view) => options.Enabled && BaseGame(view.Configuration);

    public static bool BaseGame(MatchConfiguration configuration) => configuration is
    {
        Lords: false, Kings: false, Fate: false, Council: false, VisibleCorruption: false, SequentialCorruption: false,
        RemovingRulerTokens: false, NewCities: false
    } && configuration.Dismay is null or false;

    public async Task<ProtocolSeatResult> Decide(PlayerView view, MatchState state, string invocationId, CancellationToken stopping)
    {
        var watch = Stopwatch.StartNew();
        Expire();
        var side = view.You;
        if (state.Revision != view.Revision || state.Pending?.Owner != side)
            return new(null, "Stale", 0, 0);
        ProtocolNode? root;
        try
        {
            root = ProtocolQuestions.For(view, command => engine.Validate(state, side, command) is null);
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException)
        {
            logger.LogWarning("The protocol seat could not write decision {Kind}: {Error}.", view.Decision?.Kind, error.Message);
            return new(null, "NoQuestion", 0, watch.Elapsed.TotalSeconds);
        }
        if (root is null)
        {
            Failed(view, "NoQuestion");
            return new(null, "NoQuestion", 0, watch.Elapsed.TotalSeconds);
        }
        var decision = ProtocolDecision.Start(root);
        if (decision.Command is { } forced)
            return new(Identify(forced, view, invocationId), "Forced", 0, watch.Elapsed.TotalSeconds);
        Seat seat;
        try
        {
            seat = await Open(view, stopping);
        }
        catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException or OperationCanceledException
                                          or InvalidOperationException)
        {
            if (stopping.IsCancellationRequested)
                throw;
            logger.LogWarning("The protocol sidecar did not open a game: {Error}.", error.GetType().Name);
            Failed(view, "SidecarUnavailable");
            return new(null, "SidecarUnavailable", 0, watch.Elapsed.TotalSeconds);
        }
        await seat.Gate.WaitAsync(stopping);
        try
        {
            seat.Used = DateTimeOffset.UtcNow;
            if (view.Turn > seat.Turn)
            {
                await seat.Session.Event(new JsonObject { ["kind"] = "turn_started", ["turn"] = view.Turn }, stopping);
                seat.Turn = view.Turn;
            }
            var asked = 0;
            var sideKey = ProtocolVocabulary.Side(side);
            while (decision.Question is { } question)
            {
                var left = options.DecisionTimeout - watch.Elapsed;
                if (left <= TimeSpan.Zero)
                    return Lost(view, seat, "Timeout", asked, watch);
                var message = ProtocolSession.Decision(sideKey, question, Prompt(question));
                var answer = await seat.Session.Ask(message, () => ProtocolView.Write(view, seat.Memory), (int)left.TotalMilliseconds, stopping);
                asked++;
                // What the bot's own engine chose, where the options cannot say it (the Voice of Saruman).
                var option = answer.Option is { } o ? ProtocolHints.Adjust(question, o, answer.Info) : (int?)null;
                Trace?.Invoke(new JsonObject
                {
                    ["game"] = view.MatchId, ["turn"] = view.Turn, ["side"] = sideKey, ["decision"] = view.Decision?.Kind.ToString(),
                    ["question"] = message, ["answer"] = answer.Option, ["taken"] = option, ["outcome"] = answer.Outcome,
                    ["info"] = answer.Info?.DeepClone()
                });
                if (option is not null && option != answer.Option)
                    logger.LogInformation("The protocol seat took option {Option} for {Answer} after the bot's engine action.", option, answer.Option);
                if (option is null || !decision.Answer(option.Value))
                    return Lost(view, seat, answer.Outcome switch { "timeout" => "Timeout", "illegal" => "IllegalAnswer", _ => "SidecarLost" },
                        asked, watch);
            }
            failures.TryRemove((view.MatchId, side), out _);
            return new(Identify(decision.Command!, view, invocationId), "Answered", asked, watch.Elapsed.TotalSeconds);
        }
        catch (Exception error) when (error is IOException or ObjectDisposedException)
        {
            return Lost(view, seat, "SidecarLost", 0, watch);
        }
        finally
        {
            seat.Gate.Release();
        }
    }

    // After the seat's command was accepted: the events the bot follows (SPEC §8) for its own acts.
    public async Task Applied(PlayerView before, GameCommand command, CancellationToken cancellation)
    {
        if (!seats.TryGetValue((before.MatchId, before.You), out var seat))
            return;
        var side = ProtocolVocabulary.Side(before.You);
        var events = new List<JsonObject>();
        if (command.Payload.DieId.Length > 0 && before.Dice.FirstOrDefault(d => d.Id == command.Payload.DieId)?.Face is { } face)
            events.Add(new JsonObject
            {
                ["kind"] = "die_used", ["side"] = side, ["face"] = ProtocolVocabulary.Face(face), ["action"] = Action(command.Kind)
            });
        if (command.Kind == CommandKind.MoveFellowship)
            events.Add(new JsonObject { ["kind"] = "fellowship_moved", ["side"] = side });
        if (command.Kind == CommandKind.HideFellowship)
            events.Add(new JsonObject { ["kind"] = "fellowship_hidden", ["side"] = side });
        if (command.Kind == CommandKind.EnterMordor)
            events.Add(new JsonObject { ["kind"] = "fellowship_entered_mordor", ["side"] = side });
        await seat.Gate.WaitAsync(cancellation);
        try
        {
            foreach (var e in events)
                await seat.Session.Event(e, cancellation);
        }
        catch (Exception error) when (error is IOException or ObjectDisposedException)
        {
            Close(before.MatchId, before.You);
        }
        finally
        {
            seat.Gate.Release();
        }
    }

    // The game is over: the bot hears the result and its session ends.
    public async Task Finished(PlayerView view, CancellationToken cancellation)
    {
        if (!seats.TryRemove((view.MatchId, view.You), out var seat))
            return;
        try
        {
            await seat.Session.End(view.Winner is { } w ? ProtocolVocabulary.Side(w) : null, How(view.Victory), view.Turn, cancellation);
        }
        catch (Exception error) when (error is IOException or ObjectDisposedException)
        {
        }
        finally
        {
            seat.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var key in seats.Keys)
            Close(key.Match, key.Side);
    }

    private async Task<Seat> Open(PlayerView view, CancellationToken stopping)
    {
        if (seats.TryGetValue((view.MatchId, view.You), out var open))
            return open;
        var transport = await StreamTransport.Connect(options.Host, options.Port, stopping);
        try
        {
            var game = new ProtocolGame(view.MatchId, ProtocolVocabulary.Side(view.You), Seed(view.MatchId, view.You),
                (int)options.DecisionTimeout.TotalMilliseconds, options.BotOptions);
            var session = await ProtocolSession.Open(transport, game, options.ConnectTimeout, stopping);
            logger.LogInformation("The protocol seat opened game {Match} as {Side} with {Bot} {Version}.", view.MatchId, view.You,
                session.Bot.Name, session.Bot.Version);
            var seat = new Seat(session, new ProtocolMemory());
            return seats.GetOrAdd((view.MatchId, view.You), seat) is var added && ReferenceEquals(added, seat) ? seat : Discard(seat, added);
        }
        catch
        {
            transport.Dispose();
            throw;
        }
    }

    // A failure counts toward the retry interval and, where the operator asked for it, is written for replay.
    private void Failed(PlayerView view, string outcome)
    {
        failures.AddOrUpdate((view.MatchId, view.You), 1, (_, n) => n + 1);
        if (options.DumpDirectory is not { } folder)
            return;
        try
        {
            Directory.CreateDirectory(folder);
            var name = $"{view.MatchId}-r{view.Revision}-{view.You}-{outcome}.json";
            var record = new JsonObject
            {
                ["outcome"] = outcome, ["revision"] = view.Revision, ["decision"] = view.Decision?.Kind.ToString(),
                ["view"] = System.Text.Json.JsonSerializer.SerializeToNode(view, BoardCatalog.Json)
            };
            File.WriteAllText(Path.Combine(folder, string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c is '-' or '.' ? c : '_'))),
                record.ToJsonString());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("The protocol seat could not write its failure: {Error}.", error.GetType().Name);
        }
    }

    private static Seat Discard(Seat extra, Seat kept)
    {
        extra.Dispose();
        return kept;
    }

    private ProtocolSeatResult Lost(PlayerView view, Seat seat, string outcome, int asked, Stopwatch watch)
    {
        Failed(view, outcome);
        logger.LogWarning("The protocol seat lost decision {Kind} at revision {Revision}: {Outcome}.", view.Decision?.Kind, view.Revision, outcome);
        Close(view.MatchId, view.You, seat);
        return new(null, outcome, asked, watch.Elapsed.TotalSeconds);
    }

    private void Close(string match, Side side, Seat? only = null)
    {
        if (only is not null)
        {
            if (seats.TryRemove(new KeyValuePair<(string, Side), Seat>((match, side), only)))
                only.Dispose();
            return;
        }
        if (seats.TryRemove((match, side), out var seat))
            seat.Dispose();
    }

    private void Expire()
    {
        var limit = DateTimeOffset.UtcNow - options.IdleSession;
        foreach (var (key, seat) in seats)
            if (seat.Used < limit && seat.Gate.CurrentCount == 1)
                Close(key.Match, key.Side, seat);
    }

    private static GameCommand Identify(GameCommand command, PlayerView view, string invocationId) => new()
    {
        MatchId = view.MatchId, CommandId = invocationId, ExpectedRevision = view.Revision, DecisionId = view.Decision!.Id,
        Kind = command.Kind, Payload = command.Payload
    };

    private static int Seed(string match, Side side) =>
        BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(match + ":" + side))) & int.MaxValue;

    private static string Prompt(ProtocolNode question) => question.Question switch
    {
        "ACTION" => "Choose an Action die and what to do with it, or pass",
        "ARMY_MOVE" => "Choose an Army to move, or end the move",
        "ARMY_SELECTION" => "How many of these figures go",
        "CASUALTY" => "Choose the casualty for a hit",
        "CARD_CHOICE" => (string?)question.Context?["question"] ?? "Answer the card's question",
        _ => question.Question.Replace('_', ' ').ToLowerInvariant()
    };

    private static string Action(CommandKind kind) => kind switch
    {
        CommandKind.MoveArmies => "move_armies",
        CommandKind.AttackArmy => "attack",
        CommandKind.Recruit => "recruit",
        CommandKind.DrawCard => "draw_card",
        CommandKind.AdvancePolitics => "diplomacy",
        CommandKind.PlayEvent => "play_card",
        CommandKind.MoveFellowship => "move_fellowship",
        CommandKind.HideFellowship => "hide_fellowship",
        CommandKind.MoveCompanions => "move_companions",
        CommandKind.SeparateCompanions => "separate_companions",
        CommandKind.MoveShadowCharacters => "move_minions",
        CommandKind.EnterCharacter => "play_character",
        _ => kind.ToString()
    };

    private static string How(VictoryCondition? victory) => victory switch
    {
        VictoryCondition.RingDestroyed => "RING_DESTROYED",
        VictoryCondition.RingCorrupted => "CORRUPTION",
        VictoryCondition.ShadowMilitary => "SHADOW_MILITARY",
        VictoryCondition.FreePeoplesMilitary => "FREE_PEOPLES_MILITARY",
        VictoryCondition.Conceded or VictoryCondition.TimedOut => "FORFEIT",
        _ => "ABORTED"
    };
}
