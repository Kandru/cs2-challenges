using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using Challenges.Enums;

namespace Challenges
{
    public partial class Challenges
    {
        [ConsoleCommand("challenges", "Challenges admin commands: reload | enable | disable | check-catalog")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<reload|enable|disable|check-catalog>")]
        public void CommandAdmin(CCSPlayerController? player, CommandInfo command)
        {
            string subCommand = command.GetArg(1);
            switch (subCommand.ToLowerInvariant())
            {
                case "reload":
                    try
                    {
                        ReloadAll();
                        command.ReplyToCommand(Localizer["admin.reload"]);
                    }
                    catch (Exception ex)
                    {
                        if (ClassInstances.Count == 0)
                        {
                            InitializeClasses(isHotReloaded: true);
                        }

                        string message = Localizer["core.faultyconfig"].Value
                            .Replace("{config}", Config.GetConfigPath())
                            .Replace("{error}", FormatReloadError(ex));
                        Console.WriteLine(message);
                        command.ReplyToCommand(message);
                    }
                    break;
                case "disable":
                    Config.Enabled = false;
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.disable"]);
                    break;
                case "enable":
                    Config.Enabled = true;
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.enable"]);
                    break;
                case "check-catalog":
                    List<string> mismatches = CheckCatalog();
                    if (mismatches.Count == 0)
                    {
                        command.ReplyToCommand(Localizer["admin.catalog.ok"]);
                        break;
                    }

                    foreach (string message in mismatches)
                    {
                        Console.WriteLine(message);
                        command.ReplyToCommand(message);
                    }
                    break;
                default:
                    command.ReplyToCommand(Localizer["admin.unknown_command"].Value
                        .Replace("{command}", subCommand));
                    break;
            }
        }

        private void ReloadAll()
        {
            // Config.Reload updates in place (no OnConfigParsed). Reload before teardown
            // so a bad Challenges.json does not leave the plugin without classes.
            Config.Reload();
            DestroyClasses();
            _globalStates[GlobalStates.GlobalConfig] = Config;
            LogCatalogMismatches();
            LoadChallengeFiles();
            LoadSchedules();
            InitializeClasses(isHotReloaded: true);
        }

        private static string FormatReloadError(Exception ex)
        {
            Exception root = ex.GetBaseException();
            return root is TargetException
                ? "Challenges.json is empty or a nested section is null; "
                    + "gui, notifications, discord, and temp_data must be objects."
                : root.Message;
        }
    }
}
