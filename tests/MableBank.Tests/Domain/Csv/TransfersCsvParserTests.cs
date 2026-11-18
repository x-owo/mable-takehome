using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;
using MableBank.Domain.Transfers;

namespace MableBank.Tests.Domain.Csv;

public class TransfersCsvParserTests
{
    private static CsvParseResult<Transfer> Parse(string csv) =>
        new TransfersCsvParser().Parse(new StringReader(csv));

    public class ValidRows
    {
        [Test]
        public void reads_from_to_and_amount_in_file_order()
        {
            var result = Parse("1111234522226789,1212343433335665,500.00\n3212343433335755,2222123433331212,1000.00");

            result.Errors.ShouldBeEmpty();
            result.Values.ShouldBe(
            [
                new Transfer(new("1111234522226789"), new("1212343433335665"), new Money(500m)),
                new Transfer(new("3212343433335755"), new("2222123433331212"), new Money(1000m)),
            ]);
        }

        [Test]
        public void records_the_line_number_of_each_row() =>
            Parse("\n1111234522226789,1212343433335665,500.00").Rows.Single().LineNumber.ShouldBe(2);

        [Test]
        public void leaves_account_existence_and_same_account_checks_to_the_business_rules() =>
            Parse("1111234522226789,1111234522226789,5.00").Rows.Count.ShouldBe(1);
    }

    public class MalformedRows
    {
        [TestCase("1111234522226789,1212343433335665", "Expected 3 columns")]
        [TestCase("1111234522226789,1212343433335665,5.00,x", "Expected 3 columns")]
        [TestCase("111123452222678,1212343433335665,5.00", "From account")]
        [TestCase("1111234522226789,121234343333566X,5.00", "To account")]
        [TestCase("1111234522226789,1212343433335665,five", "is not a valid amount")]
        [TestCase("1111234522226789,1212343433335665,5.555", "more than 2 decimal places")]
        [TestCase("1111234522226789,1212343433335665,0.00", "must be greater than $0")]
        [TestCase("1111234522226789,1212343433335665,-5.00", "must be greater than $0")]
        public void are_skipped_and_reported_with_a_reason(string line, string expectedReason)
        {
            var result = Parse(line);

            result.Rows.ShouldBeEmpty();
            result.Errors.ShouldHaveSingleItem().Reason.ShouldContain(expectedReason);
        }

        [Test]
        public void do_not_stop_later_valid_rows_from_parsing()
        {
            var result = Parse("bad\n1111234522226789,1212343433335665,500.00");

            result.Rows.Single().LineNumber.ShouldBe(2);
            result.Errors.Single().LineNumber.ShouldBe(1);
        }
    }

    public class SampleFile
    {
        [Test]
        public void parses_all_four_transfers_from_the_mable_example()
        {
            using var reader = File.OpenText(TestFiles.Path("mable_transactions.csv"));

            var result = new TransfersCsvParser().Parse(reader);

            result.Errors.ShouldBeEmpty();
            result.Rows.Count.ShouldBe(4);
        }
    }
}
