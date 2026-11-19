using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;
using MableBank.Domain.Transfers;

namespace MableBank.Tests.Domain.Transfers;

public class TransferProcessorTests
{
    private static readonly AccountNumber Alice = new("1111234522226789");
    private static readonly AccountNumber Bob = new("1212343433335665");
    private static readonly AccountNumber Stranger = new("9999999999999999");

    private static AccountBook BookWith(decimal aliceBalance, decimal bobBalance) =>
        new([new Account(Alice, new Money(aliceBalance)), new Account(Bob, new Money(bobBalance))]);

    private static ParsedRow<Transfer> Row(int line, AccountNumber from, AccountNumber to, decimal amount) =>
        new(line, new Transfer(from, to, new Money(amount)));

    private static Money BalanceOf(AccountBook book, AccountNumber number) => book.Find(number)!.Balance;

    public class Process
    {
        [Test]
        public void applies_transfers_and_tags_each_outcome_with_its_line_number()
        {
            var book = BookWith(100m, 0m);

            var results = new TransferProcessor().Process(book, [Row(1, Alice, Bob, 40m), Row(3, Bob, Alice, 10m)]);

            results.Select(r => (r.LineNumber, r.Outcome.Succeeded)).ShouldBe([(1, true), (3, true)]);
            BalanceOf(book, Alice).ShouldBe(new Money(70m));
            BalanceOf(book, Bob).ShouldBe(new Money(30m));
        }

        [Test]
        public void applies_transfers_in_order_so_earlier_deposits_can_fund_later_withdrawals()
        {
            var book = BookWith(0m, 50m);

            var results = new TransferProcessor().Process(book, [Row(1, Bob, Alice, 50m), Row(2, Alice, Bob, 50m)]);

            results.ShouldAllBe(r => r.Outcome.Succeeded);
        }

        [Test]
        public void skips_a_transfer_that_would_overdraw_and_carries_on_with_the_rest()
        {
            var book = BookWith(10m, 0m);

            var results = new TransferProcessor().Process(book, [Row(1, Alice, Bob, 50m), Row(2, Alice, Bob, 5m)]);

            results[0].Outcome.FailureReason.ShouldBe(TransferFailureReason.InsufficientFunds);
            results[1].Outcome.Succeeded.ShouldBeTrue();
            BalanceOf(book, Alice).ShouldBe(new Money(5m));
        }

        [Test]
        public void fails_a_transfer_from_an_account_the_company_does_not_hold() =>
            new TransferProcessor().Process(BookWith(10m, 10m), [Row(1, Stranger, Bob, 5m)])
                .Single().Outcome.FailureReason.ShouldBe(TransferFailureReason.UnknownSourceAccount);

        [Test]
        public void fails_a_transfer_to_an_account_the_company_does_not_hold_and_moves_no_money()
        {
            var book = BookWith(10m, 10m);

            var result = new TransferProcessor().Process(book, [Row(1, Alice, Stranger, 5m)]).Single();

            result.Outcome.FailureReason.ShouldBe(TransferFailureReason.UnknownDestinationAccount);
            BalanceOf(book, Alice).ShouldBe(new Money(10m));
        }

        [Test]
        public void returns_nothing_when_there_are_no_transfers() =>
            new TransferProcessor().Process(BookWith(10m, 10m), []).ShouldBeEmpty();
    }

    public class MableSampleFiles
    {
        [Test]
        public void applies_all_four_example_transfers_to_produce_the_expected_closing_balances()
        {
            using var balances = File.OpenText(TestFiles.Path("mable_account_balances.csv"));
            using var transfers = File.OpenText(TestFiles.Path("mable_transactions.csv"));
            var book = new AccountBook(new BalancesCsvParser().Parse(balances).Values);

            var results = new TransferProcessor().Process(book, new TransfersCsvParser().Parse(transfers).Rows);

            results.ShouldAllBe(r => r.Outcome.Succeeded);
            book.Accounts.Select(a => (a.Number.Value, a.Balance.ToString())).ShouldBe(
            [
                ("1111234522226789", "4820.50"),
                ("1111234522221234", "9974.40"),
                ("2222123433331212", "1550.00"),
                ("1212343433335665", "1725.60"),
                ("3212343433335755", "48679.50"),
            ]);
        }
    }
}
