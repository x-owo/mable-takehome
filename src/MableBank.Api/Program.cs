using MableBank.Api.Services;
using MableBank.Api.Storage;
using MableBank.Domain.Accounts;
using MableBank.Domain.Csv;
using MableBank.Domain.Transfers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IAccountBookRepository, InMemoryAccountBookRepository>();
builder.Services.AddSingleton<BalancesCsvParser>();
builder.Services.AddSingleton<TransfersCsvParser>();
builder.Services.AddSingleton<TransferProcessor>();
builder.Services.AddSingleton<BankingService>();

var app = builder.Build();

app.MapControllers();

app.Run();
