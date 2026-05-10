using System;
using System.Threading.Tasks;
using Npgsql;

namespace VnNotes
{
    /// <summary>
    /// Отвечает за авторизацию пользователей.
    /// </summary>
    public sealed class AuthService
    {
        private readonly SecurityLogService _logService;

        public AuthService(SecurityLogService logService)
        {
            _logService = logService;
        }

        public async Task<UserSession> LoginAsync(
            NpgsqlConnection connection,
            string username,
            string password)
        {
            const string sql =
@"SELECT id, username, password_hash, role, is_blocked, failed_attempts
FROM app_users
WHERE username = @username;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@username", username);

                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        reader.Close();

                        await _logService.WriteAsync(
                            connection,
                            null,
                            "AUTH_FAILED",
                            "Попытка входа под несуществующим пользователем: " + username);

                        return null;
                    }

                    int userId = reader.GetInt32(0);
                    string dbUsername = reader.GetString(1);
                    string passwordHash = reader.GetString(2);
                    string role = reader.GetString(3);
                    bool isBlocked = reader.GetBoolean(4);
                    int failedAttempts = reader.GetInt32(5);

                    reader.Close();

                    if (isBlocked)
                    {
                        await _logService.WriteAsync(
                            connection,
                            userId,
                            "AUTH_BLOCKED",
                            "Попытка входа в заблокированную учетную запись.");

                        return null;
                    }

                    string inputHash = Md5Hasher.HashPassword(username, password);

                    if (!string.Equals(inputHash, passwordHash, StringComparison.OrdinalIgnoreCase))
                    {
                        await IncreaseFailedAttemptsAsync(connection, userId, failedAttempts);

                        await _logService.WriteAsync(
                            connection,
                            userId,
                            "AUTH_FAILED",
                            "Введен неверный пароль.");

                        return null;
                    }

                    await ResetFailedAttemptsAsync(connection, userId);

                    await _logService.WriteAsync(
                        connection,
                        userId,
                        "AUTH_SUCCESS",
                        "Пользователь успешно вошел в систему.");

                    return new UserSession(userId, dbUsername, role);
                }
            }
        }

        private static async Task IncreaseFailedAttemptsAsync(
            NpgsqlConnection connection,
            int userId,
            int currentAttempts)
        {
            int newAttempts = currentAttempts + 1;

            const string sql =
@"UPDATE app_users
SET
    failed_attempts = @failed_attempts,
    is_blocked = CASE WHEN @failed_attempts >= 5 THEN TRUE ELSE is_blocked END
WHERE id = @id;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@failed_attempts", newAttempts);
                command.Parameters.AddWithValue("@id", userId);

                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task ResetFailedAttemptsAsync(
            NpgsqlConnection connection,
            int userId)
        {
            const string sql =
@"UPDATE app_users
SET failed_attempts = 0
WHERE id = @id;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", userId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}