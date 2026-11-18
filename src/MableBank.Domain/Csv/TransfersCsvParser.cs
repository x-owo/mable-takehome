using MableBank.Domain.Transfers;

namespace MableBank.Domain.Csv;

/// <summary>
/// Parses a day's transfers: one <c>from,to,amount</c> row per transfer, no header.
/// Only the format is checked here; whether the accounts exist is a business rule applied later.
/// </summary>
public sealed class TransfersCsvParser
{
    private const int ColumnCount = 3;

    public CsvParseResult<Transfer> Parse(TextReader reader)
    {
        var result = new CsvParseResult<Transfer>();

        foreach (var line in CsvLine.ReadAll(reader))
        {
            if (TryParseTransfer(line, out var transfer, out var error))
                result.AddRow(line, transfer);
            else
                result.AddError(line, error);
        }

        return result;
    }

    private static bool TryParseTransfer(CsvLine line, out Transfer transfer, out string error)
    {
        transfer = null!;

        if (!line.HasColumnCount(ColumnCount))
        {
            error = $"Expected {ColumnCount} columns (from,to,amount) but found {line.Fields.Length}.";
            return false;
        }

        if (!CsvFields.TryParseAccountNumber(line.Fields[0], "From account", out var from, out var fromError))
        {
            error = fromError;
            return false;
        }

        if (!CsvFields.TryParseAccountNumber(line.Fields[1], "To account", out var to, out var toError))
        {
            error = toError;
            return false;
        }

        if (!CsvFields.TryParseMoney(line.Fields[2], "Amount", out var amount, out var amountError))
        {
            error = amountError;
            return false;
        }

        if (!amount.IsPositive)
        {
            error = $"Amount '{line.Fields[2]}' must be greater than $0.";
            return false;
        }

        transfer = new Transfer(from, to, amount);
        error = string.Empty;
        return true;
    }
}
