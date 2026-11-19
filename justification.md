# Technical decisions and justification

This document records the significant technical decisions behind MableBank: what was chosen, what the
alternatives were, and why the choice fits the [brief](MABLE_BACK_END_CODE_TEST.md) and its rubric. Each
decision ends with its trade-off: what we gave up to get it.

The brief in one sentence: load one company's account balances from a CSV, then apply a day's transfers from a
second CSV, never letting an account go below $0. The rubric rewards domain models, encapsulation, separation of
concerns, short readable methods, tests that explain behaviour, and an app that "runs and provides feedback".

---

## 1. Platform and project setup

### 1.1 ASP.NET Core Web API (not a console app)
**Decision.** The system is an HTTP API. Companies upload their CSVs as request bodies.

**Why.**
- The brief describes a service that companies send files to every day. That is a backend service, not a
  one-off script.
- Each response returns structured feedback: what succeeded, what failed and why, and the closing balances.
  That directly serves the rubric's "runs and provides feedback" item.

**Trade-off.**
- More ceremony than a console app: hosting, routing and serialisation.
- Running it means starting a server and then sending requests.
- This is mitigated by the `.http` file, the VS Code launch configuration and the README commands.

### 1.2 .NET 10 (LTS)
**Why.**
- It is the current long-term-support release, with the newest C# features we use: primary constructors,
  collection expressions and record structs.
- .NET 8 also works, but its support ends in November 2026.

**Trade-off.** Readers need the .NET 10 SDK installed.

### 1.3 Three projects: Domain / Api / Tests
**Decision.**
- `MableBank.Domain` contains the models, business rules and CSV parsing, and has no ASP.NET dependency.
- `MableBank.Api` contains HTTP, orchestration, JSON contracts and storage.
- `MableBank.Tests` contains the specs.

**Why.**
- The project boundary enforces separation of concerns: the domain *cannot* reference web types, even
  accidentally.
- Clean Architecture's four or more layers (Application, Infrastructure, …) were considered and rejected as
  ceremony out of proportion to a problem this size.

**Trade-off.** Some application-level code (`BankingService`) lives in the Api project instead of its own
layer. At this scale that is the right call; it would move out if the app grew.

### 1.4 Strict compiler settings
**Decision.** Nullable reference types are enabled and warnings are treated as errors. Both are set once in
`Directory.Build.props`.

**Why.**
- Null-safety bugs and ignored warnings are caught at build time rather than in production.
- Centralising the settings means every project gets them and the `.csproj` files stay minimal.

**Trade-off.** Small friction, since every nullable path must be handled explicitly (for example
`FailureReason!.Value` in the mapper, where success has already been filtered out).

### 1.5 `.slnx` solution format; `src/` and `tests/` layout
**Why.**
- `.slnx` is the .NET 10 default. It is readable XML, so it merges cleanly in git.
- Separate `src/` and `tests/` folders are the conventional .NET layout.

**Trade-off.** Older tooling (pre-2025 IDEs) may not open `.slnx`.

---

## 2. Domain model

### 2.1 Value objects instead of primitives (`AccountNumber`, `Money`)
**Decision.**
- `AccountNumber` is a `readonly record struct` that can only hold exactly 16 ASCII digits.
- `Money` wraps a `decimal` and can only hold whole cents (at most 2 decimal places).

**Why.**
- *Make invalid states unrepresentable.* Once you have an `AccountNumber` or a `Money`, it is valid, so no
  downstream code re-validates.
- This is the rubric's "uses domain models" item: a method signature such as
  `Transfer(AccountNumber from, AccountNumber to, Money amount)` documents itself, and you can't accidentally
  swap an amount and an account number.
- `decimal` (not `double`) avoids binary floating-point error in money arithmetic. For example, 0.1 + 0.2 must
  equal exactly 0.30.
- Record structs give value equality and hashing for free, so `AccountNumber` works directly as a dictionary
  key.

**Trade-off.** Slightly more code than passing `string` and `decimal` around, and `default(AccountNumber)`
bypasses the constructor (an inherent struct limitation; we never create one that way).

### 2.2 `Money` may be negative; positivity is enforced where it matters
**Decision.**
- `Money` itself allows negative values.
- Transfer amounts must be greater than $0, enforced by `Transfer` and by the parser.
- Account balances may not be *withdrawn* below $0, enforced by `Account`.

