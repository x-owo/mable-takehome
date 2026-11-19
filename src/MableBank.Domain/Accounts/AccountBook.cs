namespace MableBank.Domain.Accounts;

/// <summary>The set of accounts a single company holds with the bank, looked up by account number.</summary>
public sealed class AccountBook
{
    private readonly List<Account> _accounts = [];
    private readonly Dictionary<AccountNumber, Account> _accountsByNumber = [];

    public AccountBook(IEnumerable<Account> accounts)
    {
        foreach (var account in accounts)
        {
            if (!_accountsByNumber.TryAdd(account.Number, account))
                throw new ArgumentException($"Account {account.Number} appears more than once.", nameof(accounts));

            _accounts.Add(account);
        }
    }

    /// <summary>All accounts, in the order they were opened.</summary>
    public IReadOnlyList<Account> Accounts => _accounts;

    public Account? Find(AccountNumber number) => _accountsByNumber.GetValueOrDefault(number);
}
