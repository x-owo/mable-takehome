using MableBank.Api.Services;
using MableBank.Api.Storage;
using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;
using MableBank.Domain.Transfers;

namespace MableBank.Tests.Api.Services;

public class BankingServiceTests
{
    private const string Company = "alpha-sales";

    private static BankingService NewService() =>
        new(new InMemoryAccountBookRepository(), new BalancesCsvParser(), new TransfersCsvParser(), new TransferProcessor());

    private static TextReader Csv(string text) => new StringReader(text);

    public class LoadBalances
    {
        [Test]
        public void stores_the_valid_accounts_for_the_company()
        {
            var service = NewService();

            service.LoadBalances(Company, Csv("1111234522226789,5000.00\nbad"));

            service.FindAccounts(Company)!.Accounts.ShouldHaveSingleItem().Balance.ShouldBe(new Money(5000m));
        }

        [Test]
        public void reports_the_rows_it_skipped() =>
            NewService().LoadBalances(Company, Csv("1111234522226789,5000.00\nbad")).Errors.ShouldHaveSingleItem().LineNumber.ShouldBe(2);

        [Test]
        public void replaces_any_accounts_previously_loaded_for_the_company()
        {
            var service = NewService();
            service.LoadBalances(Company, Csv("1111234522226789,5000.00"));

            service.LoadBalances(Company, Csv("2222123433331212,550.00"));

            service.FindAccounts(Company)!.Accounts.ShouldHaveSingleItem().Number.Value.ShouldBe("2222123433331212");
        }
    }

    public class ApplyTransfers
    {
        [Test]
        public void returns_null_when_the_company_has_no_accounts_loaded() =>
            NewService().ApplyTransfers(Company, Csv("1111234522226789,2222123433331212,1.00")).ShouldBeNull();

        [Test]
        public void applies_transfers_and_keeps_the_new_balances_for_later_requests()
        {
            var service = NewService();
            service.LoadBalances(Company, Csv("1111234522226789,100.00\n2222123433331212,0.00"));

            service.ApplyTransfers(Company, Csv("1111234522226789,2222123433331212,40.00"));

            service.FindAccounts(Company)!.Find(new AccountNumber("2222123433331212"))!.Balance.ShouldBe(new Money(40m));
        }

        [Test]
        public void reports_processed_transfers_and_parse_errors_separately()
        {
            var service = NewService();
            service.LoadBalances(Company, Csv("1111234522226789,100.00\n2222123433331212,0.00"));

            var result = service.ApplyTransfers(Company, Csv("1111234522226789,2222123433331212,40.00\nbad"))!;

            result.Processed.ShouldHaveSingleItem().LineNumber.ShouldBe(1);
            result.ParseErrors.ShouldHaveSingleItem().LineNumber.ShouldBe(2);
        }
    }
}
