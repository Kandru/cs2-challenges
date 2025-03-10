> [!TIP]
> Check the examples folder for the latest examples you can use in the configuration folder of the Challenges-Plugin. Verify the timings in *schedules.json*. Make sure to **read all documentation**. This plugin is complex and **NOT** easy to understand. If you still have questions, join our Discord for help.

# How to start

## What is the Challenges-Plugin?

The Challenges-Plugin is a tool to create and manage many challenges in your application. You can define custom challenges, set schedules, and track progress. The plugin is highly configurable, supporting various challenge types and scheduling options. It integrates with other server plug-ins, providing a solution for gamification and user engagement.

For detailed examples and configuration instructions, check the examples folder and read all the provided documentation before asking questions in our Discord.

## Installation & Update

Download the latest release from the releases page. These releases are built automatically by GitHub whenever we update the code. Place the *Challenges* folder into the *plugin* folder of your CounterstrikeSharp installation and the *ChallengesShared* folder into the *shared* folder of your CounterstrikeSharp installation. Restart the CS2 server afterward. Default configurations will be created for you.

To update the plugin, stop your server and follow the steps above by overwriting the files.

## Quick start with example challenges

Ensure the CS2 server is not running before you start. After installing the Challenges-Plugin, copy the *schedules.json* file from the *examples* folder of this repository and the entire *blueprints* directory to your server's *config* folder of the Challenges-Plugin. Check the *schedules.json* file to ensure the start and end times of the example challenge are correct. Adjust the settings in the *Challenges.json* file to your preferences. Start the CS2 server to load everything.

Join your server and check if the challenges GUI appears in the top right corner. You can also use the chat commands *!c* or *!challenges* to toggle the GUI on or off.

If no challenges are visible, check your CounterstrikeSharp log files. Enable debug messages in the config file to get hints about any syntax errors in the *.json* files. Regularly review the CounterstrikeSharp log files whenever you make changes to the server to avoid configuration mistakes.

## Check our documentation for further help

Please read all of our documentation carefully. The Challenges-Plugin is complex and takes time to understand. The complete documentation can be accessed from the *README* of this repository.

## Important: Ask your favorite Plugin developers for integration!

The Challenges-Plugin does not give rewards to players on its own. You need another plugin to handle rewards after the Challenges-Plugin completes its tasks. Without this integration, the Challenges-Plugin won't be very useful. Once you set up a third-party plugin to work with our Challenges-Plugin, you can reward players when they complete challenges. This setup allows for many possibilities. Please link to the README of this repository so that the third-party plugin developer can start the integration.
