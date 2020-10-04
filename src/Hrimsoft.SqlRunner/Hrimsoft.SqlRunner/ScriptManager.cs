using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hrimsoft.Core.Exceptions;
using Hrimsoft.SqlRunner.Models;

namespace Hrimsoft.SqlRunner
{
    /// <summary>
    /// Create an ordered list of scripts that have to be run
    /// </summary>
    public class ScriptManager
    {
        private readonly AppConfiguration _appConfig;

        public ScriptManager(AppConfiguration appConfig)
        {
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        }

        /// <summary>
        /// Creates an ordered sql scripts from all projects. 
        /// </summary>
        /// <returns>Returns an order where the 1st item is the 1st to be run</returns>
        public ICollection<ScriptInfo> GetScripts()
        {
            var sqlScriptsPath = Path.GetFullPath(_appConfig.ConsoleArgs.Path);
            if (!Directory.Exists(sqlScriptsPath))
                throw new ConfigurationException($"Console argument path is not correct. path does not exist.");
            var projects = Directory.GetDirectories(sqlScriptsPath);
            return ParseProjects(projects);
        }

        private ICollection<ScriptInfo> ParseProjects(string[] projects)
        {
            var ordered = new Dictionary<int, string>(projects.Length);
            var other   = new List<string>(projects.Length);
            foreach (var folder in projects) {
                var parts = folder.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1) {
                    if (int.TryParse(parts[0], out var order)) {
                        ordered.Add(order, folder);
                        continue;
                    }
                }
                other.Add(folder);
            }
            var scripts       = new List<ScriptInfo>();
            var sortedIndexes = ordered.Keys.OrderBy(x => x);
            foreach (var projectIndex in sortedIndexes) {
                scripts.AddRange(ParseScripts(ordered[projectIndex]));
            }
            foreach (var projectFolder in other) {
                scripts.AddRange(ParseScripts(projectFolder));
            }
            return scripts;
        }

        private IEnumerable<ScriptInfo> ParseScripts(string projectFolder)
        {
            var result = Directory.GetFiles(projectFolder, "*.sql")
                                  .Select(fileName => new ScriptInfo(fileName))
                                  .ToList();
            return result;
        }
    }
}