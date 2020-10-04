using System;
using System.IO;

namespace Hrimsoft.SqlRunner.Models
{
    /// <summary>
    /// Class describes a script that has to be run
    /// </summary>
    public class ScriptInfo
    {
        public ScriptInfo(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            ScriptPath = fileName;
            
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

        /// <summary> File path to the script that has to be run </summary>
        public string ScriptPath { get; private set; }

        /// <summary> Name of database where script must be executed </summary>
        public string Database { get; private set; }

        /// <summary> The number in queue for execution </summary>
        public int OrderId { get; private set; }
    }
}