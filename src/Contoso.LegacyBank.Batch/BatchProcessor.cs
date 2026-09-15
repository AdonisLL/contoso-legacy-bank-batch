using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Contoso.LegacyBank.Batch
{
    public sealed class BatchProcessor
    {
        private readonly BatchSettings settings;
        private readonly CsvParser parser;
        private readonly TransactionValidator validator;
        private readonly IAccountTransactionGateway gateway;
        private readonly ReconciliationReportWriter reportWriter;

        public BatchProcessor(
            BatchSettings settings,
            CsvParser parser,
            TransactionValidator validator,
            IAccountTransactionGateway gateway,
            ReconciliationReportWriter reportWriter)
        {
            this.settings = settings;
            this.parser = parser;
            this.validator = validator;
            this.gateway = gateway;
            this.reportWriter = reportWriter;
        }

        public BatchRunResult ProcessAll()
        {
            settings.EnsureDirectories();
            var run = new BatchRunResult();
            string[] files = Directory.GetFiles(settings.InputDirectory, "*.csv", SearchOption.TopDirectoryOnly)
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
                .ToArray();

            foreach (string file in files)
            {
                ReconciliationReport report = ProcessFile(file);
                run.FilesProcessed++;
                run.AcceptedCount += report.AcceptedCount;
                run.DuplicateCount += report.DuplicateCount;
                run.RejectedCount += report.RejectedCount;
            }

            return run;
        }

        public ReconciliationReport ProcessFile(string file)
        {
            string fullHash = ComputeSha256(file);
            string shortHash = fullHash.Substring(0, 12);
            string sourceName = Path.GetFileName(file);
            string reportBaseName = Path.GetFileNameWithoutExtension(file) + "." + shortHash;
            ParsedCsvFile parsed = parser.Parse(file);
            var report = new ReconciliationReport
            {
                SourceFile = sourceName,
                FileHash = fullHash,
                ProcessedUtc = DateTime.UtcNow,
                TotalRows = parsed.Records.Count + parsed.Errors.Count
            };

            foreach (RowError parseError in parsed.Errors)
            {
                report.RowErrors.Add(parseError);
                report.RejectedCount++;
            }

            foreach (CsvRecord record in parsed.Records.OrderBy(item => item.RowNumber))
            {
                ValidatedTransaction transaction;
                IList<RowError> validationErrors;
                if (!validator.TryValidate(record, out transaction, out validationErrors))
                {
                    foreach (RowError validationError in validationErrors)
                    {
                        report.RowErrors.Add(validationError);
                    }
                    report.RejectedCount++;
                    continue;
                }

                try
                {
                    ImportTransactionResponse response = gateway.Import(transaction);
                    if (response == null)
                    {
                        Reject(report, record, "EmptyServiceResponse", "The account service returned no response.");
                    }
                    else if (response.Duplicate)
                    {
                        report.DuplicateCount++;
                    }
                    else if (response.Accepted)
                    {
                        report.AcceptedCount++;
                    }
                    else
                    {
                        Reject(report, record, "ServiceRejected",
                            string.IsNullOrWhiteSpace(response.Message) ? "The account service rejected the transaction." : response.Message);
                    }
                }
                catch (Exception ex)
                {
                    Reject(report, record, "ServiceError", ex.GetBaseException().Message);
                }
            }

            report.Outcome = GetOutcome(report);
            string destinationDirectory = report.RejectedCount == 0 ? settings.ArchiveDirectory : settings.ErrorDirectory;
            string destinationName = Path.GetFileNameWithoutExtension(file) + "." + shortHash + "." +
                                     report.Outcome.ToLowerInvariant() + ".csv";
            string destinationPath = Path.Combine(destinationDirectory, destinationName);
            MoveDeterministically(file, destinationPath);
            report.DestinationFile = destinationPath;
            reportWriter.Write(report, settings.ReportsDirectory, reportBaseName);
            return report;
        }

        private static void Reject(ReconciliationReport report, CsvRecord record, string code, string message)
        {
            report.RejectedCount++;
            report.RowErrors.Add(new RowError
            {
                RowNumber = record.RowNumber,
                ExternalId = record.ExternalId,
                Code = code,
                Message = message
            });
        }

        private static string GetOutcome(ReconciliationReport report)
        {
            if (report.RejectedCount == 0)
            {
                return "Completed";
            }

            return report.AcceptedCount + report.DuplicateCount > 0 ? "Partial" : "Rejected";
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
            {
                return string.Concat(hash.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
        }

        private static void MoveDeterministically(string source, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (File.Exists(destination))
            {
                if (!string.Equals(ComputeSha256(source), ComputeSha256(destination), StringComparison.Ordinal))
                {
                    throw new IOException("A different file already exists at deterministic destination " + destination + ".");
                }

                File.Delete(source);
                return;
            }

            File.Move(source, destination);
        }
    }
}
