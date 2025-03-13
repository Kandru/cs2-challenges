# Third-Party Plugin Integration

## Basic Integration

Integrating a third-party plugin is straightforward. First, import the latest version of our interface from the main branch of our repository. Then, refer to our example plugin to learn how to create a listener for when a challenge is completed or progresses. There is a single listener for both events, and more events may be added in the future.

When you start listening, you will receive events from all plugins. You need to check if the data is relevant to your plugin. The Challenges-Plugin itself uses a separate thread to check for challenge goals. However, you **should** use a separate thread as well or otherwise the server may will lag if the execution time is too long.

```c#
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
```

The example above shows all the data you receive from a plugin. This data is what the blueprint creator includes in the *data* dictionary for a challenge.

```yaml
data:
PlayerSessions:
    setpoints: "30"
```

In this example, the data is for the *PlayerSessions*. Your plugin should listen for a specific key that matches your plugin name (case-sensitive by default). It is good practice to compare the key in lowercase to handle any typos. You can then check for any key/value pairs you need. Ensure you cast them properly with error handling to fit your requirements.

What can be in your key/value pairs? Anything you want to grant to a player when they win or progress in a challenge. Give users of your plugin the flexibility to customize the player experience. This adds significant value for everyone using challenges.

## Free Advertisement

To further promote your plugin and our Challenges-Plugin, we invite you to make a Pull Request to our README.md with the link to your repository once you have finished and tested the integration. This way, we can directly link back to your plugin. Please also link back to our Challenges-Plugin to make it easy for your users to discover that you support the Challenges-Plugin.
