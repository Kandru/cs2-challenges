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
                    ReloadAll();
                    command.ReplyToCommand(Localizer["admin.reload"]);
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
            DestroyClasses();
            // Config.Reload updates the config instance in place; it does not call OnConfigParsed.
            Config.Reload();
            _globalStates[GlobalStates.GlobalConfig] = Config;
            LogCatalogMismatches();
            LoadChallengeFiles();
            LoadSchedules();
            InitializeClasses(isHotReloaded: true);
        }
    }
}
