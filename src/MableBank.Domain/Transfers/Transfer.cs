using MableBank.Domain.Accounts;

namespace MableBank.Domain.Transfers;

/// <summary>A request to move a positive amount of money from one account to another.</summary>
public sealed record Transfer
{
    public AccountNumber From { get; }
    public AccountNumber To { get; }
    public Money Amount { get; }

    public Transfer(AccountNumber from, AccountNumber to, Money amount)
    {
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Transfer amount must be greater than $0.");

        From = from;
        To = to;
        Amount = amount;
    }

    public bool IsBetweenSameAccount => From == To;

    /// <summary>
    /// Moves the money if the business rules allow it. Validates everything before
    /// changing either balance, so a failed transfer leaves both accounts untouched.
    /// </summary>
    public TransferOutcome ExecuteOn(Account source, Account destination)
    {
        EnsureMatches(source, From, nameof(source));
        EnsureMatches(destination, To, nameof(destination));

        if (IsBetweenSameAccount)
            return TransferOutcome.Failure(this, TransferFailureReason.SameAccount);

        if (!source.CanWithdraw(Amount))
            return TransferOutcome.Failure(this, TransferFailureReason.InsufficientFunds);

        source.Withdraw(Amount);
        destination.Deposit(Amount);
        return TransferOutcome.Success(this);
    }

    private static void EnsureMatches(Account account, AccountNumber expected, string paramName)
    {
        if (account.Number != expected)
            throw new ArgumentException($"Expected account {expected} but got {account.Number}.", paramName);
    }
}
