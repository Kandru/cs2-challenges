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
        [JsonPropertyName("events")] public List<CatalogEvent> Events { get; set; } = [];

        public static void Validate(Catalog catalog, IStringLocalizer localizer)
        {
            HashSet<string> catalogClasses = catalog.Events
                .Select(e => e.EventClass)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> extractorClasses = Registry.All
                .Select(e => e.EventClassName)
                .ToHashSet(StringComparer.Ordinal);

            foreach (string missing in extractorClasses.Except(catalogClasses))
            {
                Console.WriteLine(localizer["core.faultyconfig"].Value
                    .Replace("{config}", "catalog.json")
                    .Replace("{error}", $"extractor {missing} has no catalog entry"));
            }

            foreach (string missing in catalogClasses.Except(extractorClasses))
            {
                Console.WriteLine(localizer["core.faultyconfig"].Value
                    .Replace("{config}", "catalog.json")
                    .Replace("{error}", $"catalog event {missing} has no extractor"));
            }
        }
    }

    public sealed class CatalogEvent
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("event_class")] public string EventClass { get; set; } = "";
        [JsonPropertyName("extractor")] public string Extractor { get; set; } = "";
        [JsonPropertyName("keys")] public List<string> Keys { get; set; } = [];
    }
}
