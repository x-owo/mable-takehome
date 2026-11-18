namespace MableBank.Domain.Csv;

/// <summary>A single non-blank line of a CSV file split into its comma-separated fields.</summary>
internal sealed record CsvLine(int Number, string Raw)
{
    public string[] Fields { get; } = Raw.Split(',');

    public bool HasColumnCount(int expected) => Fields.Length == expected;

    /// <summary>Reads every non-blank line, numbering lines from 1 as they appear in the file.</summary>
    public static IEnumerable<CsvLine> ReadAll(TextReader reader)
    {
        var number = 0;
        while (reader.ReadLine() is { } raw)
        {
            number++;
            if (!string.IsNullOrWhiteSpace(raw))
                yield return new CsvLine(number, raw);
        }
    }
}
