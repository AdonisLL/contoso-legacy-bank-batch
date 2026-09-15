using System;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace Contoso.LegacyBank.Batch
{
    public sealed class BatchSettings
    {
        public string RootDirectory { get; set; }
        public string InputDirectory { get; set; }
        public string ArchiveDirectory { get; set; }
        public string ErrorDirectory { get; set; }
        public string ReportsDirectory { get; set; }
        public Uri AccountServiceEndpoint { get; set; }
        public TimeSpan ServiceTimeout { get; set; }

        public static BatchSettings Load()
        {
            string root = Environment.ExpandEnvironmentVariables(Get("BatchRootDirectory", "%LOCALAPPDATA%\\ContosoLegacyBank\\Batch"));
            int timeoutSeconds;
            if (!int.TryParse(Get("ServiceTimeoutSeconds", "30"), NumberStyles.None, CultureInfo.InvariantCulture, out timeoutSeconds) || timeoutSeconds <= 0)
            {
                throw new ConfigurationErrorsException("ServiceTimeoutSeconds must be a positive integer.");
            }

            return new BatchSettings
            {
                RootDirectory = root,
                InputDirectory = Path.Combine(root, Get("InputDirectoryName", "Input")),
                ArchiveDirectory = Path.Combine(root, Get("ArchiveDirectoryName", "Archive")),
                ErrorDirectory = Path.Combine(root, Get("ErrorDirectoryName", "Error")),
                ReportsDirectory = Path.Combine(root, Get("ReportsDirectoryName", "Reports")),
                AccountServiceEndpoint = new Uri(Get("AccountServiceEndpoint", "http://localhost:8090/AccountService"), UriKind.Absolute),
                ServiceTimeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
        }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(InputDirectory);
            Directory.CreateDirectory(ArchiveDirectory);
            Directory.CreateDirectory(ErrorDirectory);
            Directory.CreateDirectory(ReportsDirectory);
        }

        private static string Get(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
