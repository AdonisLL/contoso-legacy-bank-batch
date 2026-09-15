# Contoso Legacy Bank Batch

A classic scheduled-console transaction importer targeting **.NET Framework 4.8**. It reads CSV files in deterministic file/row order, validates each transaction, and invokes the Accounts WCF `ImportTransaction` operation over `BasicHttpBinding`.

## Build and test

Use a Visual Studio Developer PowerShell or an installed Visual Studio Build Tools MSBuild:

```powershell
msbuild .\Contoso.LegacyBank.Batch.sln /t:Restore,Build /p:Configuration=Release
.\tests\Contoso.LegacyBank.Batch.Tests\bin\Release\Contoso.LegacyBank.Batch.Tests.exe
```

Both projects use the classic non-SDK project format and `packages.config` (there are currently no third-party runtime dependencies).

## Run

1. Start the Accounts WCF service at `http://localhost:8090/AccountService`.
2. Build the solution.
3. Copy one or more `.csv` files into `%LOCALAPPDATA%\ContosoLegacyBank\Batch\Input`.
4. Run:

   ```powershell
   .\src\Contoso.LegacyBank.Batch\bin\Release\Contoso.LegacyBank.Batch.exe
   ```

The process creates these configurable directories:

- `Input` — pending CSV files
- `Archive` — files with no rejected rows
- `Error` — rejected or partially processed files
- `Reports` — reconciliation reports in JSON and CSV

Configuration is in `src\Contoso.LegacyBank.Batch\App.config`. Environment variables in `BatchRootDirectory` are expanded. Exit code `0` means all rows were accepted or duplicates, `2` means one or more rows were rejected, and `1` means a fatal batch failure.

## CSV contract

The header is exact and case-sensitive:

```text
ExternalId,AccountNumber,PostedDate,Description,Amount,TransactionType
```

- `PostedDate`: `yyyy-MM-dd`
- `Amount`: positive invariant decimal, maximum two decimal places
- `TransactionType`: `Deposit` or `Withdrawal` (case-insensitive)
- Quoted descriptions, commas, and escaped double quotes are supported.
- Duplicate `ExternalId` responses are counted separately from accepted and rejected rows.

See [docs/processing-rules.md](docs/processing-rules.md) for movement, replay, WCF contract, and reconciliation rules. Ready-to-copy examples are in `samples`.

## Windows Task Scheduler

Create a task that runs the executable with the desired service account. Set **Start in** to the executable directory, do not allow overlapping runs, and retain the default per-user `%LOCALAPPDATA%` location or override `BatchRootDirectory` for a service account.
