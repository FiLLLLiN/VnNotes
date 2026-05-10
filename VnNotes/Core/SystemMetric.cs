using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VnNotes
{
    /// <summary>
    /// Хранит статистику нагрузки устройства.
    /// </summary>
    public sealed class SystemMetric
    {
        public string NodeName { get; private set; }
        public decimal CpuPercent { get; private set; }
        public decimal RamPercent { get; private set; }
        public decimal HddPercent { get; private set; }
        public DateTime CapturedAt { get; private set; }

        public SystemMetric(
            string nodeName,
            decimal cpuPercent,
            decimal ramPercent,
            decimal hddPercent,
            DateTime capturedAt)
        {
            NodeName = nodeName;
            CpuPercent = cpuPercent;
            RamPercent = ramPercent;
            HddPercent = hddPercent;
            CapturedAt = capturedAt;
        }
    }
}
