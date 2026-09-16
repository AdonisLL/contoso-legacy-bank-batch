# Security Assessment Report

**Generated:** 2026-09-16T00:48:57.247382Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 8 |
| CVE Vulnerabilities | 0 |
| CWE Vulnerabilities | 8 |
| Total Rules Assessed | 59 |
| Rules Passed | 51 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 0 |
| optional | 3 |
| potential | 5 |

## CVE Findings (Dependency Vulnerabilities)

No CVE findings met the configured severity threshold.

## CWE Findings (Code-Level Vulnerabilities)

### CWE-789: Memory Allocation with Excessive Size Value
- **Category:** Code Quality
- **Severity:** potential
- **Story Points:** 5
- **Files:** src/Contoso.LegacyBank.Batch/CsvParser.cs:23

CsvParser.Parse uses File.ReadAllLines to load the entire externally supplied CSV file into memory before validation. There is no file size or row count limit before this allocation, so a very large input file can force excessive memory allocation.

### CWE-662: Improper Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** src/Contoso.LegacyBank.Batch/BatchProcessor.cs:35, src/Contoso.LegacyBank.Batch/BatchProcessor.cs:155, src/Contoso.LegacyBank.Batch/BatchProcessor.cs:158, src/Contoso.LegacyBank.Batch/BatchProcessor.cs:169

BatchProcessor enumerates and moves files in shared input/archive/error directories without a lock, lease, or atomic claim step. If multiple batch process instances run against the same directories, they can simultaneously observe and process the same input file and race on deterministic destination moves.

### CWE-820: Missing Synchronization
- **Category:** Concurrency & Synchronization
- **Severity:** potential
- **Story Points:** 8
- **Files:** src/Contoso.LegacyBank.Batch/BatchProcessor.cs:35, src/Contoso.LegacyBank.Batch/BatchProcessor.cs:155, src/Contoso.LegacyBank.Batch/BatchProcessor.cs:158, src/Contoso.LegacyBank.Batch/BatchProcessor.cs:169

BatchProcessor enumerates and moves files in shared input/archive/error directories without a lock, lease, or atomic claim step. If multiple batch process instances run against the same directories, they can simultaneously observe and process the same input file and race on deterministic destination moves.

### CWE-778: Insufficient Logging
- **Category:** Credentials & Secrets
- **Severity:** potential
- **Story Points:** 3
- **Files:** src/Contoso.LegacyBank.Batch/BatchProcessor.cs:107, src/Contoso.LegacyBank.Batch/BatchProcessor.cs:109

BatchProcessor.ProcessFile catches exceptions from the account service import path and records only a row rejection with the base exception message. This security-relevant failure path is not logged with operational context such as source file, row number, or endpoint for audit/monitoring.

### CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 8
- **Files:** src/Contoso.LegacyBank.Batch/BatchSettings.cs:20, src/Contoso.LegacyBank.Batch/BatchSettings.cs:30

BatchSettings.Load reads directory names from appSettings and uses Path.Combine(root, configuredName) for InputDirectory, ArchiveDirectory, ErrorDirectory, and ReportsDirectory without validating that the resulting paths remain below RootDirectory. A configured traversal or absolute segment can resolve outside the intended batch root.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** src/Contoso.LegacyBank.Batch/BatchSettings.cs:30, src/Contoso.LegacyBank.Batch/BatchSettings.cs:31, src/Contoso.LegacyBank.Batch/BatchSettings.cs:32, src/Contoso.LegacyBank.Batch/BatchSettings.cs:33

BatchSettings.Load combines RootDirectory with appSettings values for the directory name settings without rejecting '..' segments, so values such as '..\Outside' can create input/archive/error/report directories outside the configured batch root.

### CWE-36: Absolute Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** src/Contoso.LegacyBank.Batch/BatchSettings.cs:30, src/Contoso.LegacyBank.Batch/BatchSettings.cs:31, src/Contoso.LegacyBank.Batch/BatchSettings.cs:32, src/Contoso.LegacyBank.Batch/BatchSettings.cs:33

BatchSettings.Load passes configured directory name values directly to Path.Combine. In .NET, an absolute second path causes Path.Combine to discard the root prefix, allowing configured absolute paths to escape the intended RootDirectory.

### CWE-99: Improper Control of Resource Identifiers ('Resource Injection')
- **Category:** Injection Attacks
- **Severity:** potential
- **Story Points:** 3
- **Files:** src/Contoso.LegacyBank.Batch/BatchSettings.cs:34, src/Contoso.LegacyBank.Batch/AccountService.cs:87

BatchSettings.Load reads AccountServiceEndpoint from appSettings and constructs a Uri, which AccountTransactionGateway passes directly into a WCF EndpointAddress. This externally controlled resource identifier can direct the batch process to an unintended service endpoint.
