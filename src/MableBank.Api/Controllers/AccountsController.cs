using MableBank.Api.Contracts;
using MableBank.Api.Services;
using MableBank.Domain.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace MableBank.Api.Controllers;

[ApiController]
[Route("companies/{companyId}/accounts")]
public sealed class AccountsController(BankingService banking) : ControllerBase
{
    /// <summary>Replaces the company's accounts with the balances in a <c>account,balance</c> CSV body.</summary>
    [HttpPut]
    public async Task<ActionResult<LoadBalancesResponse>> LoadBalances(string companyId, CancellationToken cancellationToken)
    {
        using var csv = await CsvBody.ReadAsync(Request, cancellationToken);
        var parsed = banking.LoadBalances(companyId, csv);
        return Ok(ResponseMapper.ToResponse(parsed));
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<AccountResponse>> List(string companyId)
    {
        var book = banking.FindAccounts(companyId);
        return book is null ? CompanyNotFound(companyId) : Ok(ResponseMapper.ToResponse(book));
    }

    [HttpGet("{accountNumber}")]
    public ActionResult<AccountResponse> Get(string companyId, string accountNumber)
    {
        var book = banking.FindAccounts(companyId);
        if (book is null)
            return CompanyNotFound(companyId);

        var account = AccountNumber.IsValid(accountNumber) ? book.Find(new AccountNumber(accountNumber)) : null;
        return account is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Account not found",
                detail: $"Company '{companyId}' has no account {accountNumber}.")
            : Ok(ResponseMapper.ToResponse(account));
    }

    private ObjectResult CompanyNotFound(string companyId) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Company not found",
            detail: $"No balances have been loaded for company '{companyId}'.");
}
