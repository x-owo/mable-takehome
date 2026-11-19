using MableBank.Domain.Accounts;

namespace MableBank.Tests.Domain.Accounts;

public class AccountBookTests
{
    private static readonly AccountNumber First = new("1111234522226789");
    private static readonly AccountNumber Second = new("2222123433331212");

    public class Constructor
    {
        [Test]
        public void keeps_accounts_in_the_order_given()
        {
            var book = new AccountBook([new Account(Second, Money.Zero), new Account(First, Money.Zero)]);

            book.Accounts.Select(a => a.Number).ShouldBe([Second, First]);
        }

        [Test]
        public void rejects_the_same_account_number_twice() =>
            Should.Throw<ArgumentException>(() =>
                new AccountBook([new Account(First, Money.Zero), new Account(First, new Money(5m))]));
    }

    public class Find
    {
        [Test]
        public void returns_the_account_with_that_number()
        {
            var account = new Account(First, new Money(5000m));

            new AccountBook([account]).Find(First).ShouldBeSameAs(account);
        }

        [Test]
        public void returns_null_for_an_account_the_company_does_not_hold() =>
            new AccountBook([new Account(First, Money.Zero)]).Find(Second).ShouldBeNull();
    }
}
