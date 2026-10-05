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
    - *schedules.yaml* from the *examples* folder into the plugin config folder.
    - The *blueprints* directory (one YAML file per challenge) into that same config folder.
3. Mount the Workshop addon content from *workshop/content* (Panorama layouts/styles) so the HUD can load.
4. Check *schedules.yaml* dates and challenge ids (filename stems, no `file:key` prefixes).
5. Adjust *Challenges.json*, then start the CS2 server.

At round start (during freeze time) the tracker appears top-right. Use *!c* / *!challenges* for the fullscreen menu. Build challenges in the browser via the GitHub Pages builder under *builder/* (https://kandru.github.io/cs2-challenges/).

If no challenges are visible:
- Check your CounterstrikeSharp log files.
- Enable debug messages in the config file to get hints about any syntax errors in the *.yaml* files.
- Regularly review the CounterstrikeSharp log files whenever you make changes to the server to avoid configuration mistakes.

## Check our documentation for further help

Please read all of our documentation carefully. The Challenges-Plugin is complex and takes time to understand. The complete documentation can be accessed from the *README* of this repository.

## Important: Ask your favorite Plugin developers for integration!

The Challenges-Plugin does not give rewards to players on its own. You need another plugin to handle rewards after the Challenges-Plugin completes its tasks. Without this integration, the Challenges-Plugin won't be very useful. Once you set up a third-party plugin to work with our Challenges-Plugin, you can reward players when they complete challenges. This setup allows for many possibilities. Please link to the README of this repository so that the third-party plugin developer can start the integration.
