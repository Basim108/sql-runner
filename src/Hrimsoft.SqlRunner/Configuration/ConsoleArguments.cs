using CommandLine;

namespace Hrimsoft.SqlRunner
{
    public class ConsoleArguments
    {
        [Option('p', "path", Required = true, HelpText = "Set path to the folder with sql scripts")]
        public string Path { get; set; }

        [Option('e', "env", Required = false, HelpText = "Set environment name:\n\t -e development\n\t--env staging\n\t-e production")]
        public string Environment { get; set; }
        
        [Option('s', "settings", Required = false, HelpText = "Set path to the appsetings.json file where databases, users, passwords and environments are listed\nBy default appsettings.json file will be looked at the tool working folder.")]
        public string SettingsPath { get; set; }
    }
}