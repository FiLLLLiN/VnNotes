using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VnNotes
{
    /// <summary>
    /// Загружает настройки приложения из App.config.
    /// </summary>
    public sealed class AppConfig
    {
        public string ConnectionString { get; private set; }
        public string ApplicationVersion { get; private set; }
        public string UpdateInfoUrl { get; private set; }
        public bool AutoUpdateOnStart { get; private set; }
        public string UpdaterFileName { get; private set; }

        private AppConfig(
            string connectionString,
            string applicationVersion,
            string updateInfoUrl,
            bool autoUpdateOnStart,
            string updaterFileName)
        {
            ConnectionString = connectionString;
            ApplicationVersion = applicationVersion;
            UpdateInfoUrl = updateInfoUrl;
            AutoUpdateOnStart = autoUpdateOnStart;
            UpdaterFileName = updaterFileName;
        }

        public static AppConfig Load()
        {
            string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            string applicationVersion = ConfigurationManager.AppSettings["ApplicationVersion"];
            string updateInfoUrl = ConfigurationManager.AppSettings["UpdateInfoUrl"];
            string autoUpdateText = ConfigurationManager.AppSettings["AutoUpdateOnStart"];
            string updaterFileName = ConfigurationManager.AppSettings["UpdaterFileName"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("В App.config не задан параметр ConnectionString.");

            if (string.IsNullOrWhiteSpace(applicationVersion))
                applicationVersion = "1.0.0";

            if (string.IsNullOrWhiteSpace(updateInfoUrl))
                updateInfoUrl = "";

            bool autoUpdateOnStart;

            if (!bool.TryParse(autoUpdateText, out autoUpdateOnStart))
                autoUpdateOnStart = false;

            if (string.IsNullOrWhiteSpace(updaterFileName))
                updaterFileName = "VnUpdater.exe";

            return new AppConfig(
                connectionString,
                applicationVersion,
                updateInfoUrl,
                autoUpdateOnStart,
                updaterFileName);
        }
    }
}
