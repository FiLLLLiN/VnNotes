using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VnNotes
{
    /// <summary>
    /// Хранит информацию о последней версии приложения из GitHub.
    /// </summary>
    public sealed class UpdateInfo
    {
        public string Version { get; set; }
        public string DownloadUrl { get; set; }

        public UpdateInfo()
        {
            Version = "";
            DownloadUrl = "";
        }
    }
}