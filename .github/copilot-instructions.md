# Copilot instructions

- This repository is a Windows-only classic .NET Framework 4.8 console batch application.
- Preserve non-SDK `.csproj` files and `packages.config`; do not migrate to SDK-style projects or modern .NET without an explicit request.
- Keep all runtime paths configurable and rooted under `%LOCALAPPDATA%\ContosoLegacyBank\Batch` by default.
- Preserve deterministic ordinal file ordering, source row ordering, SHA-256 destination naming, and WCF-backed duplicate detection.
- Never treat duplicate `ExternalId` values as accepted or rejected; report them in the duplicate count.
- Maintain exact CSV headers and invariant-culture amount/date validation.
- Add or update the executable tests for parser, validation, processing, or WCF contract changes.
- Validate Release builds with Visual Studio MSBuild and run `Contoso.LegacyBank.Batch.Tests.exe`.
