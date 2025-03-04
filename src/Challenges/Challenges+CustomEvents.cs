using ChallengesShared;
using ChallengesShared.Events;

namespace Challenges
{
    public class CustomEventsSender : IChallengesEventSender
    {
        public event EventHandler<IChallengesEvent>? Events;

        public void TriggerEvent(IChallengesEvent @event)
        {
            Events?.Invoke(this, @event);
        }
    }

    public partial class Challenges
    {
        private void TriggerEvent(IChallengesEvent @event)
        {
            try
            {
                var events = ChallengesEvents.Get();
                if (events == null)
                {
                    Console.WriteLine(Localizer["core.event.trigger.error"].Value
                        .Replace("{eventName}", @event.GetType().Name)
                        .Replace("{error}", "Events capability not available"));
                    return;
                }
                events.TriggerEvent(@event);
            }
            catch (Exception e)
            {
                Console.WriteLine(Localizer["core.event.trigger.error"].Value
                    .Replace("{eventName}", @event.GetType().Name)
                    .Replace("{error}", e.Message));
            }
        }
    }
}