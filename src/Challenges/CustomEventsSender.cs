using ChallengesShared;
using ChallengesShared.Events;

namespace Challenges
{
    public sealed class CustomEventsSender : IChallengesEventSender
    {
        /// <summary>Instance registered under the <c>challenges:events</c> capability.</summary>
        public static CustomEventsSender? Instance { get; set; }

        public event EventHandler<IChallengesEvent>? Events;

        public void TriggerEvent(IChallengesEvent @event)
        {
            try
            {
                Events?.Invoke(this, @event);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges] Error while triggering event {@event.GetType().Name}: {ex.Message}");
            }
        }
    }
}
