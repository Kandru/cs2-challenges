# Third-Party Plugin Integration

## Basic Integration

Integrating a third-party plugin is straightforward. First, import the latest version of our interface from the main branch of our repository. Then, refer to our example plugin to learn how to create a listener for when a challenge is completed or progresses. There is a single listener for both events, and more events may be added in the future.

When you start listening, you will receive events from all plugins. You need to check if the data is relevant to your plugin. Whenever possible, use a separate thread to process the events and then pass them back to the main thread. This prevents the server from becoming unresponsive if your plugin (or others) perform too many tasks on the main thread. Any listening plugin will add up to the total time it takes to finish the event handling.

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

```json
"data": {
    "ExamplePlugin": {
        "ExamplePoints": "1"
    }
}
```

In this example, the data is for the *ExamplePlugin*. Your plugin should listen for a specific key that matches your plugin name (case-sensitive by default). It is good practice to compare the key in lowercase to handle any typos. You can then check for any key/value pairs you need. Ensure you cast them properly with error handling to fit your requirements.

What can be in your key/value pairs? Anything you want to grant to a player when they win or progress in a challenge. Give users of your plugin the flexibility to customize the player experience. This adds significant value for everyone using challenges.

## Example Integrations

Here are some ideas for imaginary plugins that you can integrate with our Challenges-Plugin easily:

1. **Experience Points Plugin**
    ```json
    "data": {
        "ExperiencePointsPlugin": {
            "ExperienceGained": "500"
        }
    }
    ```
    This plugin grants players experience points when they complete a challenge.

2. **Currency Plugin**
    ```json
    "data": {
        "CurrencyPlugin": {
            "CoinsAwarded": "100"
        }
    }
    ```
    This plugin awards in-game currency to players upon challenge completion.

3. **Item Reward Plugin**
    ```json
    "data": {
        "ItemRewardPlugin": {
            "ItemID": "awp_001",
            "Quantity": "1"
        }
    }
    ```
    This plugin gives players a specific item as a reward for completing a challenge.

4. **Badge Plugin**
    ```json
    "data": {
        "BadgePlugin": {
            "BadgeID": "challenge_master"
        }
    }
    ```
    This plugin awards a badge to players who complete a challenge.

## Free Advertisement

To further promote your plugin and our Challenges-Plugin, we invite you to make a Pull Request to our README.md with the link to your repository once you have finished and tested the integration. This way, we can directly link back to your plugin. Please also link back to our Challenges-Plugin to make it easy for your users to discover that you support the Challenges-Plugin.
