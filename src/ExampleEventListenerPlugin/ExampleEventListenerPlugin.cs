using ChallengesShared;
using ChallengesShared.Events;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace ExampleEventListenerPlugin
{
    public partial class ExampleEventListenerPlugin : BasePlugin
    {
        public override string ModuleName => "ExampleEventListenerPlugin";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";
        public override string ModuleVersion => "1.0.0";

        private static PluginCapability<IChallengesEventSender> ChallengesEvents { get; } = new("challenges:events");

        public override void Load(bool hotReload)
        {
            Console.WriteLine("==== Example Event Listener Plugin for CS2 Challenges loaded! ====");
        }

        public override void OnAllPluginsLoaded(bool isReload)
        {
            Console.WriteLine("==== Example Event Listener Plugin initializes Event Listener! ====");
            ChallengesEvents.Get()!.Events += OnChallengesEvent;
        }

        public override void Unload(bool hotReload)
        {
            Console.WriteLine("==== Example Event Listener Plugin unloading! ====");
            ChallengesEvents.Get()!.Events -= OnChallengesEvent;
        }

        // you can test this with the command `sendtestchallengeevent` in the game console (requires @css/root permissions)
        private void OnChallengesEvent(object? sender, IChallengesEvent @event)
        {
            Console.WriteLine("==== Example Event Listener Plugin got a new event! ====");
            Console.WriteLine($"Event: {@event.GetType().Name}");
            if (@event is PlayerCompletedChallengeEvent playerCompletedChallenge)
            {
                // convert to CCSPlayerController by yourself
                Console.WriteLine($"Player: {playerCompletedChallenge.UserId}");
                // specific challenge data (can be totally custom, you NEED custom challenge data for YOUR plugin)
                // data is ALWAYS string -> cast it to the correct type on your own!
                // make sure to have a fallback in place and notify player in case of invalid data
                foreach (var kvp in playerCompletedChallenge.Data)
                {
                    Console.WriteLine($"Plugin: {kvp.Key}");
                    foreach (var data in kvp.Value)
                    {
                        Console.WriteLine($"-> {data.Key} = {data.Value}");
                    }
                }
            }
            else if (@event is PlayerProgressedChallengeEvent playerProgressedChallenge)
            {
                // convert to CCSPlayerController by yourself
                Console.WriteLine($"Player: {playerProgressedChallenge.UserId}");
                // specific challenge data (can be totally custom, you NEED custom challenge data for YOUR plugin)
                // data is ALWAYS string -> cast it to the correct type on your own!
                // make sure to have a fallback in place and notify player in case of invalid data
                foreach (var kvp in playerProgressedChallenge.Data)
                {
                    Console.WriteLine($"Plugin: {kvp.Key}");
                    foreach (var data in kvp.Value)
                    {
                        Console.WriteLine($"-> {data.Key} = {data.Value}");
                    }
                }
            }
        }
    }
}
