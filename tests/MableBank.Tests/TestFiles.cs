namespace MableBank.Tests;

/// <summary>Locates the Mable sample CSVs, which are copied into the test output's SampleData folder.</summary>
internal static class TestFiles
{
    public static string Path(string fileName) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "SampleData", fileName);
}
