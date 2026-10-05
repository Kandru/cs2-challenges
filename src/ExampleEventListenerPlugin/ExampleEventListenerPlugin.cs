using ChallengesShared;
using ChallengesShared.Events;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace ExampleEventListenerPlugin;

public class ExampleEventListenerPlugin : BasePlugin
{
    public override string ModuleName => "ExampleEventListenerPlugin";
    public override string ModuleAuthor => "Kalle <kalle@kandru.de>";
    public override string ModuleVersion => "1.0.0";

    private static PluginCapability<IChallengesEventSender> ChallengesEvents { get; } = new("challenges:events");

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        ChallengesEvents.Get()!.Events += OnChallengesEvent;
    }

    public override void Unload(bool hotReload)
    {
        ChallengesEvents.Get()!.Events -= OnChallengesEvent;
    }

    private void OnChallengesEvent(object? sender, IChallengesEvent @event)
    {
        // UserId identifies the player; resolve the controller yourself.
        // Data is plugin name -> key -> value, always strings: parse defensively.
        switch (@event)
        {
            case PlayerCompletedChallengeEvent completed:
                Dump("completed", completed.UserId, completed.Data);
                break;
            case PlayerProgressedChallengeEvent progressed:
                Dump("progressed", progressed.UserId, progressed.Data);
                break;
        }
    }

    private static void Dump(string kind, int userId, Dictionary<string, Dictionary<string, string>> data)
    {
        Console.WriteLine($"[ExampleEventListener] user {userId} {kind}");
        foreach ((string plugin, Dictionary<string, string> values) in data)
        {
            foreach ((string key, string value) in values)
            {
                Console.WriteLine($"  {plugin}.{key} = {value}");
            }
        }
    }
}
