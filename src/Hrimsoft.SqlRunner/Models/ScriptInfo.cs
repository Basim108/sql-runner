using System;
using System.IO;

namespace Hrimsoft.SqlRunner.Models
{
    /// <summary>
    /// Class describes a script that has to be run
    /// </summary>
    public class ScriptInfo
    {
        public ScriptInfo(string fileName, string upFolderName, string downFolderName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            ExecScriptPath   = fileName;
            DownScriptPath = string.Empty;
            if (!string.IsNullOrWhiteSpace(downFolderName)) {
                var relativeScriptPath = Path.GetRelativePath(fileName, upFolderName);
                DownScriptPath = Path.Combine(downFolderName, relativeScriptPath);
                if (!File.Exists(DownScriptPath))
                    DownScriptPath = string.Empty;
            }
            var parts = Path.GetFileName(fileName)
                            .Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1) {
                this.OrderId = int.TryParse(parts[0], out var order)
                    ? order
                    : -1;
                this.Database = OrderId == -1
                    ? parts[0]
                    : parts[1];
            }
        }

        /// <summary>
        /// File path to the script that has to be run.
        /// it might be from up folder but it also might be from down folder if --down console argument is set
        /// </summary>
        public string ExecScriptPath { get; private set; }
        
        /// <summary> File path to the down script that has to be run to rollback <see cref="ExecScriptPath"/> changes </summary>
        public string DownScriptPath { get; private set; }

        /// <summary> Name of database where script must be executed </summary>
        public string Database { get; private set; }

        /// <summary> The number in queue for execution </summary>
        public int OrderId { get; private set; }
    }
}