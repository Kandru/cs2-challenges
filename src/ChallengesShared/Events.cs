// many thanks to https://github.com/B3none/cs2-retakes for the shared library (GPL-3.0 licensed 2025.03.04)

namespace ChallengesShared
{
    public interface IChallengesEventSender
    {
        public event EventHandler<Events.IChallengesEvent> Events;
        public void TriggerEvent(Events.IChallengesEvent @event);
    }
}

namespace ChallengesShared.Events
{
    public interface IChallengesEvent { }

    public record AllocateEvent() : IChallengesEvent;

    public record PlayerProgressedChallengeEvent(int UserId, Dictionary<string, Dictionary<string, string>> Data) : IChallengesEvent;

    public record PlayerCompletedChallengeEvent(int UserId, Dictionary<string, Dictionary<string, string>> Data) : IChallengesEvent;
}
