using System;
using System.Threading.Tasks;
using Npgsql;

namespace VnNotes
{
    /// <summary>
    /// Отвечает за создание, просмотр и удаление заметок.
    /// </summary>
    public sealed class NoteService
    {
        private readonly SecurityLogService _logService;

        public NoteService(SecurityLogService logService)
        {
            _logService = logService;
        }

        public async Task AddNoteAsync(
            NpgsqlConnection connection,
            UserSession user,
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Текст заметки не может быть пустым.");

            const string sql =
@"INSERT INTO notes(user_id, note_text)
VALUES (@user_id, @note_text);";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@user_id", user.Id);
                command.Parameters.AddWithValue("@note_text", text);

                await command.ExecuteNonQueryAsync();
            }

            await _logService.WriteAsync(
                connection,
                user.Id,
                "NOTE_CREATED",
                "Пользователь создал заметку.");
        }

        public async Task PrintNotesAsync(
            NpgsqlConnection connection,
            UserSession user)
        {
            bool isAdmin = RoleGuard.IsAdmin(user);

            string sql = isAdmin
                ? @"SELECT n.id, u.username, n.note_text, n.created_at
FROM notes n
JOIN app_users u ON u.id = n.user_id
ORDER BY n.created_at DESC;"
                : @"SELECT n.id, u.username, n.note_text, n.created_at
FROM notes n
JOIN app_users u ON u.id = n.user_id
WHERE n.user_id = @user_id
ORDER BY n.created_at DESC;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                if (!isAdmin)
                    command.Parameters.AddWithValue("@user_id", user.Id);

                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    Console.WriteLine("Заметки:");
                    Console.WriteLine();

                    bool hasRows = false;

                    while (await reader.ReadAsync())
                    {
                        hasRows = true;

                        Console.WriteLine("ID: " + reader.GetInt32(0));
                        Console.WriteLine("Автор: " + reader.GetString(1));
                        Console.WriteLine("Текст: " + reader.GetString(2));
                        Console.WriteLine("Дата: " + reader.GetDateTime(3));
                        Console.WriteLine(new string('-', 50));
                    }

                    if (!hasRows)
                        Console.WriteLine("Заметок пока нет.");
                }
            }
        }

        public async Task DeleteNoteAsync(
            NpgsqlConnection connection,
            UserSession user,
            int noteId)
        {
            bool isAdmin = RoleGuard.IsAdmin(user);

            string sql = isAdmin
                ? @"DELETE FROM notes
WHERE id = @note_id;"
                : @"DELETE FROM notes
WHERE id = @note_id AND user_id = @user_id;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@note_id", noteId);

                if (!isAdmin)
                    command.Parameters.AddWithValue("@user_id", user.Id);

                int affectedRows = await command.ExecuteNonQueryAsync();

                if (affectedRows == 0)
                {
                    Console.WriteLine("Заметка не найдена или нет прав на удаление.");
                    return;
                }
            }

            await _logService.WriteAsync(
                connection,
                user.Id,
                "NOTE_DELETED",
                "Пользователь удалил заметку с ID " + noteId + ".");

            Console.WriteLine("Заметка удалена.");
        }
    }
}