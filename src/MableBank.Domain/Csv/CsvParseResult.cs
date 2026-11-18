namespace MableBank.Domain.Csv;

/// <summary>A successfully parsed CSV row and the 1-based line it came from.</summary>
public sealed record ParsedRow<T>(int LineNumber, T Value);

/// <summary>A CSV row that could not be parsed, with the reason it was skipped.</summary>
public sealed record CsvParseError(int LineNumber, string RawLine, string Reason);

/// <summary>The valid rows of a CSV file plus any rows that were skipped as malformed.</summary>
public sealed class CsvParseResult<T>
{
    private readonly List<ParsedRow<T>> _rows = [];
    private readonly List<CsvParseError> _errors = [];

    public IReadOnlyList<ParsedRow<T>> Rows => _rows;
    public IReadOnlyList<CsvParseError> Errors => _errors;

    public IEnumerable<T> Values => _rows.Select(row => row.Value);

    internal void AddRow(CsvLine line, T value) => _rows.Add(new ParsedRow<T>(line.Number, value));

    internal void AddError(CsvLine line, string reason) => _errors.Add(new CsvParseError(line.Number, line.Raw, reason));
}
