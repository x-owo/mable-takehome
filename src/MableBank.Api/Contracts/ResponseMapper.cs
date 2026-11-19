using MableBank.Api.Services;
using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;
using MableBank.Domain.Transfers;

namespace MableBank.Api.Contracts;

/// <summary>Turns domain results into the JSON shapes the API returns.</summary>
public static class ResponseMapper
{
    public static AccountResponse ToResponse(Account account) =>
        new(account.Number.Value, account.Balance.ToString());

    public static IReadOnlyList<AccountResponse> ToResponse(AccountBook book) =>
        book.Accounts.Select(ToResponse).ToList();

    public static LoadBalancesResponse ToResponse(CsvParseResult<Account> parsed) =>
        new(parsed.Rows.Count, ToResponse(parsed.Errors));

    public static ApplyTransfersResponse ToResponse(TransferBatchResult result) =>
        new(
            Succeeded: result.Processed.Where(p => p.Outcome.Succeeded).Select(ToSucceeded).ToList(),
            Failed: result.Processed.Where(p => !p.Outcome.Succeeded).Select(ToFailed).ToList(),
            ParseErrors: ToResponse(result.ParseErrors),
            Balances: ToResponse(result.Book));

    private static IReadOnlyList<ParseErrorResponse> ToResponse(IEnumerable<CsvParseError> errors) =>
        errors.Select(e => new ParseErrorResponse(e.LineNumber, e.RawLine, e.Reason)).ToList();

    private static TransferResponse ToSucceeded(ProcessedTransfer processed)
    {
        var transfer = processed.Outcome.Transfer;
        return new TransferResponse(processed.LineNumber, transfer.From.Value, transfer.To.Value, transfer.Amount.ToString());
    }

    private static FailedTransferResponse ToFailed(ProcessedTransfer processed)
    {
        var transfer = processed.Outcome.Transfer;
        var reason = processed.Outcome.FailureReason!.Value;
        return new FailedTransferResponse(
            processed.LineNumber,
            transfer.From.Value,
            transfer.To.Value,
            transfer.Amount.ToString(),
            new FailureReasonResponse(reason.ToString(), Describe(reason, transfer)));
    }

    public static string Describe(TransferFailureReason reason, Transfer transfer) => reason switch
    {
        TransferFailureReason.InsufficientFunds =>
            $"Account {transfer.From} has insufficient funds to transfer {transfer.Amount}.",
        TransferFailureReason.SameAccount =>
            $"Cannot transfer from account {transfer.From} to itself.",
        TransferFailureReason.UnknownSourceAccount =>
            $"Source account {transfer.From} is not held by this company.",
        TransferFailureReason.UnknownDestinationAccount =>
            $"Destination account {transfer.To} is not held by this company.",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unhandled transfer failure reason."),
    };
}
