using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Hrimsoft.SqlRunner
{
    /// <summary>
    /// For each database there is a separate db connection with one transaction for all scripts that will be run on this database
    /// </summary>
    public interface ISqlConnectionPool
    {
        /// <summary>
        /// Dictionary of connections where
        /// Key is a database name,
        /// Value is a info about connection transaction and execution context  
        /// </summary>
        IDictionary<string, ExecutionDataContext> Pool { get; }

        /// <summary>
        /// Gets a db connection by database name
        /// </summary>
        Task<ExecutionDataContext> GetContextAsync(DatabaseConfiguration dbConfig, CancellationToken cancellation);
    }
}