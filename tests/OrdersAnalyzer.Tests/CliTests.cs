namespace OrdersAnalyzer.Tests;

public sealed class CliTests : IDisposable
{
    private readonly string _file = Path.GetTempFileName();

    public CliTests()
    {
        File.WriteAllText(_file, """
            [
              { "orderId": 1, "customer": "Nino", "status": "completed",
                "items": [ { "product": "Pen", "quantity": 4, "price": 2.5 } ] },
              { "orderId": 2, "customer": "Ana", "status": "cancelled",
                "items": [ { "product": "Book", "quantity": 9, "price": 10 } ] }
            ]
            """);
    }

    public void Dispose() => File.Delete(_file);

    private static (int Code, string Out, string Err) Run(string stdin, params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = Cli.Run(args, new StringReader(stdin), output, error);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void StatsCommand_PrintsStatistics()
    {
        var (code, output, _) = Run("", "--file", _file, "stats");

        Assert.Equal(0, code);
        Assert.Contains("Completed orders:     1", output);
        Assert.Contains("Total sales:          10.00", output);
        Assert.Contains("Most popular product: Pen (4 units)", output);
    }

    [Fact]
    public void CustomerCommand_AcceptsNameWithoutQuotes()
    {
        var (code, output, _) = Run("", "customer", "ana", "--file", _file);

        Assert.Equal(0, code);
        Assert.Contains("Ana", output);
        Assert.DoesNotContain("Nino", output);
    }

    [Fact]
    public void InteractiveMenu_RunsChosenOptionsUntilExit()
    {
        var (code, output, _) = Run("3\n2\nNino\n0\n", "--file", _file);

        Assert.Equal(0, code);
        Assert.Contains("Total sales:          10.00", output);
        Assert.Contains("Pen x4", output);
    }

    [Fact]
    public void MissingFile_ReturnsErrorCode()
    {
        var (code, _, error) = Run("", "--file", "does-not-exist.json", "list");

        Assert.Equal(1, code);
        Assert.Contains("File not found", error);
    }

    [Fact]
    public void FileOptionWithoutValue_IsAnError_NotSilentlyTheDefaultFile()
    {
        var (code, _, error) = Run("", "stats", "--file");

        Assert.Equal(2, code);
        Assert.Contains("Missing value for --file", error);
    }

    [Fact]
    public void UnknownCommand_PrintsUsage()
    {
        var (code, _, error) = Run("", "--file", _file, "dance");

        Assert.Equal(2, code);
        Assert.Contains("Usage:", error);
    }
}
