using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;

namespace MableBank.Tests.Domain.Csv;

public class BalancesCsvParserTests
{
    private static CsvParseResult<Account> Parse(string csv) =>
        new BalancesCsvParser().Parse(new StringReader(csv));

    public class ValidRows
    {
        [Test]
        public void loads_each_account_with_its_opening_balance()
        {
            var result = Parse("1111234522226789,5000.00\n2222123433331212,550.00");

            result.Errors.ShouldBeEmpty();
            result.Values.Select(a => (a.Number.Value, a.Balance)).ShouldBe(
            [
                ("1111234522226789", new Money(5000m)),
                ("2222123433331212", new Money(550m)),
            ]);
        }

        [Test]
        public void records_the_line_number_of_each_row()
        {
            var result = Parse("1111234522226789,5000.00\n\n2222123433331212,550.00");

            result.Rows.Select(r => r.LineNumber).ShouldBe([1, 3]);
        }

        [Test]
        public void accepts_windows_line_endings() =>
            Parse("1111234522226789,5000.00\r\n2222123433331212,550.00\r\n").Rows.Count.ShouldBe(2);

        [Test]
        public void accepts_negative_opening_balances() =>
            Parse("1111234522226789,-20.00").Values.Single().Balance.ShouldBe(new Money(-20m));

        [Test]
        public void ignores_blank_lines()
        {
            var result = Parse("\n   \n1111234522226789,5000.00\n\n");

            result.Rows.Count.ShouldBe(1);
            result.Errors.ShouldBeEmpty();
        }

        [Test]
        public void returns_nothing_for_an_empty_file()
        {
            var result = Parse("");

            result.Rows.ShouldBeEmpty();
            result.Errors.ShouldBeEmpty();
        }
    }

    public class MalformedRows
    {
        [TestCase("1111234522226789", "Expected 2 columns")]
        [TestCase("1111234522226789,5000.00,extra", "Expected 2 columns")]
        [TestCase("111123452222678,5000.00", "must be exactly 16 digits")]
        [TestCase("1111234522226789,abc", "is not a valid amount")]
        [TestCase("1111234522226789,$5000.00", "is not a valid amount")]
        [TestCase("1111234522226789,5000.001", "more than 2 decimal places")]
        [TestCase(" 1111234522226789,5000.00", "must be exactly 16 digits")]
        public void are_skipped_and_reported_with_a_reason(string line, string expectedReason)
        {
            var result = Parse(line);

            result.Rows.ShouldBeEmpty();
            var error = result.Errors.ShouldHaveSingleItem();
            error.Reason.ShouldContain(expectedReason);
        }

        [Test]
        public void are_reported_with_their_line_number_and_raw_text()
        {
            var result = Parse("1111234522226789,5000.00\nnot,a,row");

            result.Errors.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
                e => e.LineNumber.ShouldBe(2),
                e => e.RawLine.ShouldBe("not,a,row"));
        }

        [Test]
        public void do_not_stop_later_valid_rows_from_loading()
        {
            var result = Parse("garbage\n1111234522226789,5000.00");

            result.Rows.Count.ShouldBe(1);
            result.Errors.Count.ShouldBe(1);
        }
    }

    public class SampleFile
    {
        [Test]
        public void loads_all_five_accounts_from_the_mable_example()
        {
            using var reader = File.OpenText(TestFiles.Path("mable_account_balances.csv"));

            var result = new BalancesCsvParser().Parse(reader);

            result.Errors.ShouldBeEmpty();
            result.Rows.Count.ShouldBe(5);
        }
    }

    public class DuplicateAccounts
    {
        [Test]
        public void keeps_the_first_row_and_reports_later_ones()
        {
            var result = Parse("1111234522226789,5000.00\n1111234522226789,10.00");

            result.Values.Single().Balance.ShouldBe(new Money(5000m));
            var error = result.Errors.ShouldHaveSingleItem();
            error.LineNumber.ShouldBe(2);
            error.Reason.ShouldContain("first seen on line 1");
        }
    }
}
