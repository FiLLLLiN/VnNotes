using System;
using System.Threading.Tasks;
using Npgsql;

namespace VnNotes
{
    /// <summary>
    /// Отвечает за запись и просмотр журнала безопасности.
    /// </summary>
    public sealed class SecurityLogService
    {
        public async Task WriteAsync(
            NpgsqlConnection connection,
            int? userId,
            string action,
            string details)
        {
            const string sql =
@"INSERT INTO security_logs(user_id, action, details, host_name)
VALUES (@user_id, @action, @details, @host_name);";

            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@user_id", userId.HasValue ? (object)userId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@action", action);
                    command.Parameters.AddWithValue("@details", details);
                    command.Parameters.AddWithValue("@host_name", Environment.MachineName);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Ошибка записи журнала не должна останавливать работу программы.
            }
        }

        public async Task PrintLastLogsAsync(NpgsqlConnection connection, int count)
        {
            const string sql =
@"SELECT
    l.id,
    u.username,
    l.action,
    l.details,
    l.host_name,
    l.created_at
FROM security_logs l
LEFT JOIN app_users u ON u.id = l.user_id
ORDER BY l.created_at DESC
LIMIT @count;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@count", count);

                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    Console.WriteLine("Журнал безопасности:");
                    Console.WriteLine();

                    bool hasRows = false;

                    while (await reader.ReadAsync())
                    {
                        hasRows = true;

                        Console.WriteLine("ID: " + reader.GetInt32(0));
                        Console.WriteLine("Пользователь: " + (reader.IsDBNull(1) ? "неизвестно" : reader.GetString(1)));
                        Console.WriteLine("Действие: " + reader.GetString(2));
                        Console.WriteLine("Описание: " + reader.GetString(3));
                        Console.WriteLine("Узел: " + reader.GetString(4));
                        Console.WriteLine("Дата: " + reader.GetDateTime(5));
                        Console.WriteLine(new string('-', 50));
                    }

                    if (!hasRows)
                        Console.WriteLine("Журнал безопасности пуст.");
                }
            }
        }
    }
}
