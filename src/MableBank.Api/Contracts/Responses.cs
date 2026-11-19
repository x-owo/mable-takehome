namespace MableBank.Api.Contracts;

// Money is serialised as a string ("4820.50") so clients never lose cents or trailing zeros.

public sealed record AccountResponse(string AccountNumber, string Balance);

public sealed record ParseErrorResponse(int Line, string Raw, string Reason);

public sealed record LoadBalancesResponse(int Loaded, IReadOnlyList<ParseErrorResponse> Errors);

public sealed record TransferResponse(int Line, string From, string To, string Amount);

public sealed record FailureReasonResponse(string Code, string Message);

public sealed record FailedTransferResponse(int Line, string From, string To, string Amount, FailureReasonResponse Reason);

public sealed record ApplyTransfersResponse(
    IReadOnlyList<TransferResponse> Succeeded,
    IReadOnlyList<FailedTransferResponse> Failed,
    IReadOnlyList<ParseErrorResponse> ParseErrors,
    IReadOnlyList<AccountResponse> Balances);
