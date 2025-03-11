> [!TIP]
> Actions can be complex to understand. Please look into the *examples* folder of this repository before asking questions in our Discord.

# Actions

## What are actions used for?

You can add actions to challenges to give you more control over them. For example, you can make challenges harder by checking if a player stays within certain boundaries. Let's look at an example from the *examples* folder in this repository. The file *headshots_in_a_row.json* contains two challenges for a player. In the first, easier challenge, the player needs to get 3 headshots in a row. But how do you know if the player actually got three headshots in a row without missing?

## What a control challenges?

This is where actions come in. An action can be assigned to a control challenge (and also to a normal challenge), which can be hidden from the player. This control challenge checks if the player kills someone without a headshot. If this happens, an action resets the 3 headshots in a row challenge, making the player start over. You can have unlimited control challenges for a given challenge, using all possible events in Counterstrike 2 to make your challenge more difficult.

## What actions can I use?

These are the current actions you can use for a challenge.

### challenge.delete.progress

Deletes the progress of an unfinished challenge. Will not delete the progress of a finished challenge. The value must be the complete unique challenge name. If the value does not include the file name the current file will be used for that filename.

### challenge.delete.completed

Delete the progress of a challenge even if it is already completed. The value must be the complete unique challenge name. If the value does not include the file name the current file will be used for that filename.

### challenge.mark.completed

Marks a given challenge as complete regardless of the current state. The value must be the complete unique challenge name. If the value does not include the file name the current file will be used for that filename.

### notify.player.progress.rule_broken

Notify the user when they break a rule during their progress. The title of the rule that triggers this action will be shown to the user as a message. The values must include the identifiers of the rules to check if they are broken. The rules given must not be completed or otherwise the notification will not be displayed. This notification must happen before deleting the progress of a rule, otherwise, it will not be displayed. See the example below.

### notify.player.completed.rule_broken

Notify the user when they break a rule after completing it. The title of the rule that triggers this action will be shown to the user as a message. The values should include the identifiers of the rules to check if they are broken. The rules given must be completed or otherwise the notification will not be displayed. This notification must happen before deleting the completion of a rule, otherwise, it will not be displayed.

## How should control challenges be configured?

Control challenges should be configured with the following parameters to make them work properly:

```
"OnPlayerKillHeadshot.easy.control.noheadshot": {
    "title": {
        "en": "Rule broken: without headshot",
        "de": "Regel verletzt: ohne Headshot"
    },
    "type": "player_kill",
    "amount": 1,
    "cooldown": 0,
    "is_visible": false,
    "announce_progress": false,
    "announce_completion": false,
    "data": {},
    "rules": [
        {
            "key": "global.isduringround",
            "operator": "bool==",
            "value": "true"
        },
        {
            "key": "headshot",
            "operator": "bool==",
            "value": "false"
        }
    ],
    "actions": [
        {
            "type": "notify.player.progress.rule_broken",
            "values": ["OnPlayerKillHeadshot.easy"]
        },
        {
            "type": "challenge.delete.progress",
            "values": ["OnPlayerKillHeadshot.easy"]
        },
        {
            "type": "challenge.delete.completed",
            "values": ["OnPlayerKillHeadshot.easy.control.noheadshot"]
        }
    ],
    "dependencies": []
}
```

### title

The title for a control rule should consist of the message that gets sent to the user if the rule gets executed (and thus the challenge boundaries where broken). In this example we want to notify the user with the message *Rule broken: without headshot*. Make sure to add the action *notify.player.progress.rule_broken* to send this message to the player.

### type

Adjust to your needs to create the proper control challenge. Please refer to the blueprints documentation for further information.

### amount

The amount **must be** 1 to trigger the actions after the first event occured. Change only if you want to have a grace-period until the actions will be executed. For example giving the player a grace-period until his challenge will be reset.

### cooldown

Adjust to your needs to create the proper control challenge. Please refer to the blueprints documentation for further information.

### is_visible

Must be set to *false* once you have debugged your challenge (leaving this enabled will show it in the list of challenges and helping you to visually see what is happening).

### announce_progress

Must be set to *false*.

### announce_completion

Must be set to *false*. Announcement will be handled via an action if you want to inform the user.

### data

Please refer to the blueprints documentation for further information.

### rules

Adjust to your needs to create the proper control challenge. Please refer to the blueprints documentation for further information.

### actions

All actions that should be executed. Will be executed from top to bottom. Read this documentation page completely to fully understand what a control challenge is and what actions you can use.


### dependencies

Adjust to your needs to create the proper control challenge. Please refer to the blueprints documentation for further information.

## Example actions for a control challenge

```
"actions": {
    {
        "action": "notify.player.progress.rule_broken",
        "values": ["OnPlayerKillHeadshot.easy"]
    }
    {
        "action": "challenge.delete.progress",
        "values": ["OnPlayerKillHeadshot.easy"]
    },
    {
        "action": "challenge.delete.completed",
        "values": ["OnPlayerKillHeadshot.easy.control.noheadshot"]
    }
}
```

This example demonstrates how to reset the progress of the challenge *OnPlayerKillHeadshot.easy* in our example file *headshots_in_a_row.json*. The first action notifies the user about the broken rule and displays a message in the center of the screen and in the text chat. It is necessary to add the name(s) of the rule(s) that the user could possible have broken. It will be checked whether the user made some progress on them before displaying a message. According to the second action the reset occurs only if the challenge is not yet completed. For instance, if you have achieved 2 out of the 3 required headshots. The third action resets the status of our control challenge, allowing it to be executed again. Otherwise, it would be marked as completed and would not run anymore.