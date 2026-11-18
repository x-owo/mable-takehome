namespace MableBank.Domain.Accounts;

/// <summary>A customer account whose balance may never be reduced below $0 by a withdrawal.</summary>
public sealed class Account
{
    public AccountNumber Number { get; }
    public Money Balance { get; private set; }

    public Account(AccountNumber number, Money openingBalance)
    {
        Number = number;
        Balance = openingBalance;
    }

    public bool CanWithdraw(Money amount) => Balance - amount >= Money.Zero;

    public void Withdraw(Money amount)
    {
        EnsurePositive(amount);
        if (!CanWithdraw(amount))
            throw new InvalidOperationException(
                $"Withdrawing {amount} would take account {Number} below $0 (balance {Balance}).");

        Balance -= amount;
    }

    public void Deposit(Money amount)
    {
        EnsurePositive(amount);
        Balance += amount;
    }

    private static void EnsurePositive(Money amount)
    {
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than $0.");
    }
}
