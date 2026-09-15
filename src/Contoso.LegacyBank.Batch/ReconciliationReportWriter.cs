using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Contoso.LegacyBank.Batch
{
    public sealed class ReconciliationReportWriter
    {
        public void Write(ReconciliationReport report, string reportsDirectory, string reportBaseName)
        {
            Directory.CreateDirectory(reportsDirectory);
            WriteJson(report, Path.Combine(reportsDirectory, reportBaseName + ".reconciliation.json"));
            WriteCsv(report, Path.Combine(reportsDirectory, reportBaseName + ".reconciliation.csv"));
        }

        private static void WriteJson(ReconciliationReport report, string path)
        {
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(report);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private static void WriteCsv(ReconciliationReport report, string path)
        {
            var output = new StringBuilder();
            output.AppendLine("RecordType,SourceFile,FileHash,ProcessedUtc,Outcome,TotalRows,AcceptedCount,DuplicateCount,RejectedCount,DestinationFile,RowNumber,ExternalId,ErrorCode,ErrorMessage");
            output.Append("Summary,")
                .Append(Escape(report.SourceFile)).Append(',')
                .Append(Escape(report.FileHash)).Append(',')
                .Append(Escape(report.ProcessedUtc.ToString("o", CultureInfo.InvariantCulture))).Append(',')
                .Append(Escape(report.Outcome)).Append(',')
                .Append(report.TotalRows.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(report.AcceptedCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(report.DuplicateCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(report.RejectedCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Escape(report.DestinationFile))
                .AppendLine(",,,,");

            foreach (RowError error in report.RowErrors.OrderBy(item => item.RowNumber).ThenBy(item => item.Code, StringComparer.Ordinal))
            {
                output.Append("Error,,,,,,,,,,")
                    .Append(error.RowNumber.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(Escape(error.ExternalId)).Append(',')
                    .Append(Escape(error.Code)).Append(',')
                    .Append(Escape(error.Message))
                    .AppendLine();
            }

            File.WriteAllText(path, output.ToString(), new UTF8Encoding(false));
        }

        private static string Escape(string value)
        {
            string safe = value ?? string.Empty;
            if (safe.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return safe;
            }

            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }
    }
}
