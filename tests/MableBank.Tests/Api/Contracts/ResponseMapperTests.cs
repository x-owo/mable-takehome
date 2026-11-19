using MableBank.Api.Contracts;
using MableBank.Api.Services;
using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;
using MableBank.Domain.Transfers;

namespace MableBank.Tests.Api.Contracts;

public class ResponseMapperTests
{
    private static readonly AccountNumber Alice = new("1111234522226789");
    private static readonly AccountNumber Bob = new("1212343433335665");
    private static readonly Transfer AliceToBob = new(Alice, Bob, new Money(500m));

    public class Accounts
    {
        [Test]
        public void formats_the_balance_as_a_two_decimal_string() =>
            ResponseMapper.ToResponse(new Account(Alice, new Money(4820.5m)))
                .ShouldBe(new AccountResponse("1111234522226789", "4820.50"));
    }

    public class TransferBatch
    {
        private static ApplyTransfersResponse Map(params ProcessedTransfer[] processed) =>
            ResponseMapper.ToResponse(new TransferBatchResult(
                processed,
                [new CsvParseError(9, "bad", "Expected 3 columns")],
                new AccountBook([new Account(Alice, new Money(1m))])));

        [Test]
        public void splits_succeeded_and_failed_transfers_keeping_their_line_numbers()
        {
            var response = Map(
                new ProcessedTransfer(1, TransferOutcome.Success(AliceToBob)),
                new ProcessedTransfer(2, TransferOutcome.Failure(AliceToBob, TransferFailureReason.InsufficientFunds)));

            response.Succeeded.ShouldHaveSingleItem().ShouldBe(new TransferResponse(1, Alice.Value, Bob.Value, "500.00"));
            response.Failed.ShouldHaveSingleItem().Line.ShouldBe(2);
        }

        [Test]
        public void gives_failed_transfers_a_reason_code_and_a_readable_message()
        {
            var reason = Map(new ProcessedTransfer(1, TransferOutcome.Failure(AliceToBob, TransferFailureReason.InsufficientFunds)))
                .Failed.Single().Reason;

            reason.Code.ShouldBe("InsufficientFunds");
            reason.Message.ShouldBe("Account 1111234522226789 has insufficient funds to transfer 500.00.");
        }

        [Test]
        public void includes_parse_errors_and_closing_balances()
        {
            var response = Map();

            response.ParseErrors.ShouldHaveSingleItem().ShouldBe(new ParseErrorResponse(9, "bad", "Expected 3 columns"));
            response.Balances.ShouldHaveSingleItem().Balance.ShouldBe("1.00");
        }
    }

    public class Describe
    {
        [Test]
        public void has_a_message_for_every_failure_reason()
        {
            foreach (var reason in Enum.GetValues<TransferFailureReason>())
                ResponseMapper.Describe(reason, AliceToBob).ShouldNotBeNullOrWhiteSpace();
        }
    }
}
