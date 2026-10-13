# Use the challenge builder

The [challenge builder](https://kandru.github.io/cs2-challenges/builder/) is a visual editor that writes **one challenge YAML file**.

## Export and deploy

1. Open the [builder](https://kandru.github.io/cs2-challenges/builder/).
2. Set the challenge id (snake_case filename without `.yaml`), titles, and tasks.
3. Export or copy the YAML.
4. Save it as `…/Challenges/blueprints/<id>.yaml`.
5. Add that id under `challenges:` in [schedules.yaml](../howto/schedules.md).
6. Change map or run `challenges reload`.

Schedule dates are **UTC**. See [UTC schedule dates](../howto/schedules.md#dates).

## Help links in the builder

Each `?` in the builder opens the matching page in this documentation (challenge fields, rules, actions, event attributes, and so on).

## Tips

- Order tasks from first unlockable (top) to last.
- Put a hidden rule-breaker task directly below the streak it guards. See [Streak broken](../reference/actions.md#streak-broken).
- Prefer `contains` with short weapon names (`ak47`, `knife`, `awp`) when filtering by weapon.
