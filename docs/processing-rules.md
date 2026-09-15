# Processing rules

## Ordering and replay

Files are selected only from the top level of `Input` and processed by ordinal file name. Rows are processed in source order. Every valid row is sent to the Accounts service, including repeated external IDs; the service is the system of record for idempotency. Its `Duplicate` response is reported separately.

The SHA-256 content digest is part of each destination and report name. Replaying identical bytes therefore uses the same names and row order. If the destination already contains identical bytes, the new input copy is deleted after processing; if a different file somehow occupies that digest-derived destination, processing fails instead of overwriting it. Reconciliation reports for an exact replay replace the prior report and represent the latest attempt.

## Validation

The importer requires the exact six-column header. It rejects blank rows, invalid CSV quoting, wrong column counts, missing or oversized text fields, dates outside `yyyy-MM-dd`, non-positive/non-invariant amounts, amounts with more than two decimal places, and transaction types other than deposits or withdrawals.

A malformed header is a file-level rejection. Parser failures and business validation failures never call WCF. A WCF communication error or service rejection rejects only that row and processing continues.

## File movement

| Outcome | Rule | Destination |
| --- | --- | --- |
| `Completed` | Zero rejected rows. Accepted and duplicate rows are both complete. | `Archive` |
| `Partial` | At least one accepted/duplicate row and at least one rejected row. | `Error` |
| `Rejected` | One or more rejected rows and no accepted/duplicate rows. | `Error` |

Destination names use `<source-base>.<first-12-sha256>.<outcome>.csv`. Files are moved only after all rows have been attempted. Report-write failure is fatal and leaves the already-moved input visible in Archive/Error; rerun by copying it back to Input.

## Reconciliation

Each input produces:

- `<source-base>.<first-12-sha256>.reconciliation.json`
- `<source-base>.<first-12-sha256>.reconciliation.csv`

Both formats contain the source name, complete SHA-256, UTC processing time, destination, outcome, total rows, accepted/duplicate/rejected counts, and row-numbered errors with stable error codes. `RejectedCount` counts rejected source rows, not the number of validation messages; `RowErrors` may therefore contain more entries than `RejectedCount`.

## WCF operation

The client uses `BasicHttpBinding` without transport security at:

```text
http://localhost:8090/AccountService
```

The committed generated-style client matches the Accounts service contract
namespace `urn:contoso:legacy-bank:accounts:v1`. It maps deposits to positive
`Credit` transactions and withdrawals to negative `Debit` transactions.
Successful calls return the imported transaction DTO. A typed
`DuplicateExternalId` fault is mapped to the reconciliation duplicate count;
other faults remain explicit failures.
