# Blueprints Documentation

Blueprints are events that the player sees as a challenge ingame. You can create almost unlimited challenges but you should keep in mind that too many concurrent challenges may impact the server performance negatively.

## Blueprints file

To create your first challenge you need to create a *example.json* (call it what you want) and put this empty file into the *blueprints* folder of the Challenges-Plugin. You can create unlimited files and structure them to your liking (e.g. having one file per blueprint-type or per plugin).

Each json file needs to be structured as follows and can contain multiple blueprints (each with a unique name):

```json
{
	"YourUniqueChallengeName": {
		"title": {
			"en": "My Unique Challenge ({count} / {total})"
		},
		"type": "player_jump",
		"amount": 10,
		"cooldown": 0,
		"is_visible": false,
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
				"key": "isduringround",
				"operator": "bool==",
				"value": "true"
			}
		]
	}
}
```

### title

The title is what the player can see on the upper right in the GUI per default or in the text chat once progress was made or the achievement was completed. You can also include the parameters *{count}* and *{total}* to show the current and necessary amount of the events to happen until this challenge is completed.

The default language is the players language (changeable with !lang en/de/...). If that language is not available it will fall back to the servers language (change this default in the CounterstrikeSharp settings json). If this is also not available the first entry of the list will be chosen.

### type

The type is the event that needs to happen to trigger this blueprint. In our example we listen to the *player_jump* event and whenever a player jumps this blueprint will activate.

### amount

How many times this event needs to occure to finish it. Should be reasonable for the schedule you choose where this blueprint will be active for. E.g. if you have only 24 hours for this challenge this example should not exceed 1.000. To jump 1.000 times during regular gameplay is hard and people should play and not jump all the time.

### cooldown

Time in seconds before the event will be counted again. This way you can make it harder for a player (e.g. allow jumping only every 10 seconds).

### is_visible

Whether this challenge should be visible for the player. If disabled the challenge can still be completed as normal but without any visible interaction for the player.

### is_rule

Whether this challenge is a rule. Rules are specific challenges which allow for reset of other challenges. The *title* of a rule will be shown to the user if activated! Rules should always have set *amount* to 1 and *announce_progress* as well as *announce_completion* should be set to false. Rules will be explained the rules section of this documentation.

### announce_progress

Whether you want to announce the progress to the player. Doesn't have any effect on announcing the progress to third-party plugins.

### announce_completion

Whether you want to announce the completion of this event to the player. Doesn't have any effect on announcing the progress to third-party plugins.

### data

This dictionary of strings (yes, only strings) contains all data that should be given to a third-party plugin. The Challenges-Plugin itself does not handle what happens when a player finishes a challenge. You can add multiple plugins here. If your favorite plugin does not have support for this Challenges-Plugin feel free to ask the author to add support for it. We have documentation and an example plugin ready for easy integration.

### rules

Rules are what makes this Challenges-Plugin so powerful. Almost all events have parameters which can be used to compare them against a value of your liking. In our example we do check wether there is currently an active round. If not, we simply ignore this challenge until a round has started. But it doesn't stop there: you can check for specific weapons, distances, teams and many more.

The following operators can be used:

```
== (equal)
!= (not equal)
< (less than)
> (greater than)
<= (less or equal than)
>= (greater or equal than)
bool== (equal for bool values)
bool!= (not equal for bool values)
contains (if the value of the rule contains a specific keyword)`
```

## List of global rules

Global rules are applicable for almost all events.

- [Event Data](GlobalEventData.md)
- [Player Data](GlobalPlayerData.md)

## List of events

- [OnAchievementEarned](OnAchievementEarned.md)
- [OnAddPlayerSonarIcon](OnAddPlayerSonarIcon.md)
- [OnAmmoPickup](OnAmmoPickup.md)
- [OnBombAbortdefuse](OnBombAbortdefuse.md)
- [OnBombAbortplant](OnBombAbortplant.md)
- [OnBombBegindefuse](OnBombBegindefuse.md)
- [OnBombBeginplant](OnBombBeginplant.md)
- [OnBombDefused](OnBombDefused.md)
- [OnBombDropped](OnBombDropped.md)
- [OnBombExploded](OnBombExploded.md)
- [OnBombPickup](OnBombPickup.md)
- [OnBombPlanted](OnBombPlanted.md)
- [OnBotTakeover](OnBotTakeover.md)
- [OnBreakBreakable](OnBreakBreakable.md)
- [OnBreakProp](OnBreakProp.md)
- [OnBulletDamage](OnBulletDamage.md)
- [OnDefuserPickup](OnDefuserPickup.md)
- [OnDoorClosed](OnDoorClosed.md)
- [OnDoorOpen](OnDoorOpen.md)
- [OnEnterBombzone](OnEnterBombzone.md)
- [OnEnterBuyzone](OnEnterBuyzone.md)
- [OnEnterRescuezone](OnEnterRescuezone.md)
- [OnExitBombzone](OnExitBombzone.md)
- [OnExitBuyzone](OnExitBuyzone.md)
- [OnExitRescuezone](OnExitRescuezone.md)
- [OnGrenadeBounce](OnGrenadeBounce.md)
- [OnHostageFollows](OnHostageFollows.md)
- [OnHostageHurt](OnHostageHurt.md)
- [OnHostageKilled](OnHostageKilled.md)
- [OnHostageRescued](OnHostageRescued.md)
- [OnHostageRescuedAll](OnHostageRescuedAll.md)
- [OnHostageStopsFollowing](OnHostageStopsFollowing.md)
- [OnInspectWeapon](OnInspectWeapon.md)
- [OnItemPickup](OnItemPickup.md)
- [OnItemPurchase](OnItemPurchase.md)
- [OnPlayerAvengedTeammate](OnPlayerAvengedTeammate.md)
- [OnPlayerBlind](OnPlayerBlind.md)
- [OnPlayerChangename](OnPlayerChangename.md)
- [OnPlayerChat](OnPlayerChat.md)
- [OnPlayerDeath](OnPlayerDeath.md)
- [OnPlayerDecal](OnPlayerDecal.md)
- [OnPlayerFalldamage](OnPlayerFalldamage.md)
- [OnPlayerFootstep](OnPlayerFootstep.md)
- [OnPlayerGivenC4](OnPlayerGivenC4.md)
- [OnPlayerHurt](OnPlayerHurt.md)
- [OnPlayerJump](OnPlayerJump.md)
- [OnPlayerPing](OnPlayerPing.md)
- [OnPlayerRadio](OnPlayerRadio.md)
- [OnPlayerScore](OnPlayerScore.md)
- [OnPlayerSound](OnPlayerSound.md)
- [OnPlayerSpawned](OnPlayerSpawned.md)
- [OnPlayerTeam](OnPlayerTeam.md)
- [OnTeamScore](OnTeamScore.md)
- [OnWeaponFireOnEmpty](OnWeaponFireOnEmpty.md)
- [OnWeaponReload](OnWeaponReload.md)
- [OnWeaponZoom](OnWeaponZoom.md)
- [OnWeaponZoomRifle](OnWeaponZoomRifle.md)
