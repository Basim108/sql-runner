using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Hrimsoft.SqlRunner
{
    /// <summary>
    /// Manages db connections
    /// </summary>
    public class DbConnectionPool : IDisposable
    {
        private readonly Dictionary<string, (NpgsqlConnection Connection, NpgsqlTransaction Transaction)> _connectionPool;

        public DbConnectionPool()
        {
            _connectionPool = new Dictionary<string, (NpgsqlConnection, NpgsqlTransaction)>();
        }

        public async Task<NpgsqlConnection> GetConnectionAsync(DatabaseConfiguration dbConfig, CancellationToken cancellation)
        {
            if (dbConfig == null)
                throw new ArgumentNullException(nameof(dbConfig));
            if (_connectionPool.ContainsKey(dbConfig.Name))
                return _connectionPool[dbConfig.Name].Connection;
            var build = new NpgsqlConnectionStringBuilder(dbConfig.ConnectionString)
            {
                Password            = dbConfig.Password,
                IncludeErrorDetails = true
            };
            var connection  = new NpgsqlConnection(build.ConnectionString);
            connection.Open();
            var transaction = await connection.BeginTransactionAsync(cancellation);
            _connectionPool.Add(dbConfig.Name, (connection, transaction));
            return connection;
        }

        public Task CommitAsync(CancellationToken cancellation)
        {
            foreach (var (database, (connection, transaction)) in _connectionPool) {
                var task = transaction.CommitAsync(cancellation);
                task.Wait(cancellation);
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var (database, (connection, transaction)) in _connectionPool) {
                if (!transaction.IsCompleted)
                    transaction.Rollback();
                connection.Dispose();
            }
        }
    }
}