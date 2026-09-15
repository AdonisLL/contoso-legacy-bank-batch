using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using Contoso.LegacyBank.Batch;

namespace Contoso.LegacyBank.Batch.Tests
{
    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            Run("CSV parser accepts quoted fields", ParserAcceptsQuotedFields);
            Run("CSV parser rejects malformed rows", ParserRejectsMalformedRows);
            Run("Transaction validation enforces business rules", ValidatorEnforcesRules);
            Run("WCF importer reconciles partial and replay batches", WcfIntegrationProcessesAndReplays);

            Console.WriteLine("{0} test(s) failed.", failures);
            return failures == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS " + name);
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine("FAIL " + name + ": " + ex);
            }
        }

        private static void ParserAcceptsQuotedFields()
        {
            string directory = CreateTestDirectory();
            try
            {
                string path = Path.Combine(directory, "quoted.csv");
                File.WriteAllText(path,
                    "ExternalId,AccountNumber,PostedDate,Description,Amount,TransactionType\r\n" +
                    "TX-1,10001,2026-09-15,\"Payroll, September\",1250.50,Deposit\r\n");

                ParsedCsvFile result = new CsvParser().Parse(path);
                Equal(1, result.Records.Count, "record count");
                Equal(0, result.Errors.Count, "error count");
                Equal("Payroll, September", result.Records[0].Description, "quoted description");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void ParserRejectsMalformedRows()
        {
            string directory = CreateTestDirectory();
            try
            {
                string path = Path.Combine(directory, "malformed.csv");
                File.WriteAllText(path,
                    "ExternalId,AccountNumber,PostedDate,Description,Amount,TransactionType\r\n" +
                    "TX-1,10001,2026-09-15,\"unterminated,12.00,Deposit\r\n" +
                    "TX-2,10002,2026-09-15,Too,Few\r\n" +
                    "TX-3,10003,2026-09-15,\"quoted\"junk,2.00,Deposit\r\n");

                ParsedCsvFile result = new CsvParser().Parse(path);
                Equal(0, result.Records.Count, "record count");
                Equal(3, result.Errors.Count, "error count");
                Equal("MalformedCsv", result.Errors[0].Code, "first error");
                Equal("ColumnCount", result.Errors[1].Code, "second error");
                Equal("MalformedCsv", result.Errors[2].Code, "third error");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void ValidatorEnforcesRules()
        {
            var validator = new TransactionValidator();
            ValidatedTransaction transaction;
            IList<RowError> errors;
            bool valid = validator.TryValidate(new CsvRecord
            {
                RowNumber = 2,
                ExternalId = "",
                AccountNumber = "10001",
                PostedDate = "09/15/2026",
                Description = "Bad",
                Amount = "-1.00",
                TransactionType = "Transfer"
            }, out transaction, out errors);

            False(valid, "invalid record");
            Equal(4, errors.Count, "validation error count");

            valid = validator.TryValidate(new CsvRecord
            {
                RowNumber = 3,
                ExternalId = "TX-OK",
                AccountNumber = "10001",
                PostedDate = "2026-09-15",
                Description = "Cash deposit",
                Amount = "25.10",
                TransactionType = "deposit"
            }, out transaction, out errors);

            True(valid, "valid record");
            Equal("Deposit", transaction.TransactionType, "normalized transaction type");
            Equal(25.10m, transaction.Amount, "amount");
        }

        private static void WcfIntegrationProcessesAndReplays()
        {
            string root = CreateTestDirectory();
            int port = GetAvailablePort();
            var endpoint = new Uri("http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture) + "/AccountService");
            var service = new TestAccountService();
            var host = new ServiceHost(service, endpoint);
            var serviceBinding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                HostNameComparisonMode = HostNameComparisonMode.Exact
            };
            host.AddServiceEndpoint(typeof(IAccountService), serviceBinding, string.Empty);

            try
            {
                host.Open();
                BatchSettings settings = Settings(root, endpoint);
                settings.EnsureDirectories();
                string inputPath = Path.Combine(settings.InputDirectory, "partial.csv");
                string content =
                    "ExternalId,AccountNumber,PostedDate,Description,Amount,TransactionType\r\n" +
                    "TX-100,10001,2026-09-15,Deposit,100.00,Deposit\r\n" +
                    "TX-100,10001,2026-09-15,Duplicate,100.00,Deposit\r\n" +
                    "TX-BAD,10001,2026-09-15,Bad amount,nope,Deposit\r\n" +
                    "TX-200,10001,2026-09-15,Withdrawal,10.00,Withdrawal\r\n";
                File.WriteAllText(inputPath, content);

                ReconciliationReport first;
                using (var gateway = new AccountTransactionGateway(endpoint, TimeSpan.FromSeconds(5)))
                {
                    first = Processor(settings, gateway).ProcessFile(inputPath);
                }

                Equal(2, first.AcceptedCount, "first accepted");
                Equal(1, first.DuplicateCount, "first duplicate");
                Equal(1, first.RejectedCount, "first rejected");
                Equal("Partial", first.Outcome, "first outcome");
                True(File.Exists(first.DestinationFile), "partial file moved to Error");
                Equal(settings.ErrorDirectory, Path.GetDirectoryName(first.DestinationFile), "error destination");
                Equal(2, Directory.GetFiles(settings.ReportsDirectory, "partial.*.reconciliation.*").Length, "report formats");

                File.Copy(first.DestinationFile, inputPath);
                ReconciliationReport replay;
                using (var gateway = new AccountTransactionGateway(endpoint, TimeSpan.FromSeconds(5)))
                {
                    replay = Processor(settings, gateway).ProcessFile(inputPath);
                }

                Equal(first.FileHash, replay.FileHash, "stable content hash");
                Equal(first.DestinationFile, replay.DestinationFile, "stable destination");
                Equal(0, replay.AcceptedCount, "replay accepted");
                Equal(3, replay.DuplicateCount, "replay duplicates");
                Equal(1, replay.RejectedCount, "replay rejected");
                Equal(6, service.CallCount, "only valid rows call service");
            }
            finally
            {
                try
                {
                    host.Close();
                }
                catch
                {
                    host.Abort();
                }
                DeleteDirectory(root);
            }
        }

        private static BatchProcessor Processor(BatchSettings settings, IAccountTransactionGateway gateway)
        {
            return new BatchProcessor(settings, new CsvParser(), new TransactionValidator(), gateway,
                new ReconciliationReportWriter());
        }

        private static BatchSettings Settings(string root, Uri endpoint)
        {
            return new BatchSettings
            {
                RootDirectory = root,
                InputDirectory = Path.Combine(root, "Input"),
                ArchiveDirectory = Path.Combine(root, "Archive"),
                ErrorDirectory = Path.Combine(root, "Error"),
                ReportsDirectory = Path.Combine(root, "Reports"),
                AccountServiceEndpoint = endpoint,
                ServiceTimeout = TimeSpan.FromSeconds(5)
            };
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string CreateTestDirectory()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ContosoLegacyBank",
                "BatchTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Expected true: " + message);
            }
        }

        private static void False(bool condition, string message)
        {
            True(!condition, message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "{0}: expected <{1}>, actual <{2}>.", message, expected, actual));
            }
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal sealed class TestAccountService : IAccountService
    {
        private readonly HashSet<string> externalIds = new HashSet<string>(StringComparer.Ordinal);

        public int CallCount { get; private set; }

        public TransactionDto ImportTransaction(ImportTransactionRequest request)
        {
            CallCount++;
            if (!externalIds.Add(request.ExternalId))
            {
                throw new FaultException<AccountFault>(
                    new AccountFault { Code = "DuplicateExternalId", Message = "Duplicate ExternalId." });
            }

            return new TransactionDto
            {
                ExternalId = request.ExternalId,
                AccountNumber = request.AccountNumber,
                PostedUtc = request.PostedUtc,
                Description = request.Description,
                Amount = request.Amount,
                TransactionType = request.TransactionType
            };
        }
    }
}
