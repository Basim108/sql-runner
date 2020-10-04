using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NLog;

namespace Hrimsoft.SqlRunner
{
    /// <summary>
    /// Manages db connections
    /// </summary>
    public class PostgreSqlConnectionPool : IDisposable, IAsyncDisposable, ISqlConnectionPool
    {
        private readonly ILogger _logger = LogManager.GetLogger(nameof(PostgreSqlConnectionPool));

        public IDictionary<string, ExecutionDataContext> Pool { get; }

        public PostgreSqlConnectionPool()
        {
            Pool    = new Dictionary<string, ExecutionDataContext>();
        }

        public async Task<ExecutionDataContext> GetContextAsync(DatabaseConfiguration dbConfig, CancellationToken cancellation)
        {
            _logger.Trace($"Getting a connection for database: '{dbConfig.Name}'");
            if (dbConfig == null)
                throw new ArgumentNullException(nameof(dbConfig));
            if (Pool.ContainsKey(dbConfig.Name))
                return Pool[dbConfig.Name];
            var build = new NpgsqlConnectionStringBuilder(dbConfig.ConnectionString)
            {
                Password            = dbConfig.Password,
                IncludeErrorDetails = true
            };
            var connection = new NpgsqlConnection(build.ConnectionString);
            connection.Open();
            var transaction = await connection.BeginTransactionAsync(cancellation);
            var context     = new ExecutionDataContext(connection, transaction);
            Pool.Add(dbConfig.Name, context);
            return context;
        }

        public void Dispose()
        {
            _logger.Trace("disposing a connection pool");
            foreach (var (database, context) in Pool) {
                if (context.State == ExecutionState.Executing) {
                    _logger.Debug($"rolling back a transaction for '{database}'");
                    context.Transaction.Rollback();
                }
                _logger.Debug($"disposing connection to '{database}'");
                context.Connection.Dispose();
            }
        }

        public ValueTask DisposeAsync()
        {
            _logger.Trace("disposing a connection pool asynchronously");
            return new ValueTask(Task.Run(() => {
                var rollbackTasks = Pool
                                   .Where(x => x.Value.State == ExecutionState.Executing)
                                   .Select(x => Task.Run(() => {
                                        _logger.Trace($"rolling back a transaction for {x.Key}");
                                        try {
                                            x.Value.Transaction.Rollback();
                                        }
                                        catch (Exception ex) {
                                            _logger.Error($"exception while rolling back a transaction for {x.Key}\n{ex}");
                                            throw;
                                        }
                                    }))
                                   .ToArray();
                _logger.Trace("waiting for rolling back transactions that have not been completed yet");
                Task.WaitAll(rollbackTasks);

                var connections = Pool
                                 .Select(x => Task.Run(() => {
                                      _logger.Trace($"disposing a connection to {x.Key}");
                                      try {
                                          x.Value.Connection.Dispose();
                                      }
                                      catch (Exception ex) {
                                          _logger.Error($"exception while disposing a connection to {x.Key}\n{ex}");
                                          throw;
                                      }
                                  }))
                                 .ToArray();
                _logger.Trace("waiting for disposing connections");
                Task.WaitAll(connections);
            }));
        }
    }
}