**Why.**
- Negative opening balances were deliberately *not* rejected, so `Money` must be able to represent them.
- Each rule lives on the concept it belongs to rather than being baked into the money type: "amount must be
  positive" is a rule about transfers, not about money.

**Trade-off.** An account can be *loaded* with a negative balance. It simply can't send money until topped up.
This is documented as an assumption.

### 2.3 `Account` is a mutable entity with an encapsulated balance
**Decision.**
- `Balance` has a private setter and changes only through `Withdraw` and `Deposit`.
- `Withdraw` refuses to go below $0, and both methods reject amounts that are not positive.

**Alternatives considered.** An immutable record, where `Withdraw` returns a new `Account`.

**Why mutable.**
- An account has *identity*: it is the same account before and after a transfer, with a changed balance. A
  mutable entity models that honestly.
- The invariant ("never below $0") sits next to the only code that can change the balance, so nothing can
  bypass it. That is the rubric's "models encapsulate logic appropriately" item.
- Processing stays simple: there is no need to write new instances back into a collection. With immutability,
  forgetting that write-back is a silent bug.

**Trade-off.** A mutable object raises the risk of partial updates (withdraw succeeds, deposit fails). That risk
is removed by the check-then-change design in 2.4. It also raises the risk of concurrent modification
(see 5.2).

### 2.4 `Transfer.ExecuteOn(source, destination)` checks everything before changing anything
**Decision.**
- The transfer itself performs the move.
- It checks the same-account rule and sufficient funds *first*, and only then calls `Withdraw` and then
  `Deposit`.

**Alternatives considered.**
- A `Ledger` aggregate owning the logic.
- A stateless processor operating on a dictionary.

**Why.**
- A transfer is the domain concept that "moves money", so giving it that behaviour reads naturally:
  `transfer.ExecuteOn(alice, bob)`.
- Because every rule is checked before any state changes, a failed transfer leaves both accounts *exactly* as
  they were. There is never half-moved money and no rollback logic.
- It also asserts that the accounts passed in really are its `From` and `To` accounts, which catches
  programming errors early.

**Trade-off.** A transfer can't look accounts up itself, so the "unknown account" rule had to live one level up,
in `TransferProcessor` (see 2.6).

### 2.5 Business failures are result objects, not exceptions
**Decision.**
- A rejected transfer returns `TransferOutcome.Failure(transfer, reason)`.
- `reason` is one of `InsufficientFunds`, `SameAccount`, `UnknownSourceAccount` or `UnknownDestinationAccount`.
- Exceptions are reserved for *programming errors*, such as constructing an invalid value object or passing the
  wrong account to a transfer.

**Alternatives considered.**
- Domain exceptions, such as `InsufficientFundsException`.
- A `TryX` method returning `bool`.

**Why.**
- A rejected transfer is an *expected* business outcome on a normal day, not an exceptional condition.
- Encoding it in the return type makes the possibility of failure visible in the method signature, avoids using
  exceptions for control flow, and makes tests straightforward (`outcome.FailureReason.ShouldBe(...)`).
- A `bool` was rejected because it carries no reason, and the API must report *why* a transfer failed.

**Trade-off.**
- Callers must check the outcome; nothing forces them to.
- `Account.Withdraw` still throws if called unsafely. That is a deliberate second line of defence, so the
  invariant holds even if a future caller skips the check.

### 2.6 `AccountBook` and `TransferProcessor`
**Decision.**
- `AccountBook` is one company's accounts, looked up by `AccountNumber`. It rejects duplicates and keeps the
  order the accounts were loaded in.
- `TransferProcessor` is a stateless domain service that walks the transfers in order. For each one it looks up
  both accounts (an unknown account becomes a failure outcome) and calls `ExecuteOn`.

**Why.**
- Each class has one job: `AccountBook` holds and finds accounts, `Transfer` moves money, and
  `TransferProcessor` sequences a batch.
- Wrapping the dictionary in a named domain type, instead of passing a raw `Dictionary` around, keeps "a
  company's accounts" as a real concept in the code (rubric: "uses native data structures readably").
- `AccountBook` keeps a separate `List` for ordering, because .NET does not *guarantee* that `Dictionary`
  preserves insertion order. That gives the API stable, predictable output order.

**Trade-off.** Two classes where one could do, in exchange for single responsibilities and focused tests.

### 2.7 Transfers are applied in file order, and failures are skipped
**Decision.**
- Transfers run sequentially in the order they appear.
- A failed transfer is recorded and processing continues with the next one.

