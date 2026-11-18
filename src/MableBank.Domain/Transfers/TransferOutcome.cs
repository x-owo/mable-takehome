namespace MableBank.Domain.Transfers;

/// <summary>The result of attempting a transfer: it either succeeded or failed for a known business reason.</summary>
public sealed record TransferOutcome
{
    public Transfer Transfer { get; }
    public TransferFailureReason? FailureReason { get; }

    public bool Succeeded => FailureReason is null;

    private TransferOutcome(Transfer transfer, TransferFailureReason? failureReason)
    {
        Transfer = transfer;
        FailureReason = failureReason;
    }

    public static TransferOutcome Success(Transfer transfer) => new(transfer, null);

    public static TransferOutcome Failure(Transfer transfer, TransferFailureReason reason) => new(transfer, reason);
}
