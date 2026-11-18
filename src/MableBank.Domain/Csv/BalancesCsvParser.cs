using MableBank.Domain.Accounts;

namespace MableBank.Domain.Csv;

/// <summary>
/// Parses a company's starting balances: one <c>account,balance</c> row per account, no header.
/// Malformed and duplicate rows are skipped and reported.
/// </summary>
public sealed class BalancesCsvParser
{
    private const int ColumnCount = 2;

    public CsvParseResult<Account> Parse(TextReader reader)
    {
        var result = new CsvParseResult<Account>();
        var firstSeenOn = new Dictionary<AccountNumber, int>();

        foreach (var line in CsvLine.ReadAll(reader))
        {
            if (!TryParseAccount(line, out var account, out var error))
                result.AddError(line, error);
            else if (!firstSeenOn.TryAdd(account.Number, line.Number))
                result.AddError(line, $"Duplicate account {account.Number} (first seen on line {firstSeenOn[account.Number]}).");
            else
                result.AddRow(line, account);
        }

        return result;
    }

    private static bool TryParseAccount(CsvLine line, out Account account, out string error)
    {
        account = null!;

        if (!line.HasColumnCount(ColumnCount))
        {
            error = $"Expected {ColumnCount} columns (account,balance) but found {line.Fields.Length}.";
            return false;
        }

        if (!CsvFields.TryParseAccountNumber(line.Fields[0], "Account", out var number, out var numberError))
        {
            error = numberError;
            return false;
        }

        if (!CsvFields.TryParseMoney(line.Fields[1], "Balance", out var balance, out var balanceError))
        {
            error = balanceError;
            return false;
        }

        account = new Account(number, balance);
        error = string.Empty;
        return true;
    }
}