**Alternatives considered.**
- All-or-nothing: reject the whole file if any transfer fails.
- Retry failed transfers at the end.

**Why.**
- Processing in order is the most predictable semantics, and lets an earlier deposit legitimately fund a later
  withdrawal (there is a test for this).
- Skip-and-continue matches how real batch payment files behave: one bad payment shouldn't block a company's
  other, valid payments.
- All-or-nothing would punish every customer for one overdrawn account. Retrying would make the outcome depend
  on processing passes, which is harder to reason about and explain.

**Trade-off.** The final state depends on transfer order. That is intentional, and the response reports each
transfer's line number so the outcome is explainable.

---

## 3. CSV parsing

### 3.1 Hand-rolled parsers, one per file type
**Decision.**
- `BalancesCsvParser` and `TransfersCsvParser` split lines on `,`.
- They share small internal helpers: `CsvLine` for reading and numbering lines, and `CsvFields` for turning
  fields into domain values.

**Alternatives considered.**
- The CsvHelper library.
- A generic `CsvReader<T>` with mappers.

**Why.**
- Both formats are trivial: no header, no quoting, and two or three columns.
- A library adds a dependency and mapping configuration for no real benefit. Hand-rolled parsing is short,
  transparent and fully unit-testable.
- Two explicit parsers read more clearly than a generic abstraction built for only two cases.

**Trade-off.** Quoted fields and embedded commas are not supported. They would require switching to a real CSV
library.

### 3.2 Parsers read from a `TextReader`
**Why.**
- It decouples the parsers from where the text comes from: tests pass a `StringReader`, the sample-file tests
  pass a file reader, and the API passes the request body.
- `ReadLine` handles both `\r\n` and `\n` line endings automatically.

**Trade-off.** ASP.NET Core forbids synchronous reads of the request body, so the API reads the body
asynchronously into a string first (`CsvBody.ReadAsync`). That is fine at the file sizes this brief implies. A
very large upload would want async streaming parsers.

### 3.3 Malformed rows are skipped and reported, not fatal
**Decision.**
- Each bad row becomes a `CsvParseError` (line number, raw text, reason) and parsing carries on.
- Valid rows come back as `ParsedRow<T>`, each with its line number.
- Both are returned together in `CsvParseResult<T>`.

**Why.**
- It is consistent with the skip-and-continue rule for transfers (2.7).
- It gives the most useful feedback possible: the company sees exactly which lines were wrong and why, while
  everything valid still goes through.
- Line numbers count *physical* lines, including blank ones, so they match what the user sees in their editor.

**Trade-off.** A badly broken file is "accepted" with many errors rather than rejected outright. Clients must
read the `errors` and `parseErrors` arrays.

### 3.4 Parsers check format only; business rules stay in the domain
**Decision.**
- The parsers check column count, the 16-digit format, amount syntax and precision, and that a transfer amount
  is greater than $0.
- They do *not* check whether an account exists or whether from and to are the same account.

**Why.**
- Account existence depends on the company's current state, which is a business concern for
  `TransferProcessor`, not a file-format concern.
- Keeping that boundary means the parsers need no knowledge of storage, and each rule has exactly one home.

### 3.5 Specific input rules
- **Duplicate account in a balances file:** keep the first, report later ones with a reference back to the
  original line. This is deterministic, nothing is silently overwritten, and the user is told.
- **Blank lines** are ignored, since a trailing newline or spacing shouldn't produce errors.
- **No whitespace trimming, no `$` symbol, no thousands separators.** Amounts are parsed with the invariant
  culture so results never depend on the server's locale settings (for example `1.000,00` in some European
  locales). Strict input avoids ambiguous interpretations.

---

## 4. API design

### 4.1 Resource-oriented routes, company in the URL
```
PUT  /companies/{companyId}/accounts
GET  /companies/{companyId}/accounts
GET  /companies/{companyId}/accounts/{accountNumber}
POST /companies/{companyId}/transfers
```
**Why.**
- Putting the company in the route keeps each company's data cleanly separated while still supporting the
  brief's "single company" workflow.
- **PUT** for balances is a deliberate HTTP-semantics choice: uploading balances *replaces* the company's
  starting state, and repeating the same upload gives the same result.
- **POST** for transfers fits because applying transfers changes state and is *not* repeatable safely: posting
  the same file twice moves the money twice.

**Trade-off.**
- There is no protection against accidentally re-posting the same day's transfers twice. An idempotency key
  would be the production fix.
- `companyId` is an unvalidated free-form slug, kept lenient on purpose.

