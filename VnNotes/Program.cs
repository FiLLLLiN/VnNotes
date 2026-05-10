using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace VnNotes
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            AppConfig config;

            try
            {
                config = AppConfig.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка чтения App.config.");
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                return;
            }

            SecurityLogService logService = new SecurityLogService();
            UpdateService updateService = new UpdateService(logService);

            PrintStartBanner();

            await CheckUpdateBeforeLoginAsync(config, updateService);

            using (NpgsqlConnection connection = new NpgsqlConnection(config.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка подключения к PostgreSQL.");
                    Console.WriteLine(ex.Message);
                    Console.ReadKey();
                    return;
                }

                DatabaseService databaseService = new DatabaseService();

                try
                {
                    await databaseService.InitializeAsync(connection);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка автоматической инициализации базы данных.");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Проверь файл database/init.sql и права пользователя PostgreSQL.");
                    Console.ReadKey();
                    return;
                }

                AuthService authService = new AuthService(logService);
                UserService userService = new UserService(logService);
                NoteService noteService = new NoteService(logService);
                SystemMetricService metricService = new SystemMetricService(logService);
                CommandMapService commandMapService = new CommandMapService();

                UserSession currentUser = await LoginLoopAsync(connection, authService);

                if (currentUser == null)
                {
                    Console.WriteLine("Вход не выполнен.");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("Вход выполнен. Пользователь: " + currentUser.Username + ", роль: " + currentUser.Role);
                Console.WriteLine("Для просмотра команд введите: help");
                Console.WriteLine("Для выхода введите: exit");
                Console.WriteLine();

                bool isRunning = true;

                while (isRunning)
                {
                    Console.Write("vn> ");
                    string input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    isRunning = await ProcessInteractiveCommandAsync(
                        input,
                        config,
                        connection,
                        currentUser,
                        noteService,
                        userService,
                        metricService,
                        logService,
                        updateService,
                        commandMapService);
                }
            }
        }

        private static void PrintStartBanner()
        {
            Console.WriteLine("VN Notes — консольная система заметок и мониторинга");
            Console.WriteLine(new string('-', 60));
        }

        private static async Task CheckUpdateBeforeLoginAsync(AppConfig config, UpdateService updateService)
        {
            Console.WriteLine("Проверка обновлений...");

            UpdateInfo updateInfo = updateService.LoadUpdateInfo(config);

            if (updateInfo == null)
            {
                Console.WriteLine("Проверка обновлений пропущена.");
                Console.WriteLine();
                return;
            }

            bool hasNewVersion = updateService.IsNewVersionAvailable(config, updateInfo);

            Console.WriteLine("Текущая версия: " + config.ApplicationVersion);
            Console.WriteLine("Версия на GitHub: " + updateInfo.Version);

            if (!hasNewVersion)
            {
                Console.WriteLine("Установлена актуальная версия.");
                Console.WriteLine();
                return;
            }

            Console.WriteLine("Доступна новая версия приложения.");
            Console.Write("Скачать и запустить обновление сейчас? y/n: ");

            string answer = Console.ReadLine();

            if (string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(answer, "д", StringComparison.OrdinalIgnoreCase))
            {
                await updateService.ApplyUpdateAsync(config, null, null);
            }
            else
            {
                Console.WriteLine("Обновление пропущено. Можно выполнить его позже командой: update-apply");
                Console.WriteLine();
            }
        }

        private static async Task<UserSession> LoginLoopAsync(
            NpgsqlConnection connection,
            AuthService authService)
        {
            Console.WriteLine("Авторизация пользователя");
            Console.WriteLine(new string('-', 60));

            while (true)
            {
                Console.Write("Логин: ");
                string username = Console.ReadLine();

                Console.Write("Пароль: ");
                string password = ReadPassword();

                UserSession user = await authService.LoginAsync(connection, username, password);

                if (user != null)
                    return user;

                Console.WriteLine("Неверный логин или пароль, либо учетная запись заблокирована.");
                Console.Write("Повторить вход? y/n: ");

                string answer = Console.ReadLine();

                if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(answer, "д", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                Console.WriteLine();
            }
        }

        private static string ReadPassword()
        {
            StringBuilder password = new StringBuilder();

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }

                    continue;
                }

                password.Append(key.KeyChar);
                Console.Write("*");
            }

            return password.ToString();
        }

        private static async Task<bool> ProcessInteractiveCommandAsync(
            string input,
            AppConfig config,
            NpgsqlConnection connection,
            UserSession currentUser,
            NoteService noteService,
            UserService userService,
            SystemMetricService metricService,
            SecurityLogService logService,
            UpdateService updateService,
            CommandMapService commandMapService)
        {
            string[] parts = SplitCommandLine(input);

            if (parts.Length == 0)
                return true;

            if (string.Equals(parts[0], "vn", StringComparison.OrdinalIgnoreCase))
            {
                parts = RemoveFirst(parts);

                if (parts.Length == 0)
                    return true;
            }

            string command = parts[0];

            if (string.Equals(command, "exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Завершение работы.");
                return false;
            }

            if (string.Equals(command, "help", StringComparison.OrdinalIgnoreCase))
            {
                PrintInteractiveHelp();
                return true;
            }

            if (string.Equals(command, "version", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--version", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Версия приложения: " + config.ApplicationVersion);
                return true;
            }

            if (string.Equals(command, "map", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--map", StringComparison.OrdinalIgnoreCase))
            {
                await commandMapService.CreateMarkdownAsync("COMMANDS.md");
                Console.WriteLine("Файл COMMANDS.md успешно создан.");
                return true;
            }

            if (string.Equals(command, "add", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--addNewNote", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length < 2)
                {
                    Console.WriteLine("Укажи текст заметки. Пример: add \"Текст заметки\"");
                    return true;
                }

                string noteText = JoinFrom(parts, 1);

                await noteService.AddNoteAsync(connection, currentUser, noteText);
                Console.WriteLine("Заметка успешно добавлена.");
                return true;
            }

            if (string.Equals(command, "notes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--notes", StringComparison.OrdinalIgnoreCase))
            {
                await noteService.PrintNotesAsync(connection, currentUser);
                return true;
            }

            if (string.Equals(command, "delete", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--deleteNote", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length < 2)
                {
                    Console.WriteLine("Укажи ID заметки. Пример: delete 1");
                    return true;
                }

                int noteId;

                if (!int.TryParse(parts[1], out noteId))
                {
                    Console.WriteLine("ID заметки должен быть числом.");
                    return true;
                }

                await noteService.DeleteNoteAsync(connection, currentUser, noteId);
                return true;
            }

            if (string.Equals(command, "create-user", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--createUser", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.IsAdmin(currentUser))
                {
                    Console.WriteLine("Недостаточно прав. Создавать пользователей может только admin.");
                    return true;
                }

                if (parts.Length < 4)
                {
                    Console.WriteLine("Пример: create-user student 12345 user");
                    return true;
                }

                try
                {
                    await userService.CreateUserAsync(
                        connection,
                        currentUser,
                        parts[1],
                        parts[2],
                        parts[3]);

                    Console.WriteLine("Пользователь успешно создан.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка создания пользователя.");
                    Console.WriteLine(ex.Message);
                }

                return true;
            }

            if (string.Equals(command, "users", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--users", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.IsAdmin(currentUser))
                {
                    Console.WriteLine("Недостаточно прав. Просматривать пользователей может только admin.");
                    return true;
                }

                await userService.PrintUsersAsync(connection);
                return true;
            }

            if (string.Equals(command, "unlock", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--unlockUser", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.IsAdmin(currentUser))
                {
                    Console.WriteLine("Недостаточно прав. Разблокировать пользователей может только admin.");
                    return true;
                }

                if (parts.Length < 2)
                {
                    Console.WriteLine("Пример: unlock student");
                    return true;
                }

                await userService.UnlockUserAsync(connection, currentUser, parts[1]);
                return true;
            }

            if (string.Equals(command, "register-node", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--registerNode", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.HasAnyRole(currentUser, "admin", "operator"))
                {
                    Console.WriteLine("Недостаточно прав. Регистрировать узлы может admin или operator.");
                    return true;
                }

                if (parts.Length < 4)
                {
                    Console.WriteLine("Пример: register-node app-server-1 10.0.0.10 \"Сервер приложений\"");
                    return true;
                }

                await metricService.RegisterNodeAsync(
                    connection,
                    currentUser,
                    parts[1],
                    parts[2],
                    JoinFrom(parts, 3));

                Console.WriteLine("Узел инфраструктуры сохранен.");
                return true;
            }

            if (string.Equals(command, "metrics", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--metrics", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.HasAnyRole(currentUser, "admin", "operator"))
                {
                    Console.WriteLine("Недостаточно прав. Просматривать статистику может admin или operator.");
                    return true;
                }

                SystemMetric metric = await metricService.ReadCurrentMetricAsync();
                metricService.PrintMetric(metric);

                await logService.WriteAsync(
                    connection,
                    currentUser.Id,
                    "METRICS_VIEW",
                    "Пользователь просмотрел текущую статистику.");

                return true;
            }

            if (string.Equals(command, "save-metrics", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--saveMetrics", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.HasAnyRole(currentUser, "admin", "operator"))
                {
                    Console.WriteLine("Недостаточно прав. Сохранять статистику может admin или operator.");
                    return true;
                }

                await metricService.SaveCurrentMetricAsync(connection, currentUser);
                Console.WriteLine("Статистика текущего устройства сохранена.");
                return true;
            }

            if (string.Equals(command, "metrics-list", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--metrics-list", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.HasAnyRole(currentUser, "admin", "operator"))
                {
                    Console.WriteLine("Недостаточно прав. Просматривать статистику может admin или operator.");
                    return true;
                }

                await metricService.PrintLatestMetricsAsync(connection);

                await logService.WriteAsync(
                    connection,
                    currentUser.Id,
                    "METRICS_LIST_VIEW",
                    "Пользователь просмотрел статистику узлов.");

                return true;
            }

            if (string.Equals(command, "logs", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--logs", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.IsAdmin(currentUser))
                {
                    Console.WriteLine("Недостаточно прав. Просмотр журнала доступен только admin.");
                    return true;
                }

                int count = 20;

                if (parts.Length >= 2)
                    int.TryParse(parts[1], out count);

                await logService.PrintLastLogsAsync(connection, count);

                await logService.WriteAsync(
                    connection,
                    currentUser.Id,
                    "LOGS_VIEW",
                    "Пользователь просмотрел журнал безопасности.");

                return true;
            }

            if (string.Equals(command, "update-check", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--update-check", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.IsAdmin(currentUser))
                {
                    Console.WriteLine("Недостаточно прав. Проверять обновления может только admin.");
                    return true;
                }

                await updateService.CheckUpdateAsync(config, connection, currentUser);
                return true;
            }

            if (string.Equals(command, "update-download", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--update-download", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.IsAdmin(currentUser))
                {
                    Console.WriteLine("Недостаточно прав. Загружать обновления может только admin.");
                    return true;
                }

                await updateService.DownloadUpdateAsync(config, connection, currentUser);
                return true;
            }

            if (string.Equals(command, "update-apply", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "--update-apply", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleGuard.IsAdmin(currentUser))
                {
                    Console.WriteLine("Недостаточно прав. Применять обновления может только admin.");
                    return true;
                }

                await updateService.ApplyUpdateAsync(config, connection, currentUser);
                return true;
            }

            Console.WriteLine("Команда не распознана. Введите help для просмотра команд.");
            return true;
        }

        private static void PrintInteractiveHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("help");
            Console.WriteLine("version");
            Console.WriteLine("map");
            Console.WriteLine("add \"Текст заметки\"");
            Console.WriteLine("notes");
            Console.WriteLine("delete 1");
            Console.WriteLine("create-user student 12345 user");
            Console.WriteLine("users");
            Console.WriteLine("unlock student");
            Console.WriteLine("register-node app-server-1 10.0.0.10 \"Сервер приложений\"");
            Console.WriteLine("metrics");
            Console.WriteLine("save-metrics");
            Console.WriteLine("metrics-list");
            Console.WriteLine("logs 20");
            Console.WriteLine("update-check");
            Console.WriteLine("update-download");
            Console.WriteLine("update-apply");
            Console.WriteLine("exit");
            Console.WriteLine();
        }

        private static string[] SplitCommandLine(string input)
        {
            List<string> result = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char symbol = input[i];

                if (symbol == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(symbol) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }

                    continue;
                }

                current.Append(symbol);
            }

            if (current.Length > 0)
                result.Add(current.ToString());

            return result.ToArray();
        }

        private static string[] RemoveFirst(string[] source)
        {
            if (source.Length <= 1)
                return new string[0];

            string[] result = new string[source.Length - 1];

            for (int i = 1; i < source.Length; i++)
                result[i - 1] = source[i];

            return result;
        }

        private static string JoinFrom(string[] source, int startIndex)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = startIndex; i < source.Length; i++)
            {
                if (i > startIndex)
                    builder.Append(" ");

                builder.Append(source[i]);
            }

            return builder.ToString();
        }
    }
}