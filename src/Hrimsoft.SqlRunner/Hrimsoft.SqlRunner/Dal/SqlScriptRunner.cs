using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.Core.Exceptions;
using Hrimsoft.SqlRunner.Models;
using Npgsql;

namespace Hrimsoft.SqlRunner
{
    public class SqlScriptRunner : ISqlScriptRunner, IDisposable
    {
        private readonly DbConnectionPool    _connectionPool;
        private readonly DatabaseEnvironment _environment;

        public SqlScriptRunner(DatabaseEnvironment environment)
        {
            _connectionPool = new DbConnectionPool();
            _environment    = environment;
        }

        /// <summary> Execute sql script </summary>
        public async Task ExecuteAsync(ScriptInfo scriptInfo, CancellationToken cancellation)
        {
            if (scriptInfo == null)
                throw new ArgumentNullException(nameof(scriptInfo));
            var dbConfig = GetDatabaseConfig(scriptInfo);

            var connection = await _connectionPool.GetConnectionAsync(dbConfig, cancellation);
            if (connection.State != ConnectionState.Open)
                connection.Open();
            cancellation.ThrowIfCancellationRequested();
            var sqlCommand = await File.ReadAllTextAsync(scriptInfo.ScriptPath, cancellation);

            await using var command = new NpgsqlCommand(sqlCommand, connection);
            cancellation.ThrowIfCancellationRequested();
            await command.ExecuteNonQueryAsync(cancellation);
        }

        private DatabaseConfiguration GetDatabaseConfig(ScriptInfo scriptInfo)
        {
            var result = _environment.Databases.FirstOrDefault(x => x.Name == scriptInfo.Database);
            if (result == null)
                throw new ConfigurationException($"There is no database configuration for {scriptInfo.Database}.File: {scriptInfo.ScriptPath}");
            return result;
        }

        public Task CommitAsync(CancellationToken cancellation)
        {
            return _connectionPool.CommitAsync(cancellation);
        }

        public void Dispose()
        {
            _connectionPool?.Dispose();
        }
    }
}