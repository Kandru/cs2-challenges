using System.Text.Json.Serialization;
using Challenges.Extractors;
using Microsoft.Extensions.Localization;

namespace Challenges.Configs
{
    public sealed class Catalog
    {
        [JsonPropertyName("operators")] public List<string> Operators { get; set; } = [];
        [JsonPropertyName("action_types")] public List<string> ActionTypes { get; set; } = [];
        [JsonPropertyName("weapons")] public List<string> Weapons { get; set; } = [];
        [JsonPropertyName("key_sets")] public Dictionary<string, List<CatalogKey>> KeySets { get; set; } = new(StringComparer.Ordinal);
        [JsonPropertyName("events")] public List<CatalogEvent> Events { get; set; } = [];

        public static List<string> Mismatches(Catalog catalog, IStringLocalizer localizer)
        {
            List<string> messages = [];
            HashSet<string> catalogClasses = new(StringComparer.Ordinal);
            HashSet<string> catalogTypes = new(StringComparer.Ordinal);
            foreach (CatalogEvent entry in catalog.Events)
            {
                // Synthetic types (e.g. player_trade) have no CSS extractor.
                if (string.Equals(entry.Kind, "synthetic", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.EventClass))
                {
                    catalogClasses.Add(entry.EventClass);
                }

                if (!string.IsNullOrWhiteSpace(entry.Type))
                {
                    catalogTypes.Add(entry.Type);
                }
            }

            HashSet<string> extractorClasses = new(StringComparer.Ordinal);
            HashSet<string> extractorTypes = new(StringComparer.Ordinal);
            foreach (IExtractor extractor in Registry.All)
            {
                extractorClasses.Add(extractor.EventClassName);
                foreach (string type in extractor.ChallengeTypes)
                {
                    extractorTypes.Add(type);
                }
            }

            foreach (string missing in extractorClasses)
            {
                if (!catalogClasses.Contains(missing))
                {
                    messages.Add(Format(localizer, $"extractor {missing} has no catalog entry"));
                }
            }

            foreach (string missing in catalogClasses)
            {
                if (!extractorClasses.Contains(missing))
                {
                    messages.Add(Format(localizer, $"catalog event {missing} has no extractor"));
                }
            }

            foreach (string missing in extractorTypes)
            {
                if (!catalogTypes.Contains(missing))
                {
                    messages.Add(Format(localizer, $"extractor type {missing} has no catalog entry"));
                }
            }

            foreach (string missing in catalogTypes)
            {
                if (!extractorTypes.Contains(missing))
                {
                    messages.Add(Format(localizer, $"catalog type {missing} has no extractor"));
                }
            }

            return messages;
        }

        private static string Format(IStringLocalizer localizer, string error) =>
            localizer["core.faultyconfig"].Value
                .Replace("{config}", "catalog.json")
                .Replace("{error}", error);
    }

    public sealed class CatalogEvent
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("event_class")] public string EventClass { get; set; } = "";
        [JsonPropertyName("extractor")] public string Extractor { get; set; } = "";
        [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    }

    public sealed class CatalogKey
    {
        [JsonPropertyName("key")] public string Key { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
    }
}
