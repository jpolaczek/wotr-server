using System.Diagnostics;
using System.Net.Http.Headers;
using Wotr.Contracts;

namespace Wotr.Tests;

internal sealed class KestrelProcess : IAsyncDisposable
{
    private readonly Process process;
    private readonly Task<string> output;
    private readonly Task<string> error;
    private readonly Uri address;

    private KestrelProcess(string connection)
    {
        address = new Uri($"http://127.0.0.1:{IsolatedPostgres.FreePort()}");
        var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!
        };
        info.ArgumentList.Add(typeof(Program).Assembly.Location);
        info.ArgumentList.Add("--urls=" + address);
        info.Environment["WOTR_DB"] = connection;
        info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        info.Environment["Logging__LogLevel__Default"] = "Warning";
        process = Process.Start(info)!;
        output = process.StandardOutput.ReadToEndAsync();
        error = process.StandardError.ReadToEndAsync();
    }

    public static async Task<KestrelProcess> Start(string connection)
    {
        var server = new KestrelProcess(connection);
        try
        {
            using var client = server.Client();
            for (var i = 0; i < 100; i++)
            {
                if (server.process.HasExited)
                    throw new InvalidOperationException("Kestrel exited before readiness: " + await server.error);
                try
                {
                    using var response = await client.GetAsync("/health/ready");
                    if (response.IsSuccessStatusCode)
                        return server;
                }
                catch (HttpRequestException) { }
                await Task.Delay(100);
            }
            throw new TimeoutException("Kestrel did not become ready.");
        }
        catch
        {
            await server.DisposeAsync();
            throw;
        }
    }

    public HttpClient Client(SessionView? session = null)
    {
        var client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(10) };
        if (session is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
            process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        await Task.WhenAll(output, error);
        process.Dispose();
    }
}