### 4.2 Raw `text/csv` request body (not multipart upload)
**Why.** It is the simplest contract: the CSV *is* the body. That makes it trivial to call from curl,
PowerShell, the `.http` file or another service, with no form encoding.

**Trade-off.**
- Browser-form uploads aren't directly supported.
- The `Content-Type` isn't enforced, which is lenient by choice.

### 4.3 Controllers (not Minimal APIs)
**Why.**
- Controllers group related endpoints into named classes (`AccountsController`, `TransfersController`) with
  conventional, familiar structure.
- They are easy to unit-test by constructing them directly.

**Trade-off.** Slightly more boilerplate than `MapGet`/`MapPost`.

### 4.4 Thin controllers + `BankingService` + `ResponseMapper`
**Decision.**
- Controllers only deal with HTTP: read the body, call the service, choose a status code.
- `BankingService` coordinates each use case (parse → process → store).
- `ResponseMapper` converts domain objects into JSON contracts.

**Why.**
- Separation of concerns, which the rubric lists explicitly: HTTP, workflow, domain rules and presentation each
  live in one place.
- Domain objects are never serialised directly, so internal models can change without breaking the API
  contract.

**Trade-off.** More classes, each of them small and short-methoded, which the rubric also rewards.

### 4.5 One response that reports everything
`POST /transfers` returns:
- `succeeded` and `failed` transfers, each with its CSV line number;
- `parseErrors`;
- the closing `balances`.

**Why.**
- The client gets complete, actionable feedback from a single call: what happened to every line of their file
  and the resulting state.
- That is the "provides feedback" rubric item, applied at the API level.

### 4.6 Failure reasons carry both a `code` and a `message`
```json
{ "code": "InsufficientFunds", "message": "Account 2222123433331212 has insufficient funds to transfer 99999.00." }
```
**Why.**
- `code` is stable and machine-readable, so client code can branch on it.
- `message` is human-readable for display and support.
- Messages are generated in the API layer, not the domain, because wording is a presentation concern.
- A test asserts that every `TransferFailureReason` has a message, so adding a new reason can't silently ship
  without one.

### 4.7 Money is serialised as a string
```json
{ "accountNumber": "1111234522226789", "balance": "4820.50" }
```
**Why.**
- JSON numbers are read as binary floating point by many clients, notably JavaScript, which can lose precision
  and always drops the trailing zero (`4820.5`).
- A string guarantees the exact two-decimal value, a common practice in payment APIs.

**Trade-off.** Clients must parse the string to do arithmetic.

### 4.8 Status codes
- **200** for processed uploads, even when some rows or transfers failed. The request itself succeeded, and the
  per-item results are in the body.
- **404 ProblemDetails** when a company has no balances loaded, or an account isn't held.
  - ProblemDetails (RFC 9457, which replaced RFC 7807) is the ASP.NET Core standard error shape.
  - We return 404 instead of an empty 200 so that a typo in a company ID is reported, not mistaken for an empty
    company.
- The stricter validations offered (400 for an empty body, 400 for a bad company ID, 415 unless `text/csv`)
  were declined in favour of leniency.

### 4.9 `UseHttpsRedirection` removed
**Why.** The local `http` launch profile has no HTTPS port, so the redirect middleware could only produce
warnings or broken redirects during development and testing.

**Trade-off.** A production deployment would terminate TLS at a proxy or reinstate HTTPS.

---

## 5. Storage

### 5.1 In-memory repository behind a domain interface
**Decision.**
- `IAccountBookRepository` is defined in the Domain project.
- `InMemoryAccountBookRepository` implements it in the Api project, using a `ConcurrentDictionary`.
- It is registered as a singleton, so the data lives for the lifetime of the process.

**Alternatives considered.** EF Core with SQLite, or EF Core InMemory.

**Why.**
- The brief needs no persistence, and a database adds setup, migrations and test complexity that don't serve
  the rubric.
- Defining the interface in the domain (dependency inversion) means a real database could replace it without
  touching domain or controller code.

**Trade-off.** Data is lost on restart. The README states this.

### 5.2 No per-company locking
**Decision.** Concurrent transfer uploads for the *same* company are not synchronised.

**Why.**
- It was explicitly scoped out to keep the solution focused.
- The `ConcurrentDictionary` keeps the company map itself safe. Only simultaneous changes to the *same*
  company's accounts could race.

