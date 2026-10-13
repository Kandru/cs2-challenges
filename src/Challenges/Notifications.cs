using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace Challenges
{
    /// <summary>Chat, sounds and Discord webhooks.</summary>
    public class Notifications : Module
    {
        private const int EmbedTitleLimit = 256;
        private const int EmbedDescriptionLimit = 4096;
        private const int EmbedFieldNameLimit = 256;
        private const int EmbedFieldValueLimit = 1024;
        private const int EmbedAuthorLimit = 256;
        private const int EmbedFooterLimit = 2048;

        /// <summary>Pending webhook posts; when full the oldest is dropped.</summary>
        private const int WebhookQueueCapacity = 100;
        /// <summary>Total tries per post when Discord answers 429.</summary>
        private const int WebhookMaxAttempts = 3;

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly string[] NoMentionParse = [];
        private static readonly TimeSpan WebhookJoinTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan WebhookDefaultRetry = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan WebhookMaxRetry = TimeSpan.FromSeconds(60);
        private static readonly TaskListMode[] TestTaskListModes =
        [
            TaskListMode.Full,
            TaskListMode.Short,
        ];

        private readonly List<(ChallengeDefinition Challenge, ChallengeTask Task)> _pendingCompletions = [];
        private readonly Dictionary<string, string> _completionByLanguage = new(StringComparer.Ordinal);
        private readonly HashSet<string> _mergedTitles = new(StringComparer.Ordinal);

        /// <summary>Single-reader queue drained by <see cref="_webhookSender"/>; both created on the first post.</summary>
        private Channel<WebhookPost>? _webhooks;
        private Task? _webhookSender;
        private readonly CancellationTokenSource _webhookStop = new();
        /// <summary>1 after the overflow was logged; reset once the queue drains.</summary>
        private int _webhookDropLogged;

        private readonly record struct WebhookPost(string Url, Dictionary<string, object?> Embed);

        public Notifications(State state, IStringLocalizer localizer, bool isHotReloaded)
            : base(state, localizer)
        {
            Schedules schedules = GetModule<Schedules>();
            if (schedules.NewScheduleAnnouncementPending && schedules.Current is { } running)
            {
                schedules.ClearAnnouncement();
                DiscordNewSchedule(running);
            }
        }

        public void NotifyProgress(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task, int amount)
        {
            if (!player.IsValid || !GlobalConfig.Notifications.NotifyPlayerOnChallengeProgress || !task.AnnounceProgress)
            {
                return;
            }

            PlaySound(player, GlobalConfig.Notifications.ChallengeProgressSound);
            string message = ChatMessage(player, "challenges.progress")
                .Replace("{challenge}", TaskTitle(player, challenge, task, amount))
                .Replace("{total}", task.Amount.ToString())
                .Replace("{count}", amount.ToString());
            player.PrintToChat(message);
        }

        /// <summary>
        /// Tells the player at once; completions for the other players are collected until
        /// <see cref="FlushCompletions"/> sends them as one message per patch.
        /// </summary>
        public void NotifyCompletion(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            if (!player.IsValid || !task.AnnounceCompletion)
            {
                return;
            }

            if (GlobalConfig.Notifications.NotifyOtherOnChallengeComplete)
            {
                _pendingCompletions.Add((challenge, task));
            }

            if (!GlobalConfig.Notifications.NotifyPlayerOnChallengeComplete)
            {
                return;
            }

            foreach (CCSPlayerController entry in Players.GetHumans())
            {
                if (!Players.Same(entry, player))
                {
                    continue;
                }

                PlayClientSound(player, GlobalConfig.Notifications.ChallengeCompleteSound);
                entry.PrintToChat(CompletionMessage(entry, "challenges.completed.user", player.PlayerName, challenge, task));
            }
        }

        /// <summary>
        /// Sends the completions collected for <paramref name="player"/> to every other human as one message,
        /// formatted once per recipient language.
        /// </summary>
        public void FlushCompletions(CCSPlayerController player)
        {
            if (_pendingCompletions.Count == 0)
            {
                return;
            }

            try
            {
                if (!player.IsValid || !GlobalConfig.Notifications.NotifyOtherOnChallengeComplete)
                {
                    return;
                }

                string playerName = player.PlayerName;
                _completionByLanguage.Clear();
                foreach (CCSPlayerController entry in Players.GetHumans())
                {
                    if (Players.Same(entry, player))
                    {
                        continue;
                    }

                    string language = entry.GetLanguage().Name;
                    if (!_completionByLanguage.TryGetValue(language, out string? message))
                    {
                        message = MergedCompletionMessage(entry, playerName);
                        _completionByLanguage[language] = message;
                    }

                    entry.PrintToChat(message);
                }
            }
            finally
            {
                _pendingCompletions.Clear();
                _completionByLanguage.Clear();
            }
        }

        private string CompletionMessage(
            CCSPlayerController viewer,
            string key,
            string playerName,
            ChallengeDefinition challenge,
            ChallengeTask task) =>
            ChatMessage(viewer, key)
                .Replace("{challenge}", TaskTitle(viewer, challenge, task, task.Amount))
                .Replace("{player}", playerName)
                .Replace("{total}", task.Amount.ToString())
                .Replace("{count}", task.Amount.ToString());

        /// <summary>One completion reads as before; several list their distinct titles, count/total from the first.</summary>
        private string MergedCompletionMessage(CCSPlayerController viewer, string playerName)
        {
            (ChallengeDefinition firstChallenge, ChallengeTask firstTask) = _pendingCompletions[0];
            if (_pendingCompletions.Count == 1)
            {
                return CompletionMessage(viewer, "challenges.completed.other", playerName, firstChallenge, firstTask);
            }

            StringBuilder titles = new();
            _mergedTitles.Clear();
            foreach ((ChallengeDefinition challenge, ChallengeTask task) in _pendingCompletions)
            {
                string title = TaskTitle(viewer, challenge, task, task.Amount);
                if (!_mergedTitles.Add(title))
                {
                    continue;
                }

                if (titles.Length > 0)
                {
                    titles.Append(", ");
                }

                titles.Append(title);
            }

            _mergedTitles.Clear();
            return ChatMessage(viewer, "challenges.completed.other")
                .Replace("{challenge}", titles.ToString())
                .Replace("{player}", playerName)
                .Replace("{total}", firstTask.Amount.ToString())
                .Replace("{count}", firstTask.Amount.ToString());
        }

        public void NotifyRuleBroken(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            if (!player.IsValid)
            {
                return;
            }

            PlaySound(player, GlobalConfig.Notifications.ChallengeRuleBrokenSound);
            string title = TaskTitle(player, challenge, task, 0);
            player.PrintToChat(ChatMessage(player, "challenges.rule.broken"));
            player.PrintToChat($"{ChatColors.Green}{title}");
            Tracker.ShowRuleBroken(player, challenge.Id, task.Id);
        }

        public void NotifyTaskReset(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            if (!player.IsValid)
            {
                return;
            }

            player.PrintToChat(ChatMessage(player, "challenges.deleted")
                .Replace("{challenge}", TaskTitle(player, challenge, task, 0)));
        }

        public void DiscordCompletion(
            CCSPlayerController player,
            PlayerState state,
            RunningSchedule schedule,
            ChallengeDefinition challenge,
            ChallengeTask task,
            bool challengeSolvedNow)
        {
            PluginConfigDiscord discord = GlobalConfig.Discord;
            string url = discord.WebhookUrl;
            if (string.IsNullOrEmpty(url)
                || !player.IsValid
                || !task.Visible
                || !task.AnnounceCompletion)
            {
                return;
            }

            bool singleTask = ChallengeProgress.CountVisibleTasks(challenge) <= 1;
            try
            {
                // One visible task = the challenge itself; never post a separate task card.
                if (challengeSolvedNow
                    && (discord.OnChallengeCompleted || (singleTask && discord.OnTaskCompleted)))
                {
                    QueueWebhook(url, BuildChallengeEmbed(player.PlayerName, challenge));
                    return;
                }

                if (singleTask
                    || !discord.OnTaskCompleted
                    || ParseTaskList(discord.TaskList) == TaskListMode.None)
                {
                    return;
                }

                QueueWebhook(url, BuildTaskEmbed(player.PlayerName, state, schedule, challenge, task));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges] Discord webhook failed: {ex.Message}");
            }
        }

        public void DiscordNewSchedule(RunningSchedule schedule)
        {
            PluginConfigDiscord discord = GlobalConfig.Discord;
            string url = discord.WebhookUrl;
            if (string.IsNullOrEmpty(url) || !discord.OnNewSchedule)
            {
                return;
            }

            try
            {
                QueueWebhook(url, BuildScheduleEmbed(schedule));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges] Discord webhook failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Posts sample embeds for each Discord message type using live schedule/challenge data.
        /// Returns an error message, or <c>null</c> when samples were queued.
        /// </summary>
        public string? SendDiscordTestSamples()
        {
            string url = GlobalConfig.Discord.WebhookUrl;
            if (string.IsNullOrEmpty(url))
            {
                return Localizer["admin.discord.test.no_webhook"].Value;
            }

            if (!GetModule<Schedules>().TryGetSample(out RunningSchedule schedule))
            {
                return Localizer["admin.discord.test.no_schedule"].Value;
            }

            if (!TryPickTestChallenges(
                    schedule,
                    out ChallengeDefinition? anyChallenge,
                    out ChallengeDefinition? multiChallenge,
                    out ChallengeTask? completedTask)
                || anyChallenge == null
                || multiChallenge == null
                || completedTask == null)
            {
                return anyChallenge == null
                    ? Localizer["admin.discord.test.no_challenge"].Value
                    : Localizer["admin.discord.test.no_multi_task"].Value;
            }

            string playerName = FirstPlayerName() ?? "TestPlayer";
            PlayerState demo = DemoProgress(schedule.Key, multiChallenge, completedTask);

            try
            {
                QueueWebhook(url, BuildScheduleEmbed(schedule));
                // Prefer a multi-task challenge so the completed-task list is visible.
                QueueWebhook(url, BuildChallengeEmbed(playerName, multiChallenge));
                foreach (TaskListMode mode in TestTaskListModes)
                {
                    QueueWebhook(url, BuildTaskEmbed(playerName, demo, schedule, multiChallenge, completedTask, mode));
                }
            }
            catch (Exception ex)
            {
                return Localizer["admin.discord.test.failed"].Value.Replace("{error}", ex.Message);
            }

            return null;
        }

        private static bool TryPickTestChallenges(
            RunningSchedule schedule,
            out ChallengeDefinition? anyChallenge,
            out ChallengeDefinition? multiChallenge,
            out ChallengeTask? completedTask)
        {
            anyChallenge = null;
            multiChallenge = null;
            completedTask = null;

            foreach (ChallengeDefinition entry in schedule.Challenges)
            {
                ChallengeTask? first = null;
                int visible = 0;
                foreach (ChallengeTask task in entry.Tasks)
                {
                    if (!task.Visible)
                    {
                        continue;
                    }

                    first ??= task;
                    visible++;
                }

                if (first == null)
                {
                    continue;
                }

                anyChallenge ??= entry;
                if (visible >= 2 && multiChallenge == null)
                {
                    multiChallenge = entry;
                    completedTask = first;
                }

                if (anyChallenge != null && multiChallenge != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static PlayerState DemoProgress(
            string scheduleKey,
            ChallengeDefinition challenge,
            ChallengeTask completed) =>
            new()
            {
                Challenges =
                {
                    [scheduleKey] = new Dictionary<string, Dictionary<string, TaskProgress>>(StringComparer.Ordinal)
                    {
                        [challenge.Id] = new Dictionary<string, TaskProgress>(StringComparer.Ordinal)
                        {
                            [completed.Id] = new TaskProgress { Amount = Math.Max(1, completed.Amount) },
                        },
                    },
                },
            };

        private static string? FirstPlayerName()
        {
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                if (!string.IsNullOrEmpty(player.PlayerName))
                {
                    return player.PlayerName;
                }
            }

            return null;
        }

        private Dictionary<string, object?> BuildChallengeEmbed(string playerName, ChallengeDefinition challenge)
        {
            string challengeTitle = "✅ " + Titles.Expand(
                DiscordTitle(challenge.Title),
                0,
                ChallengeProgress.CountVisibleTasks(challenge));
            string checklist = BuildAllDoneChecklist(challenge, out int total);
            return Embed(
                playerName,
                DiscordText("discord.webhook.title.challenge"),
                challengeTitle,
                [
                    Field($"{DiscordText("discord.webhook.field.tasks")} · {total} / {total}", checklist),
                ]);
        }

        private Dictionary<string, object?> BuildTaskEmbed(
            string playerName,
            PlayerState state,
            RunningSchedule schedule,
            ChallengeDefinition challenge,
            ChallengeTask task,
            TaskListMode? listMode = null)
        {
            string statusTitle = DiscordText("discord.webhook.title.task");
            TaskListMode mode = listMode ?? ParseTaskList(GlobalConfig.Discord.TaskList);
            int amount = Math.Max(1, task.Amount);
            // Always the finished task with ✅ and count/total (e.g. "✅ 5/5 …").
            string completedTaskLine = FormatTaskLine(challenge, task, amount, amount, complete: true);

            // short: only the completed task line (no checklist field).
            if (mode == TaskListMode.Short)
            {
                return Embed(playerName, statusTitle, completedTaskLine);
            }

            // full: challenge context + checklist of every visible task.
            string challengeName = Titles.Expand(
                DiscordTitle(challenge.Title),
                0,
                ChallengeProgress.CountVisibleTasks(challenge));
            string checklist = BuildFullChecklist(state, schedule.Key, challenge, out int done, out int total);
            return Embed(
                playerName,
                statusTitle,
                challengeName,
                [
                    Field($"{DiscordText("discord.webhook.field.tasks")} · {done} / {total}", checklist),
                ]);
        }

        private Dictionary<string, object?> BuildScheduleEmbed(RunningSchedule schedule)
        {
            var lines = new List<string>(schedule.Challenges.Count);
            for (int i = 0; i < schedule.Challenges.Count; i++)
            {
                lines.Add($"{i + 1}. {DiscordTitle(schedule.Challenges[i].Title)}");
            }

            string scheduleTitle = Titles.Expand(DiscordTitle(schedule.Title), 0, schedule.Challenges.Count)
                .Replace("{playerName}", "Player");
            return Embed(
                author: null,
                DiscordText("discord.webhook.title.schedule"),
                scheduleTitle,
                [
                    Field(DiscordText("discord.webhook.field.start"), FormatDate(schedule.StartDate), inline: true),
                    Field(DiscordText("discord.webhook.field.end"), FormatDate(schedule.EndDate), inline: true),
                    Field(
                        DiscordText("discord.webhook.field.challenges"),
                        JoinLimited(lines, DiscordText("discord.webhook.field.more"))),
                ]);
        }

        private Dictionary<string, object?> Embed(
            string? author,
            string title,
            string? description = null,
            List<Dictionary<string, object>>? fields = null)
        {
            var embed = new Dictionary<string, object?>
            {
                ["title"] = Truncate(title, EmbedTitleLimit),
                ["color"] = ThemeColor(),
                ["footer"] = new Dictionary<string, string>
                {
                    ["text"] = Truncate(ServerHostname(), EmbedFooterLimit),
                },
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            };

            if (!string.IsNullOrEmpty(description))
            {
                embed["description"] = Truncate(description, EmbedDescriptionLimit);
            }

            if (fields is { Count: > 0 })
            {
                embed["fields"] = fields;
            }

            if (!string.IsNullOrEmpty(author))
            {
                embed["author"] = new Dictionary<string, string>
                {
                    ["name"] = Truncate(author, EmbedAuthorLimit),
                };
            }

            return embed;
        }

        private static Dictionary<string, object> Field(string name, string value, bool inline = false) =>
            new()
            {
                ["name"] = Truncate(name, EmbedFieldNameLimit),
                ["value"] = Truncate(string.IsNullOrEmpty(value) ? "-" : value, EmbedFieldValueLimit),
                ["inline"] = inline,
            };

        private enum TaskListMode : byte
        {
            None,
            Short,
            Full,
        }

        private static TaskListMode ParseTaskList(string? value) =>
            value?.ToLowerInvariant() switch
            {
                "none" => TaskListMode.None,
                "short" => TaskListMode.Short,
                _ => TaskListMode.Full,
            };

        private string BuildAllDoneChecklist(ChallengeDefinition challenge, out int total)
        {
            PlayerState done = new();
            string checklist = BuildFullChecklist(done, "", challenge, out _, out total, allDone: true);
            return checklist;
        }

        private string BuildFullChecklist(
            PlayerState state,
            string scheduleKey,
            ChallengeDefinition challenge,
            out int done,
            out int total,
            bool allDone = false)
        {
            done = 0;
            total = 0;
            var lines = new List<string>();
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (!task.Visible)
                {
                    continue;
                }

                total++;
                int amount = Math.Max(1, task.Amount);
                int count = allDone ? amount : ChallengeProgress.GetTaskCount(state, scheduleKey, challenge.Id, task);
                bool complete = allDone || count >= amount;
                if (complete)
                {
                    done++;
                }

                lines.Add(FormatTaskLine(challenge, task, count, amount, complete));
            }

            return JoinLimited(lines, DiscordText("discord.webhook.field.more"));
        }

        private string FormatTaskLine(
            ChallengeDefinition challenge,
            ChallengeTask task,
            int count,
            int amount,
            bool complete)
        {
            string raw = DiscordTitle(Titles.TaskOrChallenge(challenge, task));
            bool hasPlaceholders = raw.Contains("{count}", StringComparison.Ordinal)
                || raw.Contains("{total}", StringComparison.Ordinal);
            string body = Titles.Expand(raw, count, amount);
            // Task/challenge titles without {count}/{total} still show progress (e.g. "5/5 Name").
            if (!hasPlaceholders)
            {
                body = $"{count}/{amount} {body}";
            }

            return $"{(complete ? "✅" : "▫️")} {body}";
        }

        private static string JoinLimited(List<string> lines, string moreTemplate, int maxLength = EmbedFieldValueLimit)
        {
            if (lines.Count == 0)
            {
                return "-";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                int gap = sb.Length == 0 ? 0 : 1;
                if (sb.Length + gap + line.Length > maxLength)
                {
                    string more = moreTemplate.Replace("{count}", (lines.Count - i).ToString());
                    if (sb.Length == 0)
                    {
                        return Truncate(more, maxLength);
                    }

                    if (sb.Length + 1 + more.Length <= maxLength)
                    {
                        sb.Append('\n').Append(more);
                    }

                    return sb.ToString();
                }

                if (sb.Length > 0)
                {
                    sb.Append('\n');
                }

                sb.Append(line);
            }

            return sb.ToString();
        }

        private int ThemeColor() => HudTheme.DiscordColor(GlobalConfig.Gui.Theme);

        private static string ServerHostname()
        {
            string? value = ConVar.Find("hostname")?.StringValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                return "CS2";
            }

            return value.Trim().Trim('"');
        }

        private string TaskTitle(CCSPlayerController viewer, ChallengeDefinition challenge, ChallengeTask task, int count) =>
            Titles.For(viewer, Titles.TaskOrChallenge(challenge, task), count, task.Amount);

        private string DiscordTitle(Dictionary<string, string> titles) =>
            Titles.Resolve(titles, DiscordCulture());

        private string DiscordText(string key)
        {
            using (new WithTemporaryCulture(DiscordCulture()))
            {
                string text = Localizer[key].Value;
                if (text == key)
                {
                    DebugPrint($"Translation for {key} not found for language {GlobalConfig.Discord.Language}");
                }

                return text;
            }
        }

        private string FormatDate(string value)
        {
            if (!Schedules.TryParseDate(value, out DateTime parsed))
            {
                return value;
            }

            DateTime utc = parsed.Kind switch
            {
                DateTimeKind.Utc => parsed,
                DateTimeKind.Local => parsed.ToUniversalTime(),
                _ => DateTime.SpecifyKind(parsed, DateTimeKind.Utc),
            };

            TimeZoneInfo zone = ResolveTimeZone();
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
            CultureInfo culture = DiscordCulture();
            // Short date + short time per discord.language (e.g. de: 01.01.2027 01:00).
            string when = local.ToString("g", culture);
            return $"{when} {FormatUtcOffset(zone.GetUtcOffset(local))}";
        }

        /// <summary>UTC, UTC+1, UTC-5, or UTC+5:30 - never UTC+00:00.</summary>
        private static string FormatUtcOffset(TimeSpan offset)
        {
            if (offset == TimeSpan.Zero)
            {
                return "UTC";
            }

            char sign = offset < TimeSpan.Zero ? '-' : '+';
            offset = offset.Duration();
            return offset.Minutes == 0
                ? $"UTC{sign}{offset.Hours}"
                : $"UTC{sign}{offset.Hours}:{offset.Minutes:00}";
        }

        private TimeZoneInfo ResolveTimeZone()
        {
            string id = GlobalConfig.Discord.Timezone?.Trim() ?? string.Empty;
            if (id.Length == 0)
            {
                return TimeZoneInfo.Local;
            }

            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out TimeZoneInfo? zone))
            {
                return zone;
            }

            // Windows hosts often lack IANA ids.
            if (id.Equals("Europe/Berlin", StringComparison.OrdinalIgnoreCase)
                && TimeZoneInfo.TryFindSystemTimeZoneById("W. Europe Standard Time", out zone))
            {
                return zone;
            }

            Console.WriteLine(
                $"[Challenges] Unknown discord.timezone '{id}', using server local ({TimeZoneInfo.Local.Id}).");
            return TimeZoneInfo.Local;
        }

        private CultureInfo DiscordCulture()
        {
            string language = GlobalConfig.Discord.Language;
            try
            {
                return CultureInfo.GetCultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                // "de" / "en" should work; try a regional default for bare codes.
                try
                {
                    return CultureInfo.GetCultureInfo(language.ToLowerInvariant() switch
                    {
                        "de" => "de-DE",
                        "en" => "en-GB",
                        _ => language,
                    });
                }
                catch (CultureNotFoundException)
                {
                    return CultureInfo.InvariantCulture;
                }
            }
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..(max - 1)] + "…";

        private static void PlaySound(CCSPlayerController player, string sound)
        {
            if (string.IsNullOrEmpty(sound) || !player.IsValid)
            {
                return;
            }

            if (sound.StartsWith("sounds/", StringComparison.Ordinal))
            {
                PlayClientSound(player, sound);
                return;
            }

            RecipientFilter filter = [player];
            player.EmitSound(sound, filter);
        }

        /// <summary>Client-local <c>play</c> only - never audible to other players.</summary>
        private static void PlayClientSound(CCSPlayerController player, string sound)
        {
            if (string.IsNullOrEmpty(sound) || !player.IsValid)
            {
                return;
            }

            player.ExecuteClientCommand($"play {sound}");
        }

        /// <summary>
        /// Stops accepting posts, lets the sender drain for up to <see cref="WebhookJoinTimeout"/>,
        /// then cancels whatever is still pending.
        /// </summary>
        public override void Destroy()
        {
            _pendingCompletions.Clear();
            Channel<WebhookPost>? queue = _webhooks;
            Task? sender = _webhookSender;
            if (queue != null && sender != null)
            {
                queue.Writer.TryComplete();
                bool drained;
                try
                {
                    drained = sender.Wait(WebhookJoinTimeout);
                }
                catch (AggregateException)
                {
                    drained = true;
                }

                if (!drained)
                {
                    Console.WriteLine(
                        $"[Challenges] Discord webhook queue not drained within {WebhookJoinTimeout.TotalSeconds:0}s; "
                        + $"dropping {queue.Reader.Count} pending post(s)");
                }
            }

            _webhookStop.Cancel();
        }

        /// <summary>Game thread: embeds are built here, serialisation and HTTP run on the single sender.</summary>
        private void QueueWebhook(string webhookUrl, Dictionary<string, object?> embed)
        {
            if (_webhookStop.IsCancellationRequested)
            {
                return;
            }

            if (_webhooks == null)
            {
                Channel<WebhookPost> queue = Channel.CreateBounded<WebhookPost>(
                    new BoundedChannelOptions(WebhookQueueCapacity)
                    {
                        FullMode = BoundedChannelFullMode.DropOldest,
                        SingleReader = true,
                        SingleWriter = true,
                    },
                    OnWebhookDropped);
                _webhooks = queue;
                CancellationToken token = _webhookStop.Token;
                _webhookSender = Task.Run(() => SendWebhooksAsync(queue.Reader, token));
            }

            _webhooks.Writer.TryWrite(new WebhookPost(webhookUrl, embed));
        }

        private void OnWebhookDropped(WebhookPost _)
        {
            if (Interlocked.Exchange(ref _webhookDropLogged, 1) == 0)
            {
                Console.WriteLine(
                    $"[Challenges] Discord webhook queue full ({WebhookQueueCapacity}); dropping the oldest posts");
            }
        }

        private async Task SendWebhooksAsync(ChannelReader<WebhookPost> reader, CancellationToken token)
        {
            try
            {
                while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out WebhookPost post))
                    {
                        await SendWebhookAsync(post, token).ConfigureAwait(false);
                    }

                    Volatile.Write(ref _webhookDropLogged, 0);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges] Discord webhook sender stopped: {ex.Message}");
            }
        }

        /// <summary>Posts one embed; on 429 waits for <c>Retry-After</c> and retries, up to <see cref="WebhookMaxAttempts"/> tries.</summary>
        private static async Task SendWebhookAsync(WebhookPost post, CancellationToken token)
        {
            string json;
            try
            {
                json = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["embeds"] = new[] { post.Embed },
                    ["allowed_mentions"] = new Dictionary<string, object> { ["parse"] = NoMentionParse },
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges] Discord webhook failed: {ex.Message}");
                return;
            }

            for (int attempt = 1; ; attempt++)
            {
                TimeSpan wait;
                try
                {
                    using StringContent content = new(json, Encoding.UTF8, "application/json");
                    using HttpResponseMessage response = await Http.PostAsync(post.Url, content, token).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        // Bucket exhausted: wait for its reset before the next post instead of running into a 429.
                        if (HeaderValue(response, "X-RateLimit-Remaining") == "0"
                            && ParseSeconds(HeaderValue(response, "X-RateLimit-Reset-After")) is { } reset)
                        {
                            await Task.Delay(reset, token).ConfigureAwait(false);
                        }

                        return;
                    }

                    if ((int)response.StatusCode != 429 || attempt >= WebhookMaxAttempts)
                    {
                        Console.WriteLine(
                            $"[Challenges] Discord webhook failed: {(int)response.StatusCode} {response.ReasonPhrase}");
                        return;
                    }

                    wait = await RetryAfterAsync(response, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Challenges] Discord webhook failed: {ex.Message}");
                    return;
                }

                await Task.Delay(wait, token).ConfigureAwait(false);
            }
        }

        /// <summary>JSON <c>retry_after</c>, else the <c>Retry-After</c> header, else one second; capped at a minute.</summary>
        private static async Task<TimeSpan> RetryAfterAsync(HttpResponseMessage response, CancellationToken token)
        {
            TimeSpan? wait = null;
            try
            {
                string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("retry_after", out JsonElement retry)
                    && retry.ValueKind == JsonValueKind.Number)
                {
                    wait = TimeSpan.FromSeconds(retry.GetDouble());
                }
            }
            catch (Exception ex) when (ex is JsonException or HttpRequestException)
            {
            }

            if (wait == null && response.Headers.RetryAfter is { } header)
            {
                wait = header.Delta ?? (header.Date is { } date ? date - DateTimeOffset.UtcNow : null);
            }

            wait ??= ParseSeconds(HeaderValue(response, "Retry-After")) ?? WebhookDefaultRetry;
            return wait.Value < TimeSpan.Zero
                ? TimeSpan.Zero
                : wait.Value > WebhookMaxRetry ? WebhookMaxRetry : wait.Value;
        }

        private static string? HeaderValue(HttpResponseMessage response, string name) =>
            response.Headers.TryGetValues(name, out IEnumerable<string>? values) ? values.FirstOrDefault() : null;

        private static TimeSpan? ParseSeconds(string? value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) && seconds >= 0
                ? TimeSpan.FromSeconds(Math.Min(seconds, WebhookMaxRetry.TotalSeconds))
                : null;
    }
}
