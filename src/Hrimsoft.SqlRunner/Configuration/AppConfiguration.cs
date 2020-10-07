using System;
using System.Linq;
using Hrimsoft.Core.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Hrimsoft.SqlRunner
{
    public class AppConfiguration
    {
        public readonly  ConsoleArguments ConsoleArgs;
        private readonly IConfiguration   _appSettings;

        public AppConfiguration(ConsoleArguments consoleArgs)
        {
            ConsoleArgs = consoleArgs ?? throw new ArgumentNullException(nameof(consoleArgs));
            var settingsPath = string.IsNullOrWhiteSpace(ConsoleArgs.SettingsPath)
                ? "appsettings.json"
                : ConsoleArgs.SettingsPath;
            _appSettings = new ConfigurationBuilder()
                          .AddJsonFile(settingsPath)
                          .AddUserSecrets<Program>()
                          .Build();
        }

        private DatabaseEnvironment _currentEnvironment;
        public DatabaseEnvironment CurrentEnvironment {
            get {
                if (_currentEnvironment != null)
                    return _currentEnvironment;
                var sectionName  = "Environments";
                var section      = _appSettings.GetSection(sectionName);
                var environments = section.Get<DatabaseEnvironment[]>();
                if (environments == null)
                    throw new ConfigurationException($"Configuration is wrong. Section name is '{sectionName}'");
                _currentEnvironment = environments.FirstOrDefault(
                                          x => x.Environment == ConsoleArgs.Environment)
                                      ?? throw new ConfigurationException($"There is no configuration for environment: '{ConsoleArgs.Environment}'");
                return _currentEnvironment;
            }
        }

        private string _upFolderName;
        public string UpFolderName {
            get {
                if (string.IsNullOrWhiteSpace(_upFolderName)) {
                    _upFolderName = _appSettings[nameof(UpFolderName)];
                    if (string.IsNullOrWhiteSpace(_upFolderName))
                        _upFolderName = "Up";
                }
                return _upFolderName;
            }
        }
        
        private string _downFolderName;
        public string DownFolderName {
            get {
                if (string.IsNullOrWhiteSpace(_downFolderName)) {
                    _downFolderName = _appSettings[nameof(DownFolderName)];
                    if (string.IsNullOrWhiteSpace(_downFolderName))
                        _downFolderName = "Down";
                }
                return _downFolderName;
            }
        }
    }
}