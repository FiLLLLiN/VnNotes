using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace VnNotes
{
    /// <summary>
    /// Отвечает за получение и сохранение статистики CPU, RAM и HDD.
    /// </summary>
    public sealed class SystemMetricService
    {
        private readonly SecurityLogService _logService;

        public SystemMetricService(SecurityLogService logService)
        {
            _logService = logService;
        }

        public async Task<SystemMetric> ReadCurrentMetricAsync()
        {
            decimal cpu = await GetCpuPercentAsync();
            decimal ram = GetRamPercent();
            decimal hdd = GetHddPercent();

            return new SystemMetric(
                Environment.MachineName,
                Round(cpu),
                Round(ram),
                Round(hdd),
                DateTime.Now);
        }

        public async Task SaveCurrentMetricAsync(
            NpgsqlConnection connection,
            UserSession user)
        {
            SystemMetric metric = await ReadCurrentMetricAsync();

            int nodeId = await GetOrCreateNodeAsync(
                connection,
                metric.NodeName,
                "Автоматически зарегистрированный узел");

            const string sql =
@"INSERT INTO system_metrics(node_id, user_id, cpu_percent, ram_percent, hdd_percent)
VALUES (@node_id, @user_id, @cpu_percent, @ram_percent, @hdd_percent);";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@node_id", nodeId);
                command.Parameters.AddWithValue("@user_id", user.Id);
                command.Parameters.AddWithValue("@cpu_percent", metric.CpuPercent);
                command.Parameters.AddWithValue("@ram_percent", metric.RamPercent);
                command.Parameters.AddWithValue("@hdd_percent", metric.HddPercent);

                await command.ExecuteNonQueryAsync();
            }

            await _logService.WriteAsync(
                connection,
                user.Id,
                "METRICS_SAVED",
                "Сохранена статистика узла " + metric.NodeName + ".");
        }

        public void PrintMetric(SystemMetric metric)
        {
            Console.WriteLine("Текущая статистика устройства:");
            Console.WriteLine("Узел: " + metric.NodeName);
            Console.WriteLine("CPU: " + metric.CpuPercent.ToString(CultureInfo.InvariantCulture) + "%");
            Console.WriteLine("RAM: " + metric.RamPercent.ToString(CultureInfo.InvariantCulture) + "%");
            Console.WriteLine("HDD: " + metric.HddPercent.ToString(CultureInfo.InvariantCulture) + "%");
            Console.WriteLine("Дата: " + metric.CapturedAt);
        }

        public async Task RegisterNodeAsync(
            NpgsqlConnection connection,
            UserSession user,
            string nodeName,
            string ipAddress,
            string description)
        {
            const string sql =
@"INSERT INTO infrastructure_nodes(node_name, ip_address, description)
VALUES (@node_name, @ip_address, @description)
ON CONFLICT(node_name) DO UPDATE
SET ip_address = EXCLUDED.ip_address,
    description = EXCLUDED.description,
    is_active = TRUE;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@node_name", nodeName);
                command.Parameters.AddWithValue("@ip_address", string.IsNullOrWhiteSpace(ipAddress) ? (object)DBNull.Value : ipAddress);
                command.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description);

                await command.ExecuteNonQueryAsync();
            }

            await _logService.WriteAsync(
                connection,
                user.Id,
                "NODE_REGISTERED",
                "Зарегистрирован узел " + nodeName + ".");
        }

        public async Task PrintLatestMetricsAsync(NpgsqlConnection connection)
        {
            const string sql =
@"SELECT DISTINCT ON (n.id)
    n.node_name,
    n.ip_address,
    m.cpu_percent,
    m.ram_percent,
    m.hdd_percent,
    m.captured_at
FROM infrastructure_nodes n
LEFT JOIN system_metrics m ON m.node_id = n.id
ORDER BY n.id, m.captured_at DESC;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
            {
                Console.WriteLine("Последняя статистика узлов:");
                Console.WriteLine();

                bool hasRows = false;

                while (await reader.ReadAsync())
                {
                    hasRows = true;

                    Console.WriteLine("Узел: " + reader.GetString(0));
                    Console.WriteLine("IP: " + (reader.IsDBNull(1) ? "не указан" : reader.GetString(1)));

                    if (reader.IsDBNull(2))
                    {
                        Console.WriteLine("Статистика отсутствует.");
                    }
                    else
                    {
                        Console.WriteLine("CPU: " + reader.GetDecimal(2) + "%");
                        Console.WriteLine("RAM: " + reader.GetDecimal(3) + "%");
                        Console.WriteLine("HDD: " + reader.GetDecimal(4) + "%");
                        Console.WriteLine("Дата: " + reader.GetDateTime(5));
                    }

                    Console.WriteLine(new string('-', 50));
                }

                if (!hasRows)
                    Console.WriteLine("Узлы не зарегистрированы.");
            }
        }

        private static async Task<int> GetOrCreateNodeAsync(
            NpgsqlConnection connection,
            string nodeName,
            string description)
        {
            const string sql =
@"INSERT INTO infrastructure_nodes(node_name, description)
VALUES (@node_name, @description)
ON CONFLICT(node_name) DO UPDATE
SET node_name = EXCLUDED.node_name
RETURNING id;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@node_name", nodeName);
                command.Parameters.AddWithValue("@description", description);

                object result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        private static async Task<decimal> GetCpuPercentAsync()
        {
            try
            {
                using (PerformanceCounter counter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    counter.NextValue();
                    await Task.Delay(800);
                    return Clamp(Convert.ToDecimal(counter.NextValue()));
                }
            }
            catch
            {
                return 0;
            }
        }

        private static decimal GetRamPercent()
        {
            try
            {
                using (PerformanceCounter counter = new PerformanceCounter("Memory", "% Committed Bytes In Use"))
                {
                    return Clamp(Convert.ToDecimal(counter.NextValue()));
                }
            }
            catch
            {
                return 0;
            }
        }

        private static decimal GetHddPercent()
        {
            try
            {
                string root = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory);

                DriveInfo drive = DriveInfo
                    .GetDrives()
                    .Where(x => x.IsReady)
                    .FirstOrDefault(x => root != null && root.StartsWith(x.Name, StringComparison.OrdinalIgnoreCase));

                if (drive == null)
                    drive = DriveInfo.GetDrives().FirstOrDefault(x => x.IsReady);

                if (drive == null || drive.TotalSize == 0)
                    return 0;

                long used = drive.TotalSize - drive.AvailableFreeSpace;
                return Clamp((decimal)used * 100m / drive.TotalSize);
            }
            catch
            {
                return 0;
            }
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2);
        }

        private static decimal Clamp(decimal value)
        {
            if (value < 0)
                return 0;

            if (value > 100)
                return 100;

            return value;
        }
    }
}