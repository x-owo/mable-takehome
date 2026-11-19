using MableBank.Api.Contracts;
using MableBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MableBank.Api.Controllers;

[ApiController]
[Route("companies/{companyId}/transfers")]
public sealed class TransfersController(BankingService banking) : ControllerBase
{
    /// <summary>Applies a day's <c>from,to,amount</c> CSV body to the company's accounts, in file order.</summary>
    [HttpPost]
    public async Task<ActionResult<ApplyTransfersResponse>> Apply(string companyId, CancellationToken cancellationToken)
    {
        using var csv = await CsvBody.ReadAsync(Request, cancellationToken);
        var result = banking.ApplyTransfers(companyId, csv);

        return result is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Company not found",
                detail: $"No balances have been loaded for company '{companyId}'.")
            : Ok(ResponseMapper.ToResponse(result));
    }
}
