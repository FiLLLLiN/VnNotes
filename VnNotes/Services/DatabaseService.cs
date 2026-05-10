using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VnNotes
{
    /// <summary>
    /// Отвечает за создание таблиц PostgreSQL.
    /// </summary>
    public sealed class DatabaseService
    {
        public async Task InitializeAsync(NpgsqlConnection connection)
        {
            string sqlPath = GetInitScriptPath();

            if (!File.Exists(sqlPath))
                throw new FileNotFoundException("Не найден SQL-файл инициализации: " + sqlPath);

            string sql = File.ReadAllText(sqlPath);

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        private static string GetInitScriptPath()
        {
            string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database", "init.sql");

            if (File.Exists(outputPath))
                return outputPath;

            return Path.Combine(Directory.GetCurrentDirectory(), "database", "init.sql");
        }
    }
}