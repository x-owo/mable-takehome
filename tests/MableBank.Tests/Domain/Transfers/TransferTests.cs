using MableBank.Domain.Accounts;
using MableBank.Domain.Transfers;

namespace MableBank.Tests.Domain.Transfers;

public class TransferTests
{
    private static readonly AccountNumber Alice = new("1111234522226789");
    private static readonly AccountNumber Bob = new("1212343433335665");

    public class Constructor
    {
        [TestCase(0)]
        [TestCase(-500)]
        public void rejects_amounts_that_are_not_positive(decimal amount) =>
            Should.Throw<ArgumentOutOfRangeException>(() => new Transfer(Alice, Bob, new Money(amount)));
    }

    public class ExecuteOn
    {
        private Account _alice = null!;
        private Account _bob = null!;

        [SetUp]
        public void OpenAccounts()
        {
            _alice = new Account(Alice, new Money(5000m));
            _bob = new Account(Bob, new Money(1200m));
        }

        [Test]
        public void moves_the_amount_from_source_to_destination()
        {
            var outcome = new Transfer(Alice, Bob, new Money(500m)).ExecuteOn(_alice, _bob);

            outcome.Succeeded.ShouldBeTrue();
            _alice.Balance.ShouldBe(new Money(4500m));
            _bob.Balance.ShouldBe(new Money(1700m));
        }

        [Test]
        public void fails_with_insufficient_funds_and_moves_no_money_when_source_would_go_below_zero()
        {
            var outcome = new Transfer(Alice, Bob, new Money(5000.01m)).ExecuteOn(_alice, _bob);

            outcome.FailureReason.ShouldBe(TransferFailureReason.InsufficientFunds);
            _alice.Balance.ShouldBe(new Money(5000m));
            _bob.Balance.ShouldBe(new Money(1200m));
        }

        [Test]
        public void fails_when_transferring_to_the_same_account()
        {
            var outcome = new Transfer(Alice, Alice, new Money(10m)).ExecuteOn(_alice, _alice);

            outcome.FailureReason.ShouldBe(TransferFailureReason.SameAccount);
            _alice.Balance.ShouldBe(new Money(5000m));
        }

        [Test]
        public void reports_the_transfer_it_attempted()
        {
            var transfer = new Transfer(Alice, Bob, new Money(1m));

            transfer.ExecuteOn(_alice, _bob).Transfer.ShouldBe(transfer);
        }

        [Test]
        public void throws_if_given_accounts_that_do_not_match_the_transfer() =>
            Should.Throw<ArgumentException>(() => new Transfer(Alice, Bob, new Money(1m)).ExecuteOn(_bob, _alice));
    }
}
