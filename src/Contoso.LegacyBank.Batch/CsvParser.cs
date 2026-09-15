using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Contoso.LegacyBank.Batch
{
    public sealed class CsvParser
    {
        public static readonly string[] RequiredHeaders =
        {
            "ExternalId", "AccountNumber", "PostedDate", "Description", "Amount", "TransactionType"
        };

        public ParsedCsvFile Parse(string path)
        {
            var result = new ParsedCsvFile();
            string[] lines;

            try
            {
                lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
            }
            catch (Exception ex)
            {
                result.Errors.Add(Error(0, null, "FileReadError", ex.Message));
                return result;
            }

            if (lines.Length == 0)
            {
                result.Errors.Add(Error(1, null, "MissingHeader", "The CSV file is empty."));
                return result;
            }

            string headerError;
            IList<string> headers;
            if (!TryParseLine(RemoveBom(lines[0]), out headers, out headerError))
            {
                result.Errors.Add(Error(1, null, "MalformedHeader", headerError));
                return result;
            }

            if (!headers.SequenceEqual(RequiredHeaders, StringComparer.Ordinal))
            {
                result.Errors.Add(Error(1, null, "InvalidHeader",
                    "Expected header: " + string.Join(",", RequiredHeaders) + "."));
                return result;
            }

            result.HeaderValid = true;
            for (int index = 1; index < lines.Length; index++)
            {
                int rowNumber = index + 1;
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    result.Errors.Add(Error(rowNumber, null, "BlankRow", "Blank rows are not allowed."));
                    continue;
                }

                IList<string> fields;
                string parseError;
                if (!TryParseLine(lines[index], out fields, out parseError))
                {
                    result.Errors.Add(Error(rowNumber, null, "MalformedCsv", parseError));
                    continue;
                }

                if (fields.Count != RequiredHeaders.Length)
                {
                    string externalId = fields.Count > 0 ? fields[0] : null;
                    result.Errors.Add(Error(rowNumber, externalId, "ColumnCount",
                        "Expected 6 columns but found " + fields.Count + "."));
                    continue;
                }

                result.Records.Add(new CsvRecord
                {
                    RowNumber = rowNumber,
                    ExternalId = fields[0],
                    AccountNumber = fields[1],
                    PostedDate = fields[2],
                    Description = fields[3],
                    Amount = fields[4],
                    TransactionType = fields[5]
                });
            }

            return result;
        }

        public static bool TryParseLine(string line, out IList<string> fields, out string error)
        {
            fields = new List<string>();
            error = null;
            var value = new StringBuilder();
            bool inQuotes = false;
            bool closedQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char current = line[i];
                if (inQuotes)
                {
                    if (current == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            value.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                            closedQuote = true;
                        }
                    }
                    else
                    {
                        value.Append(current);
                    }
                }
                else if (current == ',')
                {
                    fields.Add(value.ToString());
                    value.Clear();
                    closedQuote = false;
                }
                else if (current == '"' && value.Length == 0 && !closedQuote)
                {
                    inQuotes = true;
                }
                else if (closedQuote)
                {
                    error = "Only a comma or end of row may follow a closing quote.";
                    return false;
                }
                else if (current == '"')
                {
                    error = "A quote may only begin a quoted field.";
                    return false;
                }
                else
                {
                    value.Append(current);
                }
            }

            if (inQuotes)
            {
                error = "The row contains an unterminated quoted field.";
                return false;
            }

            fields.Add(value.ToString());
            return true;
        }

        private static string RemoveBom(string value)
        {
            return value.Length > 0 && value[0] == '\uFEFF' ? value.Substring(1) : value;
        }

        private static RowError Error(int row, string externalId, string code, string message)
        {
            return new RowError { RowNumber = row, ExternalId = externalId, Code = code, Message = message };
        }
    }
}
