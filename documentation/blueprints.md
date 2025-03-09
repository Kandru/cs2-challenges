# Blueprints Documentation

Blueprints are in-game challenges that players can see and complete. You can create many challenges, but be aware that having too many active challenges at once might slow down the server.

## Blueprints file

To create your first challenge, follow these steps:

1. Create a new file with a `.json` extension (e.g., `example.json`).
2. Place this file in the `blueprints` folder of the Challenges-Plugin.
3. You can create multiple files and organize them as you prefer (e.g., one file per blueprint type or plugin).

Each blueprint file must contain at least one challenge. A challenge is defined by an event type (see below) and various settings. Each challenge needs a unique name within the same blueprint file. The Challenges-Plugin will automatically generate an identifier for each challenge.

For example:
- Blueprint filename: `example.json`
- Unique Challenge name: `YourUniqueChallengeName`
- Resulting identifier: `example.YourUniqueChallengeName`

When the blueprint file is loaded into the Challenges-Plugin, it will be referenced by this identifier.

Each JSON file should be structured as follows and can contain multiple blueprints, each with a unique name:

```json
{
	"YourUniqueChallengeName": {
		"title": {
			"en": "My Unique Challenge ({count} / {total})"
		},
		"type": "player_jump",
		"amount": 10,
		"cooldown": 0,
		"is_visible": true,
		"is_rule": true,
		"announce_progress": true,
		"announce_completion": true,
		"data": {
			"ExamplePlugin": {
				"ExamplePoints": "1"
			}
		},
		"rules": [
			{
				"key": "global.isduringround",
				"operator": "bool==",
				"value": "true"
			}
		],
		"dependencies": []
	}
}
```

### title

The title is what the player sees in the upper right corner of the GUI by default or in the text chat when progress is made or the challenge is completed. You can use the placeholders *{count}* and *{total}* to show the current and required number of events needed to complete the challenge.

The default language is the player's language (changeable with !lang en/de/...). If that language is not available, it will fall back to the server's language (set this default in the CounterstrikeSharp settings json). If neither is available, the first entry in the list will be used.

### type

The type is the event that triggers this blueprint. In our example, we use the *player_jump* event. This means that every time a player jumps, this challenge will be activated.

### amount

The number of times this event must occur to complete the challenge. Choose a reasonable number based on the duration the blueprint will be active. For example, if the challenge lasts only 24 hours, the required amount should not exceed 1,000. Jumping 1,000 times in regular gameplay is difficult, and players should focus on playing rather than just jumping.

### cooldown

The cooldown is the time in seconds that must pass before the event can be counted again. For example, if you set the cooldown to 10 seconds, a player can only trigger the event once every 10 seconds. This can make the challenge more difficult by limiting how often the event can occur.

### is_visible

Whether this challenge should be visible to the player. If set to false, the challenge can still be completed, but the player will not see any notifications or progress updates.

### is_rule

A rule is a special type of challenge that can change other challenges. If a challenge is marked as a rule, its *title* will be shown to the player when it is activated and changes another challenge. Rules must have the *amount* set to 1, and both *announce_progress* and *announce_completion* should be set to false. More details about rules can be found in the rules section of this documentation.

### announce_progress

Whether you want to notify the player about their progress. This setting does not affect notifications sent to third-party plugins.

### announce_completion

Whether you want to notify the player when this challenge is completed. This setting does not affect notifications sent to third-party plugins.

### data

This section contains a dictionary of strings, where each string represents data to be sent to a third-party plugin. The Challenges-Plugin itself does not manage the actions taken when a player completes a challenge. You can include multiple plugins here. If your preferred plugin does not support the Challenges-Plugin, you can request the plugin developer to add support. We provide documentation and an example plugin to facilitate easy integration.

### rules

Rules make the Challenges-Plugin very powerful. Almost all events have parameters that you can compare against values you choose. In our example, we check if there is an active round. If not, we ignore this challenge until a round starts. You can also check for specific weapons, distances, teams, and more.

### dependencies

Dependencies are conditions that must be met before a challenge becomes available to the player. These conditions are other challenges that the player must complete first. This allows you to create a series of challenges with increasing difficulty.

To set a dependency, list the unique name of the required challenge. If the required challenge is in the same file, just use its unique name. For example, *YourUniqueChallengeName*. If the required challenge is in a different file, include the filename. For example, *example.YourUniqueChallengeName*.

## List of global rules

Global rules are applicable for almost all events.

