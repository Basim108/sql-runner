using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hrimsoft.Core.Extensions;
using Hrimsoft.SqlRunner.Models;
using NLog;

namespace Hrimsoft.SqlRunner
{
    /// <summary>
    /// Manages sql scripts execution process
    /// </summary>
    public class ExecutionManager
    {
        private readonly ILogger             _logger = LogManager.GetLogger(nameof(ExecutionManager));
        private readonly ISqlScriptsAccessor _scriptsAccessor;
        private readonly ISqlConnectionPool  _connectionPool;
        private readonly ISqlScriptRunner    _runner;

        private readonly IDictionary<string, ICollection<ScriptInfo>> _executedScripts;

        public ExecutionManager(
            ISqlScriptsAccessor scriptsAccessor,
            ISqlConnectionPool connectionPool,
            ISqlScriptRunner runner)
        {
            _scriptsAccessor = scriptsAccessor ?? throw new ArgumentNullException(nameof(scriptsAccessor));
            _connectionPool  = connectionPool;
            _runner          = runner ?? throw new ArgumentNullException(nameof(runner));
            _executedScripts = new Dictionary<string, ICollection<ScriptInfo>>();
        }

        /// <summary>
        /// Executes up sql-scripts, and roll them back if errors occurred by running down sql scripts 
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns>Returns True if all up scripts executed successfully, otherwise returns False</returns>
        public async Task<bool> RunUpScriptsAsync(CancellationToken cancellation)
        {
            _logger.Trace("running up scripts");
            var scriptInfoList = _scriptsAccessor.GetUpScripts();
            if (scriptInfoList.IsNullOrEmpty()) {
                _logger.Debug("There is no scripts to execute.");
                return true;
            }
            _executedScripts.Clear();
            try {
                foreach (var script in scriptInfoList) {
                    _logger.Debug($"Running {Path.GetFileName(script.ExecScriptPath)}");
                    await _runner.ExecuteAsync(script, cancellation);
                    if (!_executedScripts.ContainsKey(script.Database)) {
                        _executedScripts.Add(script.Database, new List<ScriptInfo>());
                    }
                    _executedScripts[script.Database].Add(script);
                    _logger.Debug($"Finished {Path.GetFileName(script.ExecScriptPath)}");
                }
                Commit(cancellation);
            }
            catch (Exception ex) {
                _logger.Error(ex.ToString());
                await RollbackAfterFailureAsync(cancellation);
                return false;
            }
            return true;
        }

        public async Task<bool> RunDownScriptsAsync(CancellationToken cancellation)
        {
            _logger.Trace("running down scripts");
            var scriptInfoList = _scriptsAccessor.GetDownScripts();
            if (scriptInfoList.IsNullOrEmpty()) {
                _logger.Debug("There is no scripts to execute.");
                return true;
            }
            _executedScripts.Clear();
            try {
                foreach (var script in scriptInfoList) {
                    _logger.Debug($"Running {Path.GetFileName(script.ExecScriptPath)}");
                    await _runner.ExecuteAsync(script, cancellation);
                    if (!_executedScripts.ContainsKey(script.Database)) {
                        _executedScripts.Add(script.Database, new List<ScriptInfo>());
                    }
                    _executedScripts[script.Database].Add(script);
                    _logger.Debug($"Finished {Path.GetFileName(script.ExecScriptPath)}");
                }
                Commit(cancellation);
            }
            catch (Exception ex) {
                _logger.Error(ex.ToString());
                return false;
            }
            return true;
        }
        
        private void Commit(CancellationToken cancellation)
        {
            _logger.Debug("commiting");
            var commitTasks = _connectionPool.Pool
                                             .Where(x => x.Value.State == ExecutionState.Executing)
                                             .Select(x => Task.Run(() => {
                                                  _logger.Trace($"commiting a transaction for {x.Key}");
                                                  try {
                                                      x.Value.Transaction.Commit();
                                                      x.Value.State = ExecutionState.Completed;
                                                  }
                                                  catch (Exception ex) {
                                                      _logger.Error($"exception while commiting {x.Key}\n{ex}");
                                                      throw;
                                                  }
                                              }))
                                             .ToArray();
            _logger.Debug($"waiting for {commitTasks.Length} tasks");
            Task.WaitAll(commitTasks, cancellation);
        }

        private async Task RollbackAfterFailureAsync(CancellationToken cancellation)
        {
            _logger.Debug("rolling back after failure");
            foreach (var (database, context) in _connectionPool.Pool) {
                _logger.Debug($"examining database: '{database}'");
                switch (context.State) {
                    case ExecutionState.Executing:
                        _logger.Debug($"rolling back a transaction for {database}");
                        context.Transaction.Rollback();
                        break;
                    case ExecutionState.Completed:
                        _logger.Debug($"running down-scripts for {database} to roll back committed changes");
                        var scriptInfos  = _executedScripts[database];
                        var needToCommit = false;
                        foreach (var info in scriptInfos) {
                            if (string.IsNullOrWhiteSpace(info.DownScriptPath))
                                continue;
                            needToCommit = true;
                            await _runner.ExecuteAsync(info.DownScriptPath, context, cancellation);
                        }
                        if (needToCommit) {
                            _logger.Debug("commiting after down-scripts execution");
                            context.Transaction.Commit();
                        }
                        else {
                            _logger.Debug($"there is no down-script to run for database: '{database}'");
                        }
                        break;
                }
            }
            _logger.Debug("rolling back finished");
        }
    }
}