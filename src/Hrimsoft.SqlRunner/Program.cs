using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Hrimsoft.SqlRunner
{
    class Program
    {
        private static CancellationTokenSource _stoppingCts;

        static void Main(string[] args)
        {
            try {
                _stoppingCts = new CancellationTokenSource();

                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

                Parser.Default
                      .ParseArguments<ConsoleArguments>(args)
                      .WithParsedAsync(RunWithOptionsAsync);
            }
            catch (Exception ex) {
                ProcessError(ex);
            }
        }

        private static Task RunWithOptionsAsync(ConsoleArguments consoleArguments)
        {
            try {
                var appConfig      = new AppConfiguration(consoleArguments);
                var scriptManager  = new ScriptManager(appConfig);
                var scriptInfoList = scriptManager.GetScripts();

                using var scriptRunner = new SqlScriptRunner(appConfig.CurrentEnvironment);
                foreach (var script in scriptInfoList) {
                    Console.WriteLine($"{DateTime.Now:o}: Running {Path.GetFileName(script.ScriptPath)}");
                    var task = scriptRunner.ExecuteAsync(script, _stoppingCts.Token);
                    task.Wait();
                    Console.WriteLine($"{DateTime.Now:o}: Finished {Path.GetFileName(script.ScriptPath)}");
                    if (task.IsFaulted)
                        throw task.Exception.InnerException;
                }
                scriptRunner.CommitAsync(_stoppingCts.Token).Wait(_stoppingCts.Token);
            }
            catch (Exception ex) {
                ProcessError(ex);
            }
            return Task.CompletedTask;
        }

        private static void ProcessError(Exception ex)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} {ex}");
            Console.ForegroundColor = defaultColor;
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            _stoppingCts.Cancel();
        }
    }
}