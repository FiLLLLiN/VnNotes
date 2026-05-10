using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;

namespace VnNotes
{
    /// <summary>
    /// Проверяет, загружает и запускает обновление приложения.
    /// </summary>
    public sealed class UpdateService
    {
        private readonly SecurityLogService _logService;

        public UpdateService(SecurityLogService logService)
        {
            _logService = logService;
        }

        public UpdateInfo LoadUpdateInfo(AppConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.UpdateInfoUrl))
            {
                Console.WriteLine("В App.config не задан UpdateInfoUrl.");
                return null;
            }

            try
            {
                using (WebClient client = new WebClient())
                {
                    string json = client.DownloadString(config.UpdateInfoUrl);

                    UpdateInfo info = new UpdateInfo();
                    info.Version = ExtractJsonValue(json, "version");
                    info.DownloadUrl = ExtractJsonValue(json, "downloadUrl");

                    if (string.IsNullOrWhiteSpace(info.Version))
                        return null;

                    return info;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка проверки обновлений.");
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public bool IsNewVersionAvailable(AppConfig config, UpdateInfo updateInfo)
        {
            if (updateInfo == null)
                return false;

            Version currentVersion = ParseVersion(config.ApplicationVersion);
            Version remoteVersion = ParseVersion(updateInfo.Version);

            return remoteVersion > currentVersion;
        }

        public async Task CheckUpdateAsync(
            AppConfig config,
            NpgsqlConnection connection,
            UserSession user)
        {
            UpdateInfo updateInfo = LoadUpdateInfo(config);

            if (updateInfo == null)
                return;

            Version currentVersion = ParseVersion(config.ApplicationVersion);
            Version remoteVersion = ParseVersion(updateInfo.Version);

            Console.WriteLine("Текущая версия: " + config.ApplicationVersion);
            Console.WriteLine("Версия на GitHub: " + updateInfo.Version);

            if (remoteVersion > currentVersion)
                Console.WriteLine("Доступна новая версия приложения.");
            else
                Console.WriteLine("Установлена актуальная версия приложения.");

            if (connection != null && user != null)
            {
                await _logService.WriteAsync(
                    connection,
                    user.Id,
                    "UPDATE_CHECK",
                    "Выполнена проверка обновлений.");
            }
        }

        public async Task DownloadUpdateAsync(
            AppConfig config,
            NpgsqlConnection connection,
            UserSession user)
        {
            UpdateInfo updateInfo = LoadUpdateInfo(config);

            if (updateInfo == null)
                return;

            Version currentVersion = ParseVersion(config.ApplicationVersion);
            Version remoteVersion = ParseVersion(updateInfo.Version);

            if (remoteVersion <= currentVersion)
            {
                Console.WriteLine("Новая версия не найдена.");
                return;
            }

            if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
            {
                Console.WriteLine("В version.json не указана ссылка downloadUrl.");
                return;
            }

            Directory.CreateDirectory("updates");

            string savePath = Path.Combine("updates", "VnNotes-v" + updateInfo.Version + ".zip");

            try
            {
                using (WebClient client = new WebClient())
                {
                    Console.WriteLine("Загрузка обновления...");
                    client.DownloadFile(updateInfo.DownloadUrl, savePath);
                }

                Console.WriteLine("Обновление загружено:");
                Console.WriteLine(Path.GetFullPath(savePath));

                if (connection != null && user != null)
                {
                    await _logService.WriteAsync(
                        connection,
                        user.Id,
                        "UPDATE_DOWNLOADED",
                        "Загружено обновление версии " + updateInfo.Version + ".");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки обновления.");
                Console.WriteLine(ex.Message);
            }
        }

        public async Task ApplyUpdateAsync(
            AppConfig config,
            NpgsqlConnection connection,
            UserSession user)
        {
            UpdateInfo updateInfo = LoadUpdateInfo(config);

            if (updateInfo == null)
                return;

            Version currentVersion = ParseVersion(config.ApplicationVersion);
            Version remoteVersion = ParseVersion(updateInfo.Version);

            if (remoteVersion <= currentVersion)
            {
                Console.WriteLine("Обновление не требуется.");
                return;
            }

            await DownloadUpdateAsync(config, connection, user);

            string packagePath = Path.GetFullPath(Path.Combine("updates", "VnNotes-v" + updateInfo.Version + ".zip"));
            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.UpdaterFileName);

            if (!File.Exists(updaterPath))
            {
                Console.WriteLine("Файл обновлятора не найден:");
                Console.WriteLine(updaterPath);
                return;
            }

            string targetPath = AppDomain.CurrentDomain.BaseDirectory;

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = updaterPath;
            startInfo.Arguments = "--package \"" + packagePath + "\" --target \"" + targetPath + "\" --wait 2000";
            startInfo.UseShellExecute = true;

            Process.Start(startInfo);

            if (connection != null && user != null)
            {
                await _logService.WriteAsync(
                    connection,
                    user.Id,
                    "UPDATE_APPLY",
                    "Запущена установка обновления версии " + updateInfo.Version + ".");
            }

            Console.WriteLine("Запущено автоматическое обновление. Основная программа закрывается.");
            Environment.Exit(0);
        }

        private static string ExtractJsonValue(string json, string key)
        {
            string pattern = "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"";
            Match match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);

            if (!match.Success)
                return "";

            return match.Groups[1].Value;
        }

        private static Version ParseVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Version(0, 0, 0);

            value = value.Trim().TrimStart('v', 'V');

            Version version;

            if (Version.TryParse(value, out version))
                return version;

            return new Version(0, 0, 0);
        }
    }
}