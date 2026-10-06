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
            HashSet<string> catalogClasses = catalog.Events
                .Select(e => e.EventClass)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> extractorClasses = Registry.All
                .Select(e => e.EventClassName)
                .ToHashSet(StringComparer.Ordinal);

            foreach (string missing in extractorClasses.Except(catalogClasses))
            {
                messages.Add(Format(localizer, $"extractor {missing} has no catalog entry"));
            }

            foreach (string missing in catalogClasses.Except(extractorClasses))
            {
                messages.Add(Format(localizer, $"catalog event {missing} has no extractor"));
            }

            HashSet<string> catalogTypes = catalog.Events
                .Select(e => e.Type)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> extractorTypes = Registry.All
                .SelectMany(e => e.ChallengeTypes)
                .ToHashSet(StringComparer.Ordinal);

            foreach (string missing in extractorTypes.Except(catalogTypes))
            {
                messages.Add(Format(localizer, $"extractor type {missing} has no catalog entry"));
            }

            foreach (string missing in catalogTypes.Except(extractorTypes))
            {
                messages.Add(Format(localizer, $"catalog type {missing} has no extractor"));
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
