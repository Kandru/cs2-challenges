using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using Challenges.Utils;

namespace Challenges
{
    public partial class Challenges
    {
        [ConsoleCommand("challenges", "Challenges admin commands: reload | enable | disable | check-catalog | test-discord | perf [reset]")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<reload|enable|disable|check-catalog|test-discord|perf [reset]>")]
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
                        if (Modules.Count == 0)
                        {
                            InitializeModules(isHotReloaded: true);
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
                case "test-discord":
                    if (Modules.GetValueOrDefault(nameof(Notifications)) is not Notifications notes)
                    {
                        command.ReplyToCommand(Localizer["admin.discord.test.no_plugin"]);
                        break;
                    }

                    command.ReplyToCommand(notes.SendDiscordTestSamples() ?? Localizer["admin.discord.test.ok"].Value);
                    break;
                case "perf":
                    CommandPerf(command);
                    break;
                default:
                    command.ReplyToCommand(Localizer["admin.unknown_command"].Value
                        .Replace("{command}", subCommand));
                    break;
            }
        }

        /// <summary><c>challenges perf</c>: top handlers by total time plus queue gauges; <c>perf reset</c> zeroes them.</summary>
        private void CommandPerf(CommandInfo command)
        {
            if (command.GetArg(2).Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                PerfCounters.Global.Reset();
                command.ReplyToCommand(Localizer["admin.perf.reset"]);
                return;
            }

            if (!PerfCounters.Enabled)
            {
                command.ReplyToCommand(Localizer["admin.perf.disabled"]);
                return;
            }

            PerfGauges? gauges = null;
            if (_engine is { } engine && _saves is { } saves)
            {
                ProgressPipeline pipeline = engine.Pipeline;
                gauges = new PerfGauges(
                    pipeline.QueuedWork,
                    pipeline.QueuedPatches,
                    pipeline.PendingSteamIds,
                    pipeline.ShadowCount,
                    saves.Queued,
                    saves.InFlight);
            }

            foreach (string line in PerfCounters.Global.Report(PerfCounters.ReportRows, gauges))
            {
                command.ReplyToCommand(line);
            }
        }

        private void ReloadAll()
        {
            Config.Reload();
            DestroyModules();
            _state.Config = Config;
            LogCatalogMismatches();
            LoadSchedulesAndChallenges();
            InitializeModules(isHotReloaded: true);
        }

        private static string FormatReloadError(Exception ex)
        {
            Exception root = ex.GetBaseException();
            return root is TargetException
                ? "Challenges.json is empty or a nested section is null; "
                    + "gui, notifications, discord, persistence, and temp_data must be objects."
                : root.Message;
        }
    }
}
