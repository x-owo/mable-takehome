using System.Text;
using MableBank.Api.Contracts;
using MableBank.Api.Controllers;
using MableBank.Api.Services;
using MableBank.Api.Storage;
using MableBank.Domain.Csv;
using MableBank.Domain.Transfers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MableBank.Tests.Api.Controllers;

public class ControllerTests
{
    private const string Company = "alpha-sales";

    private BankingService _banking = null!;

    [SetUp]
    public void NewBank() =>
        _banking = new BankingService(
            new InMemoryAccountBookRepository(), new BalancesCsvParser(), new TransfersCsvParser(), new TransferProcessor());

    private static T WithBody<T>(T controller, string body) where T : ControllerBase
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private AccountsController Accounts(string body = "") => WithBody(new AccountsController(_banking), body);

    private TransfersController Transfers(string body = "") => WithBody(new TransfersController(_banking), body);

    private static T OkValue<T>(ActionResult<T> result) =>
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeAssignableTo<T>()!;

    private static void ShouldBeNotFound<T>(ActionResult<T> result) =>
        result.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);

    private Task LoadAlphaSales() =>
        Accounts("1111234522226789,100.00\n2222123433331212,0.00").LoadBalances(Company, CancellationToken.None);

    public class PutAccounts : ControllerTests
    {
        [Test]
        public async Task returns_how_many_accounts_loaded_and_which_rows_were_skipped()
        {
            var result = await Accounts("1111234522226789,100.00\nbad").LoadBalances(Company, CancellationToken.None);

            var response = OkValue(result);
            response.Loaded.ShouldBe(1);
            response.Errors.ShouldHaveSingleItem().Line.ShouldBe(2);
        }
    }

    public class GetAccounts : ControllerTests
    {
        [Test]
        public async Task lists_the_company_accounts_with_balances()
        {
            await LoadAlphaSales();

            OkValue(Accounts().List(Company)).Select(a => a.Balance).ShouldBe(["100.00", "0.00"]);
        }

        [Test]
        public void returns_404_for_a_company_with_no_balances_loaded() =>
            ShouldBeNotFound(Accounts().List("unknown"));
    }

    public class GetAccount : ControllerTests
    {
        [Test]
        public async Task returns_the_account()
        {
            await LoadAlphaSales();

            OkValue(Accounts().Get(Company, "1111234522226789")).ShouldBe(new AccountResponse("1111234522226789", "100.00"));
        }

        [TestCase("9999999999999999")]
        [TestCase("not-an-account")]
        public async Task returns_404_for_an_account_the_company_does_not_hold(string accountNumber)
        {
            await LoadAlphaSales();

            ShouldBeNotFound(Accounts().Get(Company, accountNumber));
        }

        [Test]
        public void returns_404_for_a_company_with_no_balances_loaded() =>
            ShouldBeNotFound(Accounts().Get("unknown", "1111234522226789"));
    }

    public class PostTransfers : ControllerTests
    {
        [Test]
        public async Task reports_succeeded_and_failed_transfers_and_the_new_balances()
        {
            await LoadAlphaSales();

            var result = await Transfers("1111234522226789,2222123433331212,40.00\n2222123433331212,1111234522226789,99.00")
                .Apply(Company, CancellationToken.None);

            var response = OkValue(result);
            response.Succeeded.ShouldHaveSingleItem().Line.ShouldBe(1);
            response.Failed.ShouldHaveSingleItem().Reason.Code.ShouldBe("InsufficientFunds");
            response.Balances.Select(b => b.Balance).ShouldBe(["60.00", "40.00"]);
        }

        [Test]
        public async Task returns_404_for_a_company_with_no_balances_loaded() =>
            ShouldBeNotFound(await Transfers("1111234522226789,2222123433331212,1.00").Apply("unknown", CancellationToken.None));
    }
}
