> [!TIP]
> Actions can be complex. Check the *examples* folder in this repository before asking questions in our Discord.

# Actions

## What are actions used for?

Actions give you more control over challenges. For example, you can make challenges harder by checking if a player stays within certain boundaries. Let's look at an example from the *examples* folder. The file *blind_them_by_the_light.json* contains three challenges:

1. Blind 5 enemies.
2. Blind 15 enemies for at least 1 second without blinding teammates or yourself for 2+ seconds.
3. Blind 20 enemies for at least 2 seconds with the same restrictions.

The second challenge unlocks after completing the first one, and the third challenge unlocks after the second one.

## What are control challenges?

Control challenges can be hidden from the player and check specific conditions, like if the player blinds themselves or teammates. If this happens, an action resets the other challenge, making the player start over. You can have unlimited control challenges for a given challenge.

## What actions can I use?

### challenge.delete.progress

Deletes the progress of an unfinished challenge. The value must be the challenge name.

### challenge.delete.completed

Deletes the progress of a completed challenge. The value must be the challenge name.

### challenge.mark.completed

Marks a challenge as complete regardless of its current state. The value must be the unique challenge name.

### notify.player.progress.rule_broken

Notifies the user when they break a rule during their progress. The values must include the challenge names of the rules to check.

### notify.player.completed.rule_broken

Notifies the user when they break a rule after completing it. The values should include the challenge names of the rules to check.

## How should control challenges be configured?

Control challenges should be configured with the following parameters:

```yaml
OnPlayerBlind.hard.control.noselfflash:
  title:
    en: "Rule broken: do not flash yourself (>2s)!"
    de: "Regel verletzt: selbst geblendet (>2s)!"
  type: player_got_blinded
  amount: 1
  cooldown: 0
  is_visible: false
  announce_progress: false
  announce_completion: false
  rules:
    - key: global.iswarmup
      operator: bool==
      value: "false"
    - key: global.isduringround
      operator: bool==
      value: "true"
    - key: isselfflash
      operator: bool==
      value: "true"
    - key: blindduration
      operator: ">="
      value: "2"
  actions:
    - type: notify.player.progress.rule_broken
      values:
        - OnPlayerBlind.hard
    - type: challenge.delete.progress
      values:
        - OnPlayerBlind.hard
    - type: challenge.delete.completed
      values:
        - OnPlayerBlind.hard.control.noselfflash
  dependencies:
    - OnPlayerBlind.medium
```

### title

The title for a control rule should consist of the message that gets sent to the user if the rule gets executed (and thus the challenge boundaries where broken). In this example we want to notify the user with the message *Rule broken: do not flash yourself (>2s)!*. Make sure to add the action *notify.player.progress.rule_broken* to send this message to the player.

### type

Adjust to your needs to create the proper control challenge. Please refer to the blueprints documentation for further information on all available events.

### amount

The amount **must be** 1 to trigger the actions on the first time the event occured. Change only if you want to have a grace-period until the actions will be executed. For example giving the player a grace-period until his challenge will be reset.

### cooldown

Adjust to your needs to create the proper control challenge. Please refer to the blueprints documentation for further information.

### is_visible

Must be set to *false* once you have debugged your challenge (leaving this enabled will show it in the list of challenges and helping you to visually see what is happening).

### announce_progress

Should be set to *false*. Enable only if you want to notify the player in case of amount > 1.

### announce_completion

Should be set to *false*. Announcement will be handled via an action if you want to inform the user.

### data

Please refer to the blueprints documentation for further information. You can punish the player by sending some data to third-party plugins for breaking the rules.

### rules

Adjust to your needs to create the proper control challenge. Please refer to the blueprints documentation for further information.

### actions

All actions that should be executed. Will be executed from top to bottom. Read this documentation page completely to fully understand what a control challenge is and what actions you can use.


### dependencies

Adjust to your needs to create the proper control challenge. Please refer to the blueprints documentation for further information.

## Example actions for a control challenge

```yaml
  actions:
    - type: notify.player.progress.rule_broken
      values:
        - OnPlayerBlind.hard
    - type: challenge.delete.progress
      values:
        - OnPlayerBlind.hard
    - type: challenge.delete.completed
      values:
        - OnPlayerBlind.hard.control.noselfflash
```

This example shows how to reset the progress of the challenge OnPlayerBlind.hard in the file blind_them_by_the_light.json.

1. Notify User: The first action notifies the user about the broken rule and displays a message in the center of the screen and in the text chat. You need to specify the name(s) of the rule(s) that the user might have broken. The system checks if the user made any progress on these rules before displaying the message.

2. Reset Progress: The second action resets the progress only if the challenge is not yet completed. For example, if you have blinded 3 out of the 5 required enemies, the progress will be reset.

3. Reset Control Challenge: The third action resets the status of the control challenge, allowing it to be executed again. If the control challenge is marked as completed, it will not run anymore.
