using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;

namespace MableBank.Domain.Transfers;

/// <summary>
/// Applies a day's transfers to a company's accounts in file order. A transfer that breaks a
/// business rule is recorded as failed and processing carries on with the next one.
/// </summary>
public sealed class TransferProcessor
{
    public IReadOnlyList<ProcessedTransfer> Process(AccountBook book, IEnumerable<ParsedRow<Transfer>> transfers) =>
        transfers
            .Select(row => new ProcessedTransfer(row.LineNumber, Apply(book, row.Value)))
            .ToList();

    private static TransferOutcome Apply(AccountBook book, Transfer transfer)
    {
        var source = book.Find(transfer.From);
        if (source is null)
            return TransferOutcome.Failure(transfer, TransferFailureReason.UnknownSourceAccount);

        var destination = book.Find(transfer.To);
        if (destination is null)
            return TransferOutcome.Failure(transfer, TransferFailureReason.UnknownDestinationAccount);

        return transfer.ExecuteOn(source, destination);
    }
}
