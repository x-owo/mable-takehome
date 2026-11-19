# MableBank

A small ASP.NET Core Web API for the [Mable back end code challenge](MABLE_BACK_END_CODE_TEST.md). It loads a
company's starting account balances from a CSV and then applies a day's transfers CSV to them. A transfer is
never allowed to take an account below $0.

## Running and testing

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet test                                   # run the NUnit test suite
dotnet run --project src/MableBank.Api        # start the API on http://localhost:5223
```

With the API running, load the sample files:

```sh
curl -X PUT  http://localhost:5223/companies/alpha-sales/accounts  -H "Content-Type: text/csv" --data-binary @mable_account_balances.csv
curl -X POST http://localhost:5223/companies/alpha-sales/transfers -H "Content-Type: text/csv" --data-binary @mable_transactions.csv
curl         http://localhost:5223/companies/alpha-sales/accounts
```

### In VS Code

- Install the recommended **C# Dev Kit** extension. The **Testing** sidebar then shows the specs as a tree that
  you can run or debug one test at a time.
- **Ctrl+Shift+B** builds the solution.
- **Tasks: Run Test Task** runs `test`. Other tasks are `test (detailed)`, which lists every spec by name, and
  `test (watch)`, which re-runs the tests when a file changes.
- [`src/MableBank.Api/MableBank.Api.http`](src/MableBank.Api/MableBank.Api.http) has ready-made requests for
  every endpoint, including a batch of failing transfers. Send them with the REST Client extension.

### Endpoints

| Method | Route | Body | Returns |
|---|---|---|---|
| `PUT`  | `/companies/{companyId}/accounts` | `account,balance` CSV | Number of accounts loaded and any skipped rows |
| `GET`  | `/companies/{companyId}/accounts` | none | Every account and its balance |
| `GET`  | `/companies/{companyId}/accounts/{accountNumber}` | none | One account, or `404` |
| `POST` | `/companies/{companyId}/transfers` | `from,to,amount` CSV | Succeeded and failed transfers, parse errors, closing balances |

Requests for a company that has no balances loaded return `404`.

## Project layout

```
src/MableBank.Domain    domain models, business rules, CSV parsing (no ASP.NET dependency)
src/MableBank.Api       controllers, application service, JSON contracts, in-memory storage
tests/MableBank.Tests   NUnit + Shouldly specs, folders mirror the src namespaces
```

## Design decisions

**Domain models over primitives.**
- `AccountNumber` (exactly 16 digits) and `Money` (whole cents only) are value types.
- They can't hold an invalid value, so the rest of the code never re-checks them.

**Accounts protect their own balance.**
- `Account` is a mutable entity: its `Balance` can only change through `Withdraw` and `Deposit`.
- `Withdraw` refuses to take the balance below $0.

**A transfer checks before it changes anything.**
- `Transfer.ExecuteOn(source, destination)` checks every rule before touching either balance.
- So a failed transfer never leaves money half-moved.

**Business failures are results, not exceptions.**
- A transfer the rules reject is an expected outcome, not an error.
- It comes back as a `TransferOutcome` with a `TransferFailureReason`:
  - `InsufficientFunds`
  - `SameAccount`
  - `UnknownSourceAccount`
  - `UnknownDestinationAccount`
- Exceptions are reserved for programming errors, such as passing the wrong account to a transfer.

**Skip and continue.**
- Transfers are applied in file order, so an earlier deposit can fund a later withdrawal.
- A transfer that fails is reported and the rest of the file still runs.

**Malformed CSV rows are skipped and reported, not fatal.**
- The parsers check format only: column count, digits, amount precision, and that a transfer amount is greater
  than $0.
- Each bad row is reported with its line number, raw text and reason.
- Whether an account exists is a business rule, applied later by `TransferProcessor`.
- In a balances file, the first occurrence of a duplicate account is kept and later ones are reported.

**Clear responsibilities.**

| Class | Job |
|---|---|
| `BalancesCsvParser` / `TransfersCsvParser` | Text → domain objects |
| `AccountBook` | A company's accounts, looked up by number |
| `TransferProcessor` | Look up accounts, run transfers in order |
| `BankingService` | Use-case orchestration: parse → process → store |
| Controllers | HTTP only |
| `ResponseMapper` | Domain → JSON |

**Storage sits behind an interface.**
- `IAccountBookRepository` lives in the domain.
- The in-memory implementation lives in the API project and keeps each company separate.
- Reloading balances (`PUT`) replaces the company's accounts.
- Data does not survive a restart, and requests are not synchronised against each other.

**Money is a JSON string** (`"4820.50"`), so clients never lose cents or trailing zeros.

**Failure reasons have a code and a message.** The `code` is stable for clients to match on, and the `message`
is written for people.

**Specs read like documentation.** The tests use NUnit with rspec-style nested classes, for example
`AccountTests` → `Withdraw` → `throws_and_leaves_the_balance_unchanged_when_it_would_go_below_zero`. There is
also an end-to-end spec over the Mable sample files that checks the expected closing balances.

## Assumptions

- **No header row.** CSV files have no header row and fields are not quoted or trimmed. Blank lines are
  ignored.
- **Negative opening balances are accepted.** Such an account simply can't send money until it is topped up.
- **Amounts** must be plain numbers with at most two decimal places. `$500` and `1,000.00` are rejected.
