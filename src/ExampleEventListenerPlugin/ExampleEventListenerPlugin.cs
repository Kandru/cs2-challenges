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

        private void OnChallengesEvent(object? sender, IChallengesEvent @event)
        {
            Console.WriteLine("==== Example Event Listener Plugin got a new event! ====");
        }
    }
}
