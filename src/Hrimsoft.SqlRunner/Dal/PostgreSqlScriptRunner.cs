using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.Core.Exceptions;
using Hrimsoft.SqlRunner.Models;
using NLog;
using Npgsql;

namespace Hrimsoft.SqlRunner
{
    public class PostgreSqlScriptRunner : ISqlScriptRunner
    {
        private readonly ILogger             _logger = LogManager.GetLogger(nameof(PostgreSqlScriptRunner));
        private readonly ISqlConnectionPool  _connectionPool;
        private readonly DatabaseEnvironment _environment;

        public PostgreSqlScriptRunner(
            DatabaseEnvironment environment,
            ISqlConnectionPool connectionPool)
        {
            _connectionPool = connectionPool;
            _environment    = environment;
        }

        /// <summary> Execute sql script </summary>
        public async Task ExecuteAsync(ScriptInfo scriptInfo, CancellationToken cancellation)
        {
            if (scriptInfo == null)
                throw new ArgumentNullException(nameof(scriptInfo));
            var dbConfig   = GetDatabaseConfig(scriptInfo);
            var context = await _connectionPool.GetContextAsync(dbConfig, cancellation);
            if (context == null)
                throw new ApplicationException($"Db connection pool returned null context for database {dbConfig.Name}");
            await this.ExecuteAsync(scriptInfo.ExecScriptPath, context, cancellation);
        }
        
        public async Task ExecuteAsync(string scriptPath, ExecutionDataContext context, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
                throw new ArgumentNullException(nameof(scriptPath));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            
            _logger.Trace($"executing script: {scriptPath}");
            var npgsqlConnection = context.Connection as NpgsqlConnection;
            if (npgsqlConnection == null)
                throw new ApplicationException($"Db connection must be npgsql but it is {context.Connection.GetType().FullName}");
            if (npgsqlConnection.State != ConnectionState.Open)
                npgsqlConnection.Open();
            var npgsqlTransaction = context.Transaction as NpgsqlTransaction;
            if (npgsqlTransaction == null)
                throw new ApplicationException($"Db transaction must be npgsql but it is {context.Transaction.GetType().FullName}");

            cancellation.ThrowIfCancellationRequested();
            _logger.Trace($"reading script: {scriptPath}");
            var sqlCommand = await File.ReadAllTextAsync(scriptPath, cancellation);
            
            await using var command = new NpgsqlCommand(sqlCommand, npgsqlConnection, npgsqlTransaction);
            cancellation.ThrowIfCancellationRequested();
            _logger.Trace($"executing script: {scriptPath}");
            await command.ExecuteNonQueryAsync(cancellation);
            _logger.Trace($"executed script: {scriptPath}");
        }

        private DatabaseConfiguration GetDatabaseConfig(ScriptInfo scriptInfo)
        {
            var result = _environment.Databases.FirstOrDefault(x => x.Name == scriptInfo.Database);
            if (result == null)
                throw new ConfigurationException($"There is no database configuration for {scriptInfo.Database}.File: {scriptInfo.ExecScriptPath}");
            return result;
        }
    }
}