using System;
using System.Threading.Tasks;
using Npgsql;

namespace VnNotes
{
    /// <summary>
    /// Отвечает за управление пользователями.
    /// </summary>
    public sealed class UserService
    {
        private readonly SecurityLogService _logService;

        public UserService(SecurityLogService logService)
        {
            _logService = logService;
        }

        public async Task CreateUserAsync(
            NpgsqlConnection connection,
            UserSession currentUser,
            string username,
            string password,
            string role)
        {
            if (role != "admin" && role != "operator" && role != "user")
                throw new InvalidOperationException("Недопустимая роль пользователя.");

            string passwordHash = Md5Hasher.HashPassword(username, password);

            const string sql =
@"INSERT INTO app_users(username, password_hash, role)
VALUES (@username, @password_hash, @role);";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password_hash", passwordHash);
                command.Parameters.AddWithValue("@role", role);

                await command.ExecuteNonQueryAsync();
            }

            await _logService.WriteAsync(
                connection,
                currentUser.Id,
                "USER_CREATED",
                "Создан пользователь " + username + " с ролью " + role + ".");
        }

        public async Task PrintUsersAsync(NpgsqlConnection connection)
        {
            const string sql =
@"SELECT id, username, role, is_blocked, failed_attempts, created_at
FROM app_users
ORDER BY id;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
            {
                Console.WriteLine("Пользователи:");
                Console.WriteLine();

                while (await reader.ReadAsync())
                {
                    Console.WriteLine("ID: " + reader.GetInt32(0));
                    Console.WriteLine("Логин: " + reader.GetString(1));
                    Console.WriteLine("Роль: " + reader.GetString(2));
                    Console.WriteLine("Заблокирован: " + reader.GetBoolean(3));
                    Console.WriteLine("Ошибок входа: " + reader.GetInt32(4));
                    Console.WriteLine("Создан: " + reader.GetDateTime(5));
                    Console.WriteLine(new string('-', 50));
                }
            }
        }

        public async Task UnlockUserAsync(
            NpgsqlConnection connection,
            UserSession currentUser,
            string username)
        {
            const string sql =
@"UPDATE app_users
SET is_blocked = FALSE,
    failed_attempts = 0
WHERE username = @username;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@username", username);

                int affectedRows = await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    Console.WriteLine("Пользователь не найден.");
                    return;
                }
            }

            await _logService.WriteAsync(
                connection,
                currentUser.Id,
                "USER_UNLOCKED",
                "Разблокирован пользователь " + username + ".");

            Console.WriteLine("Пользователь разблокирован.");
        }
    }
}