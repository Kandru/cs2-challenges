# EventInstructorServerHintCreate (instructor_server_hint_create_hint_activator_userid / instructor_server_hint_create_userid)

CSS game event `instructor_server_hint_create` (`EventInstructorServerHintCreate`).

Who gets credit: the **event player(s)** listed under challenge types.

## Challenge types

Put one of these in your task `type:` field:

- `instructor_server_hint_create_hint_activator_userid`
- `instructor_server_hint_create_userid`

## Rule keys

> [!WARNING]
> If you use an unknown key, the task will not work because that condition can never be met.

| Key | Type |
|-----|------|
| [global event data](../rules/GlobalEventData.md) | see page |
| [player data](../rules/GlobalPlayerData.md) | prefixes `hint_activator_userid`, `userid` |
| `hint_activator_caption` | string |
| `hint_allow_nodraw_target` | bool |
| `hint_binding` | string |
| `hint_caption` | string |
| `hint_color` | string |
| `hint_entindex` | int |
| `hint_flags` | int |
| `hint_forcecaption` | bool |
| `hint_gamepad_binding` | string |
| `hint_icon_offscreen` | string |
| `hint_icon_offset` | float |
| `hint_icon_onscreen` | string |
| `hint_layoutfile` | string |
| `hint_local_player_only` | bool |
| `hint_name` | string |
| `hint_nooffscreen` | bool |
| `hint_range` | float |
| `hint_replace_key` | string |
| `hint_start_sound` | string |
| `hint_target` | int |
| `hint_timeout` | int |
| `hint_vr_height_offset` | float |
| `hint_vr_offset_x` | float |
| `hint_vr_offset_y` | float |
| `hint_vr_offset_z` | float |
| `hint_vr_panel_type` | int |
