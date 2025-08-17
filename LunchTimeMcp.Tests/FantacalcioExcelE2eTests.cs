using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LunchTimeMCP.Tests;

public class FantacalcioExcelE2eTests
{
    //[Fact(Skip="Network call; enable manually")] // Remove Skip to run real E2E
    [Fact]
    public async Task DownloadPricesAsCsv_ProducesFileWithHeaderAndRows()
    {
        var path = await FantacalcioExcelService.DownloadPricesAsCsvAsync();
        Assert.True(File.Exists(path), "CSV file should exist");

        var lines = await File.ReadAllLinesAsync(path);
        Assert.True(lines.Length > 1, "Should contain header + at least one data line");
        Assert.Equal("Role,Player,Team,InitialPrice,CurrentPrice,FVM", lines[0]);
        Assert.Contains(lines.Skip(1), l => l.Split(',').Length >= 3);
    }
}
