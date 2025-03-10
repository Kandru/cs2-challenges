namespace ChallengesShared.Events;

public record PlayerProgressedChallengeEvent(int UserId, Dictionary<string, Dictionary<string, string>> Data) : IChallengesEvent;
