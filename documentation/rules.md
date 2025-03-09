# Rules Documentation

Rules make the Challenges-Plugin very powerful. Almost all events have parameters that you can compare against values you choose. In our example, we check if there is an active round. If not, we ignore this challenge until a round starts. You can also check for specific weapons, distances, teams, and more.

All rules are combined with AND, meaning every rule must be met for the challenge to count. Currently, there is no option to use OR.

## example rule

```
{
    "key": "global.isduringround",
    "operator": "bool==",
    "value": "true"
}
```

### key

The key can be found in almost every event and is the value which will be compared against your given value.

### operator

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

### value

The value you want to compare with the key. Make sure the value matches the type of the key (e.g., number, string, bool). Refer to the event documentation to determine the correct type. Depending on the type, only certain operators from the list above can be used.

## Advanced logic

Usually, the Challenges-Plugin does nothing when a player progresses or completes a challenge. However, there is one exception: you can make challenges harder by checking if a player is within the challenge's boundaries. For example, if you want to create a challenge where the player must get 10 headshots in a row, what happens if they kill someone without a headshot? The challenge needs to reset itself. This is where advanced logic comes in.

In the *data* section of the challenge, you can send data not only to a third-party plugin but also to the Challenges-Plugin itself. The Challenges-Plugin supports some commands.

```
"data": {
    "Challenges": {
        "delete_progress0": "headshots_in_a_row.OnPlayerKillHeadshot_hard",
        "delete_completed0": "headshots_in_a_row.rule_OnPlayerKillHeadshot_hard_donotdie"
    }
}
```

In this example when the rule challenge gets executed it actually deletes the progress on the *OnPlayerKillHeadshot_hard* challenge.

To be continued...