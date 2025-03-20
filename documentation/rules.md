> [!TIP]
> Rules can be complex to understand. Please look into the *examples* folder of this repository before asking questions in our Discord.

# Rules Documentation

Rules make the Challenges-Plugin very powerful. Almost all events have parameters that you can compare against values you choose. In our example, we check if there is an active round. If not, we ignore this challenge until a round starts. You can also check for specific weapons, distances, teams, and more.

All rules are combined with AND, meaning every rule must be met for the challenge to count. Currently, there is no option to use OR.

## example rule(s)

```yaml
- key: global.iswarmup
    operator: bool==
    value: "false"
- key: global.isduringround
    operator: bool==
    value: "true"
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
!contains (if the value of the rule does not contain a specific keyword)`
```

### value

The value you want to compare with the key. Make sure the value matches the type of the key (e.g., number, string, bool). Refer to the event documentation to determine the correct type. Depending on the type, only certain operators from the list above can be used.

## List of global rules

Global rules are applicable for almost all events.

- [Event Data](rules/GlobalEventData.md)
- [Player Data](rules/GlobalPlayerData.md)
