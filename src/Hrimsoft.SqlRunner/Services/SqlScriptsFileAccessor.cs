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
    public class SqlScriptsFileAccessor : ISqlScriptsAccessor
    {
        private readonly string _upFolder;
        private readonly string _downFolder;

        public SqlScriptsFileAccessor(AppConfiguration appConfig)
        {
            if(appConfig == null) 
                throw new ArgumentNullException(nameof(appConfig));
            if (!Directory.Exists(appConfig.ConsoleArgs.Path))
                throw new ConfigurationException($"Console argument path is not correct. path '{appConfig.ConsoleArgs.Path}' does not exist.");
            _upFolder   = Path.Combine(new[] {appConfig.ConsoleArgs.Path, appConfig.UpFolderName});
            _downFolder = Path.Combine(new[] {appConfig.ConsoleArgs.Path, appConfig.DownFolderName});
        }

        /// <summary>
        /// Creates an ordered sql scripts from all projects in up folder. 
        /// </summary>
        /// <returns>Returns an order where the 1st item is the 1st to be run</returns>
        public ICollection<ScriptInfo> GetUpScripts()
        {
            var projects = new List<string> {_upFolder};
            projects.AddRange(Directory.GetDirectories(_upFolder));
            return ParseProjects(projects, true);
        }

        /// <summary>
        /// Creates an ordered sql scripts from all projects in down folder. 
        /// </summary>
        /// <returns>Returns an order where the 1st item is the 1st to be run</returns>
        public ICollection<ScriptInfo> GetDownScripts()
        {
            var projects = new List<string> {_downFolder};
            projects.AddRange(Directory.GetDirectories(_downFolder));
            return ParseProjects(projects, false);
        }

        private ICollection<ScriptInfo> ParseProjects(ICollection<string> projects, bool linkToDownScript)
        {
            var ordered = new Dictionary<int, string>(projects.Count);
            var other   = new List<string>(projects.Count);
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
                scripts.AddRange(ParseScripts(ordered[projectIndex], linkToDownScript));
            }
            foreach (var projectFolder in other) {
                scripts.AddRange(ParseScripts(projectFolder, linkToDownScript));
            }
            return scripts;
        }

        private IEnumerable<ScriptInfo> ParseScripts(string projectFolder, bool linkToDownScript)
        {
            var downFolder = linkToDownScript ? _downFolder : "";
            var result = Directory.GetFiles(projectFolder, "*.sql")
                                  .Select(fileName => new ScriptInfo(fileName, _upFolder, downFolder))
                                  .ToList();
            return result;
        }
    }
}