// many thanks to https://github.com/B3none/cs2-retakes for the shared library (GPL-3.0 licensed 2025.03.04)
using ChallengesShared.Events;

namespace ChallengesShared;

public interface IChallengesEventSender
{
    public event EventHandler<IChallengesEvent> Events;
    public void TriggerEvent(IChallengesEvent @event);
}
