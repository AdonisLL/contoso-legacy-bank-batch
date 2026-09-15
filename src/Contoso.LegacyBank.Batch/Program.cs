using System;

namespace Contoso.LegacyBank.Batch
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                BatchSettings settings = BatchSettings.Load();
                settings.EnsureDirectories();
                using (var gateway = new AccountTransactionGateway(settings.AccountServiceEndpoint, settings.ServiceTimeout))
                {
                    var processor = new BatchProcessor(
                        settings,
                        new CsvParser(),
                        new TransactionValidator(),
                        gateway,
                        new ReconciliationReportWriter());
                    BatchRunResult result = processor.ProcessAll();
                    Console.WriteLine(
                        "Files={0}; Accepted={1}; Duplicates={2}; Rejected={3}",
                        result.FilesProcessed,
                        result.AcceptedCount,
                        result.DuplicateCount,
                        result.RejectedCount);
                    return result.RejectedCount == 0 ? 0 : 2;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fatal batch error: " + ex);
                return 1;
            }
        }
    }
}
