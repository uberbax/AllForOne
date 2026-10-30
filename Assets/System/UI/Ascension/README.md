# Hero Ascension

Prefab: Resources/UI_HeroAscension.prefab. Place below any ObjHolder containing a hero; GoAscend reads the nearest parent holder and operates on the owned inventory hero.

The badge shows the ascend parameter (initially 0); the fraction shows current shard_ID quantity / cost of the next ascension. Click the strip to ascend. Cost for target levels 1-10 is 5 shards, 11-20 is 10, 21-30 is 15, and increases by 5 every 10 target levels. Normal hero level is independent.

Ascension is stored in the hero's existing upgradePars["ascend"]. All matching shard stacks are counted and consumed, including the standard shard definition plus shardID format. No hero ownership or insufficient shards prevents spending. Saves use MainStates.Save when useSave is enabled.

The prefab is nested under each HeroCard in UI_Pub, directly above mileage tokens. Optional notOwnedVisual displays the Pub NEW badge for an unowned hero. The controller refreshes on enable, global UI refresh, and every 0.25 seconds while active.
