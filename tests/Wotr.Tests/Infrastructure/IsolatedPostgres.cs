using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Wotr.Tests;

// Owns only the temporary cluster it creates; never stops the developer's database.
internal sealed class IsolatedPostgres : IAsyncDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "wotr-journal-" + Guid.NewGuid().ToString("N"));
    private const string Homebrew = "/opt/homebrew/opt/postgresql@17/bin";
    private const string Debian = "/usr/lib/postgresql";
    private readonly string binaries = Binaries();
    private readonly int port = FreePort();
    private bool running;
    public string Connection => $"Host=127.0.0.1;Port={port};Username=wotr;Database=postgres";

    public async Task Start()
    {
        Directory.CreateDirectory(directory);
        await Run("initdb", "-D", Path.Combine(directory, "data"), "-U", "wotr", "-A", "trust", "--encoding=UTF8", "--locale=C");
        await Launch();
    }

    public async Task Restart()
    {
        await Stop();
        await Launch();
    }

    private async Task Launch()
    {
        await Run("pg_ctl", "-D", Path.Combine(directory, "data"), "-l", Path.Combine(directory, "postgres.log"),
            "-o", $"-p {port} -h 127.0.0.1 -c unix_socket_directories=''", "-w", "start");
        running = true;
    }

    private async Task Stop()
    {
        if (!running)
            return;
        await Run("pg_ctl", "-D", Path.Combine(directory, "data"), "-w", "stop", "-m", "fast");
        running = false;
    }

    // The server tools of the temporary cluster: WOTR_PG_BIN, else PostgreSQL 17 from Homebrew (macOS), else the newest
    // Debian/Ubuntu install (/usr/lib/postgresql/<version>/bin, as on GitHub's Ubuntu runners), else the tools on PATH.
    private static string Binaries()
    {
        var configured = Environment.GetEnvironmentVariable("WOTR_PG_BIN");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;
        if (File.Exists(Path.Combine(Homebrew, "initdb")))
            return Homebrew;
        return NewestDebian() ?? "";
    }

    private static string? NewestDebian()
    {
        if (!Directory.Exists(Debian))
            return null;
        return Directory.GetDirectories(Debian)
            .Select(folder => (Bin: Path.Combine(folder, "bin"), Version: int.TryParse(Path.GetFileName(folder), out var version) ? version : 0))
            .Where(install => File.Exists(Path.Combine(install.Bin, "initdb")))
            .OrderByDescending(install => install.Version)
            .Select(install => install.Bin)
            .FirstOrDefault();
    }

    private async Task Run(string tool, params string[] arguments)
    {
        var path = binaries.Length > 0 ? Path.Combine(binaries, tool) : tool;
        var info = new ProcessStartInfo(path) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(timeout.Token);
        await output;
        var message = await error;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Isolated PostgreSQL {path} failed: {message}");
    }

    internal static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public async ValueTask DisposeAsync()
    {
        await Stop();
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }
}
