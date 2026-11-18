using MableBank.Domain.Accounts;

namespace MableBank.Tests.Domain.Accounts;

public class AccountTests
{
    private static Account AccountWithBalance(decimal balance) =>
        new(new AccountNumber("1111234522226789"), new Money(balance));

    public class Withdraw
    {
        [Test]
        public void reduces_the_balance_by_the_amount()
        {
            var account = AccountWithBalance(5000m);

            account.Withdraw(new Money(500m));

            account.Balance.ShouldBe(new Money(4500m));
        }

        [Test]
        public void allows_the_balance_to_reach_exactly_zero()
        {
            var account = AccountWithBalance(550m);

            account.Withdraw(new Money(550m));

            account.Balance.ShouldBe(Money.Zero);
        }

        [Test]
        public void throws_and_leaves_the_balance_unchanged_when_it_would_go_below_zero()
        {
            var account = AccountWithBalance(100m);

            Should.Throw<InvalidOperationException>(() => account.Withdraw(new Money(100.01m)));
            account.Balance.ShouldBe(new Money(100m));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void rejects_amounts_that_are_not_positive(decimal amount) =>
            Should.Throw<ArgumentOutOfRangeException>(() => AccountWithBalance(100m).Withdraw(new Money(amount)));
    }

    public class CanWithdraw
    {
        [TestCase(100, true)]
        [TestCase(99.99, true)]
        [TestCase(100.01, false)]
        public void is_true_only_if_the_balance_would_stay_at_or_above_zero(decimal amount, bool expected) =>
            AccountWithBalance(100m).CanWithdraw(new Money(amount)).ShouldBe(expected);
    }

    public class Deposit
    {
        [Test]
        public void increases_the_balance_by_the_amount()
        {
            var account = AccountWithBalance(1200m);

            account.Deposit(new Money(25.60m));

            account.Balance.ShouldBe(new Money(1225.60m));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void rejects_amounts_that_are_not_positive(decimal amount) =>
            Should.Throw<ArgumentOutOfRangeException>(() => AccountWithBalance(100m).Deposit(new Money(amount)));
    }
}
