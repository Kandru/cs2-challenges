using CounterStrikeSharp.API.Core;

namespace ChallengesShared.Events;

public record PlayerProgressedChallengeEvent(CCSPlayerController Player, Dictionary<string, string> Data) : IChallengesEvent;
