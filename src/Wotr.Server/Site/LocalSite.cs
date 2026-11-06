using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Wotr.Server;

// WEB-02: a local server has no nginx. Only in Development, and only when told where the site is, it serves infra/site as
// infra/nginx/game.conf does, with that configuration's headers, so the account page can be tried beside a local game.
public static partial class LocalSite
{
    public const string RootKey = "Wotr:Site:Root";

    public static void Use(WebApplication app)
    {
        var root = app.Configuration[RootKey];
        if (!app.Environment.IsDevelopment() || string.IsNullOrEmpty(root))
            return;
        var folder = Path.GetFullPath(root);
        var files = new PhysicalFileProvider(folder);
        var headers = SecurityHeaders(folder);
        app.Use((context, next) =>
        {
            Rewrite(context.Request, files);
            return next();
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            OnPrepareResponse = served => AddHeaders(served.Context.Response, headers)
        });
    }

    // nginx's try_files: /account/reset is /account/reset.html, and a folder is its index.
    private static void Rewrite(HttpRequest request, PhysicalFileProvider files)
    {
        var path = request.Path.Value ?? "";
        if (path.StartsWith("/api", StringComparison.Ordinal) || Path.HasExtension(path))
            return;
        var page = path.TrimEnd('/') + ".html";
        var index = path.TrimEnd('/') + "/index.html";
        if (path != "/" && files.GetFileInfo(page).Exists)
            request.Path = page;
        else if (files.GetFileInfo(index).Exists)
            request.Path = index;
    }

    private static List<KeyValuePair<string, string>> SecurityHeaders(string site)
    {
        var configuration = Path.Combine(site, "..", "nginx", "security-headers.conf");
        if (!File.Exists(configuration))
            return [];
        return File.ReadLines(configuration).Select(line => HeaderLine().Match(line)).Where(match => match.Success)
            .Select(match => KeyValuePair.Create(match.Groups[1].Value, match.Groups[2].Value)).ToList();
    }

    private static void AddHeaders(HttpResponse response, List<KeyValuePair<string, string>> headers)
    {
        foreach (var header in headers)
            response.Headers[header.Key] = header.Value;
        response.Headers.CacheControl = "no-cache";
    }

    [GeneratedRegex("^add_header ([A-Za-z-]+) \"([^\"]*)\" always;$")]
    private static partial Regex HeaderLine();
}
