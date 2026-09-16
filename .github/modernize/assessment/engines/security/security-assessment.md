# Security Assessment Report

**Generated:** 2026-09-16T16:13:52.817Z

## Summary

| Metric | Count |
|--------|-------|
| Total Findings | 4 |
| CVE Vulnerabilities | 0 |
| CWE Vulnerabilities | 4 |
| Total Rules Assessed | 59 |
| Rules Passed | 55 |

### By Severity

| Severity | Count |
|----------|-------|
| mandatory | 0 |
| optional | 3 |
| potential | 1 |

## CVE Findings (Dependency Vulnerabilities)

## CWE Findings (Code-Level Vulnerabilities)

### CWE-22: Improper Limitation of a Pathname to a Restricted Directory ('Path Traversal')
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 8
- **Files:** src/Contoso.LegacyBank.Batch/BatchSettings.cs:30

BatchSettings.Load combines the configured InputDirectoryName with RootDirectory at line 30 without validating that the resolved path remains beneath the configured root.

### CWE-23: Relative Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** src/Contoso.LegacyBank.Batch/BatchSettings.cs:30

BatchSettings.Load accepts a configured directory name containing relative traversal segments when constructing InputDirectory at line 30.

### CWE-36: Absolute Path Traversal
- **Category:** File & Path Security
- **Severity:** optional
- **Story Points:** 5
- **Files:** src/Contoso.LegacyBank.Batch/BatchSettings.cs:30

BatchSettings.Load passes a configured directory name directly to Path.Combine at line 30, allowing a rooted path to replace the configured root.

### CWE-99: Improper Control of Resource Identifiers ('Resource Injection')
- **Category:** Injection Attacks
- **Severity:** potential
- **Story Points:** 3
- **Files:** src/Contoso.LegacyBank.Batch/BatchSettings.cs:34

BatchSettings.Load constructs the WCF AccountServiceEndpoint URI from an unrestricted application configuration value at line 34.
