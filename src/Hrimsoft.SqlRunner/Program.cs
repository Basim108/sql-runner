using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Hrimsoft.SqlRunner
{
    class Program
    {
        private static CancellationTokenSource _stoppingCts;

        static int Main(string[] args)
        {
            var logger = LogManager.LoadConfiguration("nlog.config")
                                   .GetLogger(nameof(Main));
            try {
                _stoppingCts = new CancellationTokenSource();

                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

                var task = Parser.Default
                      .ParseArguments<ConsoleArguments>(args)
                      .WithParsedAsync(opt => RunWithOptionsAsync(opt, logger));
                task.Wait();
            }
            catch (Exception ex) {
                logger.Error(ex);
                return -1;
            }
            finally {
                LogManager.Shutdown();
            }
            return 0;
        }

        private static async Task RunWithOptionsAsync(ConsoleArguments consoleArguments, ILogger logger)
        {
            try {
                consoleArguments.CalculateDefaults();
                await using var connectionPool = new PostgreSqlConnectionPool();

                var appConfig        = new AppConfiguration(consoleArguments);
                var scriptsAccessor  = new SqlScriptsFileAccessor(appConfig);
                var scriptRunner     = new PostgreSqlScriptRunner(appConfig.CurrentEnvironment, connectionPool);
                var executionManager = new ExecutionManager(scriptsAccessor, connectionPool, scriptRunner);

                var success = false;
                if (consoleArguments.Up)
                    success = await executionManager.RunUpScriptsAsync(_stoppingCts.Token);
                else
                    success = await executionManager.RunDownScriptsAsync(_stoppingCts.Token);
                if (!success)
                    throw new ApplicationException("Script running failed");
            }
            catch (Exception ex) {
                logger.Error(ex);
            }
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            _stoppingCts.Cancel();
        }
    }
}