using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Contoso.LegacyBank.Batch
{
    public sealed class CsvRecord
    {
        public int RowNumber { get; set; }
        public string ExternalId { get; set; }
        public string AccountNumber { get; set; }
        public string PostedDate { get; set; }
        public string Description { get; set; }
        public string Amount { get; set; }
        public string TransactionType { get; set; }
    }

    public sealed class ParsedCsvFile
    {
        public ParsedCsvFile()
        {
            Records = new List<CsvRecord>();
            Errors = new List<RowError>();
        }

        public IList<CsvRecord> Records { get; private set; }
        public IList<RowError> Errors { get; private set; }
        public bool HeaderValid { get; set; }
    }

    public sealed class ValidatedTransaction
    {
        public int RowNumber { get; set; }
        public string ExternalId { get; set; }
        public string AccountNumber { get; set; }
        public DateTime PostedDate { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
    }

    [DataContract]
    public sealed class RowError
    {
        [DataMember(Order = 1)]
        public int RowNumber { get; set; }

        [DataMember(Order = 2)]
        public string ExternalId { get; set; }

        [DataMember(Order = 3)]
        public string Code { get; set; }

        [DataMember(Order = 4)]
        public string Message { get; set; }
    }

    [DataContract]
    public sealed class ReconciliationReport
    {
        public ReconciliationReport()
        {
            RowErrors = new List<RowError>();
        }

        [DataMember(Order = 1)]
        public string SourceFile { get; set; }

        [DataMember(Order = 2)]
        public string FileHash { get; set; }

        [DataMember(Order = 3)]
        public DateTime ProcessedUtc { get; set; }

        [DataMember(Order = 4)]
        public int TotalRows { get; set; }

        [DataMember(Order = 5)]
        public int AcceptedCount { get; set; }

        [DataMember(Order = 6)]
        public int DuplicateCount { get; set; }

        [DataMember(Order = 7)]
        public int RejectedCount { get; set; }

        [DataMember(Order = 8)]
        public string Outcome { get; set; }

        [DataMember(Order = 9)]
        public string DestinationFile { get; set; }

        [DataMember(Order = 10)]
        public IList<RowError> RowErrors { get; private set; }
    }

    public sealed class BatchRunResult
    {
        public int FilesProcessed { get; set; }
        public int AcceptedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int RejectedCount { get; set; }
    }
}
