using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace LunchTimeMCP;

public static class FantacalcioExcelService
{
    // Endpoint pattern used by site for Excel export (year and competition may vary)
    // Example discovered earlier; adjust if season changes.
    private const string DefaultExcelUrl = "https://www.fantacalcio.it/api/v1/Excel/prices/20/1"; // 20=season id? 1=classic

    private static readonly HttpClient Http = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    });

    public static async Task<string> DownloadPricesAsCsvAsync(string? excelUrl = null, string? outputFile = null)
    {
        var url = excelUrl ?? DefaultExcelUrl;
        var bytes = await Http.GetByteArrayAsync(url);

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheet(1);

        // Heuristic: find header row by looking for a cell named "GIOCATORE" or "CALCIATORE"
        var headerRow = ws.FirstRowUsed();
        var firstRow = headerRow.RowUsed();

        var headers = new List<string>();
        foreach (var cell in firstRow.Cells())
        {
            headers.Add(cell.GetString().Trim());
        }

        // Map columns we care about (flexible if order shifts)
        int idxPlayer = IndexOf(headers, "GIOCATORE", "CALCIATORE", "NOME");
        int idxTeam = IndexOf(headers, "SQUADRA", "TEAM");
        int idxRole = IndexOf(headers, "R", "RUOLO", "ROLE");
        int idxInitial = IndexOf(headers, "INIZIALE", "QUOT INIZ", "QUOT_INIZ");
        int idxCurrent = IndexOf(headers, "ATTUALE", "QUOT", "QUOT ATT");
        int idxFvm = IndexOf(headers, "FVM");

        var sb = new StringBuilder();
        sb.AppendLine("Role,Player,Team,InitialPrice,CurrentPrice,FVM");

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            string Get(int idx)
                => idx >= 0 ? row.Cell(idx + 1).GetString().Trim() : string.Empty;

            var role = NormalizeRole(Get(idxRole));
            var player = Get(idxPlayer);
            if (string.IsNullOrWhiteSpace(player)) continue;
            var team = Get(idxTeam);
            var initial = Get(idxInitial);
            var current = Get(idxCurrent);
            var fvm = Get(idxFvm);

            sb.AppendLine(string.Join(',', Csv(role), Csv(player), Csv(team), Csv(initial), Csv(current), Csv(fvm)));
        }

        var file = outputFile ?? Path.Combine(AppContext.BaseDirectory, $"fantacalcio_excel_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
        return file;
    }

    private static int IndexOf(List<string> headers, params string[] options)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            foreach (var opt in options)
            {
                if (string.Equals(headers[i], opt, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        return -1;
    }

    private static string NormalizeRole(string raw)
    {
        raw = (raw ?? string.Empty).Trim().ToUpperInvariant();
        return raw switch
        {
            "P" or "POR" => "P",
            "D" or "DC" or "DD" or "DS" => "D",
            "C" or "E" or "M" or "T" => "C",
            "A" or "AP" or "PC" or "SP" => "A",
            _ => raw
        };
    }

    private static string Csv(string v)
    {
        if (v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }
}
