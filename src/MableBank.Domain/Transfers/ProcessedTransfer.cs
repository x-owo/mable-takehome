namespace MableBank.Domain.Transfers;

/// <summary>The outcome of a transfer, together with the CSV line it was read from.</summary>
public sealed record ProcessedTransfer(int LineNumber, TransferOutcome Outcome);
