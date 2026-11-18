namespace MableBank.Domain.Transfers;

public enum TransferFailureReason
{
    InsufficientFunds,
    SameAccount,
    UnknownSourceAccount,
    UnknownDestinationAccount,
}