**Trade-off.**
- Two simultaneous `POST /transfers` for one company could interleave and break the no-overdraft guarantee.
- The fix is a per-company lock (or a database transaction) around `ApplyTransfers`. That is the first thing to
  add before real use.

### 5.3 Stored accounts are changed in place
**Why.** Because `AccountBook` holds mutable `Account` entities and the repository stores references, applying
transfers updates the stored state directly; no explicit save is needed after processing.

**Trade-off.** This relies on in-memory reference semantics. A persistent repository would need an explicit
`Save` after processing, a one-line change in `BankingService`.

---

## 6. Testing

### 6.1 NUnit as the rspec equivalent
**Why.**
- The rubric names rspec (Ruby), so an equivalent was needed.
- NUnit's nested fixtures map naturally onto rspec's `describe`/`context` blocks, and `[TestCase]` gives concise
  data-driven specs.

### 6.2 rspec-style structure and naming
```
AccountTests
  └─ Withdraw
       ├─ reduces_the_balance_by_the_amount
       └─ throws_and_leaves_the_balance_unchanged_when_it_would_go_below_zero
```
**Why.**
- The rubric asks that "tests explain the functionality". Nested classes per method or context, with
  plain-English snake_case names, make the test explorer read as a specification of the system.

**Trade-off.** Snake_case breaks normal C# naming conventions. That is acceptable, and common, in test code.

### 6.3 Shouldly for assertions
**Why.**
- `balance.ShouldBe(new Money(4500m))` reads close to rspec's `expect(...).to eq(...)` and gives clear failure
  messages.
- It is MIT-licensed. FluentAssertions was avoided because v8+ requires a commercial licence.

### 6.4 Tests written alongside each step
**Why.** Every commit was verified when it was made, rather than every step being tested at the end. This
caught real issues during development: a `Dictionary` ordering assumption, and a type assertion in a test
helper.

### 6.5 Orthogonal tests, plus one end-to-end spec
**Decision.**
- Each class is tested at its own level: value objects, `Account`, `Transfer`, parsers, processor, repository,
  service, mapper and controllers.
- One spec feeds the real Mable sample CSVs through parse → process and asserts the exact expected closing
  balances.

**Why.**
- Focused tests pinpoint failures, which is the rubric's "tests are orthogonal" item.
- The end-to-end spec proves the pieces work together and that the system "calculates the test files
  accurately".
- The sample CSVs are linked into the test output (not copied), so there is a single source of truth.

**Trade-off.**
- Controller tests construct controllers directly with a fake `HttpContext` rather than going through the full
  HTTP pipeline.
- Real-HTTP integration tests (`WebApplicationFactory`) were scoped out. The endpoints were instead verified
  manually by running the API and calling it with curl.

---

## 7. Developer experience

### 7.1 VS Code configuration
- **`tasks.json`:**
  - `build` is the default build task (Ctrl+Shift+B).
  - `test` is the default test task.
  - `test (detailed)` lists every spec by name.
  - `test (watch)` re-runs the tests when a file changes.
- **`launch.json`:** press F5 to debug the API on `http://localhost:5223`, with a pre-build step. There is also
  an attach-to-process configuration.
- **`extensions.json`** recommends C# Dev Kit, for the Testing sidebar and debugging individual tests.
- **`settings.json`** points VS Code at `MableBank.slnx`.

**Why.** The project is developed in VS Code, so build, test, run and debug each need a single keystroke or
command.

### 7.2 `MableBank.Api.http`
**Why.** It provides ready-made requests for every endpoint, including a deliberately failing batch (overdraft,
unknown account, same account, malformed row). Anyone can exercise the whole API without writing a request.

### 7.3 Git history: one commit per step
**Why.** The history tells the story of the build in order: scaffold → domain → specs → parsers → processing →
API → docs. That makes review easy and every step independently verifiable.

---

## 8. Known limitations and what would come next

| Limitation | Production fix |
|---|---|
| Concurrent uploads for one company can race | Per-company lock, or a DB transaction around `ApplyTransfers` |
| Re-posting the same transfers file applies it twice | Idempotency key per upload, or a hash of each processed file |
| Data lost on restart | Swap `InMemoryAccountBookRepository` for an EF Core implementation |
| No quoted-field CSV support | Switch the parsers' line splitting to CsvHelper |
| No authentication; any caller can act for any company | Authenticate callers and authorise them per company |
| Whole request body read into memory | Async streaming parser for very large files |
| No transfer history or audit trail | Persist each `ProcessedTransfer` with a timestamp |
