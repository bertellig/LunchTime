using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

namespace LunchTimeMCP;

public static class FantacalcioHtmlScraper
{
    private const string Url = "https://www.fantacalcio.it/quotazioni-fantacalcio";

    private static readonly HttpClient Http = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    });

    public static async Task<string> DownloadCsvAsync(string? url = null, string? outputFile = null)
    {
        var targetUrl = url ?? Url;
        var html = await Http.GetStringAsync(targetUrl);

        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html));

        // Attempt to select player rows (client-side pagination just hides rows)
        var primary = doc.QuerySelectorAll("tr.player-row");
        var rows = primary.Length > 0
            ? primary.Select(e => e).ToList()
            : doc.QuerySelectorAll("table.pills-table tr")
                .Where(r => !string.IsNullOrWhiteSpace(r.GetAttribute("data-filter-role-classic")))
                .Select(e => e).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Role,Player,Team,InitialPrice,CurrentPrice,FVM,MantraRoles,OutOfGame");

        foreach (var row in rows)
        {
            var roleClassic = (row.GetAttribute("data-filter-role-classic") ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(roleClassic)) continue;

            // Map classic role to P/D/C/A
            var role = roleClassic switch
            {
                "p" => "P",
                "d" => "D",
                "c" => "C",
                "a" => "A",
                _ => roleClassic.ToUpperInvariant()
            };

            var name = row.QuerySelector("th.player-name span")?.TextContent?.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var outOfGame = row.QuerySelector("th.player-name .out-of-game") != null ? "Y" : "N";

            string GetText(string selector) => row.QuerySelector(selector)?.TextContent?.Trim() ?? "";

            var team = GetText("td.player-team");
            var initPrice = GetText("td.player-classic-initial-price");
            var currentPrice = GetText("td.player-classic-current-price");
            var fvm = GetText("td.player-classic-fvm");

            // Mantra roles: either data attribute (pipe-separated) or span.role list
            var mantra = (row.GetAttribute("data-filter-role-mantra") ?? "")
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (mantra.Length == 0)
            {
                mantra = row.QuerySelectorAll("th.player-role span.role")
                            .Select(s => s.GetAttribute("data-value") ?? "")
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray();
            }

            var mantraJoined = string.Join("/", mantra);

            sb.AppendLine(string.Join(",",
                Csv(role),          // Role
                Csv(name),          // Player
                Csv(team),          // Team
                Csv(initPrice),     // InitialPrice
                Csv(currentPrice),  // CurrentPrice
                Csv(fvm),           // FVM
                Csv(mantraJoined),  // MantraRoles
                Csv(outOfGame)      // OutOfGame
            ));
        }

        var file = outputFile ??
                   Path.Combine(AppContext.BaseDirectory,
                       $"fantacalcio_players_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");

        await File.WriteAllTextAsync(file, sb.ToString(), Encoding.UTF8);
        return file;
    }

    private static string Csv(string? v)
    {
        v ??= "";
        if (v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }
}