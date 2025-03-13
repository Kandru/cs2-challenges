> [!TIP]
> Check the examples folder for the latest examples you can use in the configuration folder of the Challenges-Plugin. Verify the timings in *schedules.yaml*. Make sure to **read all documentation**. This plugin is complex and **NOT** easy to understand. If you still have questions, join our Discord for help.

# How to start

## Installation & Update

1. **Download the latest release** from the releases page. These releases are built automatically by GitHub whenever we update the code.
2. **Place the folders**:
    - *Challenges* folder into the *plugin* folder of your CounterstrikeSharp installation.
    - *ChallengesShared* folder into the *shared* folder of your CounterstrikeSharp installation.
3. **Restart the CS2 server**. Default configurations will be created for you.

To update the plugin:
1. Stop your server.
2. Follow the installation steps above by overwriting the files.

## Quick start with example challenges

1. Ensure the CS2 server is not running.
2. After installing the Challenges-Plugin, copy:
    - *schedules.yaml* file from the *examples* folder of this repository.
    - Entire *blueprints* directory to your server's *config* folder of the Challenges-Plugin.
3. Check the *schedules.yaml* file to ensure the start and end times of the example challenge are correct.
4. Adjust the settings in the *Challenges.json* file to your preferences.
5. Start the CS2 server to load everything.

Join your server and check if the challenges GUI appears in the top right corner. You can also use the chat commands *!c* or *!challenges* to toggle the GUI on or off.

If no challenges are visible:
- Check your CounterstrikeSharp log files.
- Enable debug messages in the config file to get hints about any syntax errors in the *.yaml* files.
- Regularly review the CounterstrikeSharp log files whenever you make changes to the server to avoid configuration mistakes.

## Check our documentation for further help

Please read all of our documentation carefully. The Challenges-Plugin is complex and takes time to understand. The complete documentation can be accessed from the *README* of this repository.

## Important: Ask your favorite Plugin developers for integration!

The Challenges-Plugin does not give rewards to players on its own. You need another plugin to handle rewards after the Challenges-Plugin completes its tasks. Without this integration, the Challenges-Plugin won't be very useful. Once you set up a third-party plugin to work with our Challenges-Plugin, you can reward players when they complete challenges. This setup allows for many possibilities. Please link to the README of this repository so that the third-party plugin developer can start the integration.
