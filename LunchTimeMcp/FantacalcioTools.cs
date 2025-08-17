using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace LunchTimeMCP;

[McpServerToolType]
public sealed class FantacalcioTools
{
    [McpServerTool, Description("Scrape all Fantacalcio player quotations into a CSV. Returns the CSV file path.")]
    public async Task<string> ScrapeAsync(
        [Description("Optional override URL")] string? url = null)
    {
        return await FantacalcioHtmlScraper.DownloadCsvAsync(url);
    }

    [McpServerTool, Description("Download Fantacalcio official Excel quotations and convert to CSV. Returns file path.")]
    public async Task<string> ScrapeExcelAsync(
        [Description("Optional Excel API URL override")] string? excelUrl = null)
    {
        return await FantacalcioExcelService.DownloadPricesAsCsvAsync(excelUrl);
    }
}