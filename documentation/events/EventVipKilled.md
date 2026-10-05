# EventVipKilled (vip_killed_attacker / vip_killed_userid)

CSS game event `vip_killed` (`EventVipKilled`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `vip_killed_attacker`
- `vip_killed_userid`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `attacker`, `userid` |
