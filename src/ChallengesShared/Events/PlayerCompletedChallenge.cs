namespace ChallengesShared.Events;

public record PlayerCompletedChallengeEvent(int UserId, Dictionary<string, Dictionary<string, string>> Data) : IChallengesEvent;