- [Event Data](blueprints/GlobalEventData.md)
- [Player Data](blueprints/GlobalPlayerData.md)

## List of events

- [OnAchievementEarned](blueprints/OnAchievementEarned.md)
- [OnAddPlayerSonarIcon](blueprints/OnAddPlayerSonarIcon.md)
- [OnAmmoPickup](blueprints/OnAmmoPickup.md)
- [OnBombAbortdefuse](blueprints/OnBombAbortdefuse.md)
- [OnBombAbortplant](blueprints/OnBombAbortplant.md)
- [OnBombBegindefuse](blueprints/OnBombBegindefuse.md)
- [OnBombBeginplant](blueprints/OnBombBeginplant.md)
- [OnBombDefused](blueprints/OnBombDefused.md)
- [OnBombDropped](blueprints/OnBombDropped.md)
- [OnBombExploded](blueprints/OnBombExploded.md)
- [OnBombPickup](blueprints/OnBombPickup.md)
- [OnBombPlanted](blueprints/OnBombPlanted.md)
- [OnBotTakeover](blueprints/OnBotTakeover.md)
- [OnBreakBreakable](blueprints/OnBreakBreakable.md)
- [OnBreakProp](blueprints/OnBreakProp.md)
- [OnBulletDamage](blueprints/OnBulletDamage.md)
- [OnDefuserPickup](blueprints/OnDefuserPickup.md)
- [OnDoorClosed](blueprints/OnDoorClosed.md)
- [OnDoorOpen](blueprints/OnDoorOpen.md)
- [OnEnterBombzone](blueprints/OnEnterBombzone.md)
- [OnEnterBuyzone](blueprints/OnEnterBuyzone.md)
- [OnEnterRescuezone](blueprints/OnEnterRescuezone.md)
- [OnExitBombzone](blueprints/OnExitBombzone.md)
- [OnExitBuyzone](blueprints/OnExitBuyzone.md)
- [OnExitRescuezone](blueprints/OnExitRescuezone.md)
- [OnGrenadeBounce](blueprints/OnGrenadeBounce.md)
- [OnHostageFollows](blueprints/OnHostageFollows.md)
- [OnHostageHurt](blueprints/OnHostageHurt.md)
- [OnHostageKilled](blueprints/OnHostageKilled.md)
- [OnHostageRescued](blueprints/OnHostageRescued.md)
- [OnHostageRescuedAll](blueprints/OnHostageRescuedAll.md)
- [OnHostageStopsFollowing](blueprints/OnHostageStopsFollowing.md)
- [OnInspectWeapon](blueprints/OnInspectWeapon.md)
- [OnItemPickup](blueprints/OnItemPickup.md)
- [OnItemPurchase](blueprints/OnItemPurchase.md)
- [OnPlayerAvengedTeammate](blueprints/OnPlayerAvengedTeammate.md)
- [OnPlayerBlind](blueprints/OnPlayerBlind.md)
- [OnPlayerChangename](blueprints/OnPlayerChangename.md)
- [OnPlayerChat](blueprints/OnPlayerChat.md)
- [OnPlayerDeath](blueprints/OnPlayerDeath.md)
- [OnPlayerDecal](blueprints/OnPlayerDecal.md)
- [OnPlayerFalldamage](blueprints/OnPlayerFalldamage.md)
- [OnPlayerFootstep](blueprints/OnPlayerFootstep.md)
- [OnPlayerGivenC4](blueprints/OnPlayerGivenC4.md)
- [OnPlayerHurt](blueprints/OnPlayerHurt.md)
- [OnPlayerJump](blueprints/OnPlayerJump.md)
- [OnPlayerPing](blueprints/OnPlayerPing.md)
- [OnPlayerRadio](blueprints/OnPlayerRadio.md)
- [OnPlayerScore](blueprints/OnPlayerScore.md)
- [OnPlayerSound](blueprints/OnPlayerSound.md)
- [OnPlayerSpawned](blueprints/OnPlayerSpawned.md)
- [OnPlayerTeam](blueprints/OnPlayerTeam.md)
- [OnTeamScore](blueprints/OnTeamScore.md)
- [OnWeaponFireOnEmpty](blueprints/OnWeaponFireOnEmpty.md)
- [OnWeaponReload](blueprints/OnWeaponReload.md)
- [OnWeaponZoom](blueprints/OnWeaponZoom.md)
- [OnWeaponZoomRifle](blueprints/OnWeaponZoomRifle.md)
