using CounterStrikeSharp.API.Core;

namespace ChallengesShared.Events;

public record PlayerCompletedChallengeEvent(CCSPlayerController Player, Dictionary<string, string> Data) : IChallengesEvent;
