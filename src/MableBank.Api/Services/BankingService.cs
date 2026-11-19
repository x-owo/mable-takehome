using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;
using MableBank.Domain.Transfers;

namespace MableBank.Api.Services;

/// <summary>The outcome of applying a day's transfers CSV to a company's accounts.</summary>
public sealed record TransferBatchResult(
    IReadOnlyList<ProcessedTransfer> Processed,
    IReadOnlyList<CsvParseError> ParseErrors,
    AccountBook Book);

/// <summary>Coordinates parsing, processing and storage for the API's use cases.</summary>
public sealed class BankingService(
    IAccountBookRepository repository,
    BalancesCsvParser balancesParser,
    TransfersCsvParser transfersParser,
    TransferProcessor processor)
{
    /// <summary>Replaces the company's accounts with the valid rows of a balances CSV.</summary>
    public CsvParseResult<Account> LoadBalances(string companyId, TextReader csv)
    {
        var parsed = balancesParser.Parse(csv);
        repository.Save(companyId, new AccountBook(parsed.Values));
        return parsed;
    }

    public AccountBook? FindAccounts(string companyId) => repository.Find(companyId);

    /// <summary>Applies the valid rows of a transfers CSV, or returns null if the company has no accounts loaded.</summary>
    public TransferBatchResult? ApplyTransfers(string companyId, TextReader csv)
    {
        var book = repository.Find(companyId);
        if (book is null)
            return null;

        var parsed = transfersParser.Parse(csv);
        var processed = processor.Process(book, parsed.Rows);
        return new TransferBatchResult(processed, parsed.Errors, book);
    }
}
