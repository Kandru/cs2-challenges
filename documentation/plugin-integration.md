# Third-party plugin integration

> [!NOTE]
> This page is for **plugin authors**. Server owners only need another plugin that already supports Challenges (see the README “Compatible plugins” list).

This plugin tracks challenges. Your plugin grants rewards when a player progresses or completes one.

## Listen for events

1. Reference `ChallengesShared` from this repository.
2. Resolve the `challenges:events` capability (`IChallengesEventSender`).
3. Subscribe to `Events`. You get both progress and completion on that one handler.

Keep heavy work off the main thread (or keep it short) so the server does not lag.

```csharp
if (@event is PlayerCompletedChallengeEvent completed)
{
    // UserId → resolve CCSPlayerController yourself
    Console.WriteLine($"Player: {completed.UserId}");

    // data is string → string; cast yourself and handle bad values
    foreach (var pluginEntry in completed.Data)
    {
        Console.WriteLine($"Plugin: {pluginEntry.Key}");
        foreach (var pair in pluginEntry.Value)
        {
            Console.WriteLine($"-> {pair.Key} = {pair.Value}");
        }
    }
}
```

Progress uses `PlayerProgressedChallengeEvent` with the same shape.

## Challenge `data`

Blueprint authors put a nested map under `data`. Use your plugin name as the top-level key (compare case-insensitively if you want to forgive typos):

```yaml
data:
  PlayerSessions:
    setpoints: "30"
```

Anything you need to grant can live there. Keep values as strings in YAML; parse them in your plugin with a fallback if they are wrong.

## Advertise support

When your integration is tested, open a pull request against this README to add your plugin under “Compatible plugins”, and link back to Challenges from your own docs.
