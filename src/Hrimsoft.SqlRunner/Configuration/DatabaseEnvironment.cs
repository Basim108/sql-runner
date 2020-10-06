using System.Collections.Generic;

namespace Hrimsoft.SqlRunner
{
    public class DatabaseEnvironment
    {
        public string                             Environment { get; set; }
        public ICollection<DatabaseConfiguration> Databases   { get; set; }
    }
}