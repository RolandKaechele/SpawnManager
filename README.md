# SpawnManager

Handles spawning and despawning of game objects with per-prefab object pooling, named spawn points, batch spawning, wave support, and JSON-driven mod-friendly definitions.

## Features

- Named `SpawnDefinition` assets configured in the Inspector or loaded from `StreamingAssets/spawns/`
- Per-prefab **object pools** — reuses instances instead of instantiating/destroying
- World-space **SpawnPoint** components auto-register themselves at runtime
- Trigger types: `Manual`, `OnStart`, `OnTriggerEnter`, `Timer`
- `PauseSpawning()` / `ResumeSpawning()` for cutscenes and loading screens
- `OnSpawnedCallback` and `OnDespawnedCallback` delegate hooks
- Full Editor window with live instance counts, pause/resume and per-definition Spawn buttons
- JSON modding support via `StreamingAssets/spawns/` (entries merged by `id`)

## Optional Integrations

| Feature | Define Symbol |
| --- | --- |
| DOTween Pro (scale-punch spawn/despawn) | `SPAWNMANAGER_DOTWEEN` |
| EnemyManager auto-registration | `SPAWNMANAGER_EEM` |
| AiManager agent registration | `SPAWNMANAGER_AIM` |
| EventManager events | `SPAWNMANAGER_EM` |
| StateManager pause (Cutscene/Loading/Dialogue) | `SPAWNMANAGER_STM` |
| MapLoaderFramework auto-despawn/spawn on chapter load | `SPAWNMANAGER_MLF` |
| CutsceneManager pause during cutscenes | `SPAWNMANAGER_CSM` |

## EventManager Events

When `SPAWNMANAGER_EM` is active the following events are fired:

| Event Key | Payload |
| --- | --- |
| `spawn.spawned` | `SpawnInstanceRecord` |
| `spawn.despawned` | `SpawnInstanceRecord` |
| `spawn.wave.completed` | `definitionId` (string) |

## JSON Modding

Place one or more `.json` files in `StreamingAssets/spawns/` to override or extend spawn definitions at runtime without recompiling.
All `*.json` files in the folder are loaded and merged by `id` at startup.
Each file contains exactly one spawn entry and is named by spawn ID (e.g., `pickup_medikit.json`, `pickup_ammo.json`).

**Example:** `StreamingAssets/spawns/green_spider_swarm.json`

```json
{
  "spawns": [
    {
      "id": "green_spider_swarm",
      "count": 8,
      "timerInterval": 20.0
    }
  ]
}
```

## Quick Start

1. Add a `SpawnManager` component to a persistent GameObject.
2. Add `SpawnPoint` components to world-space GameObjects and assign `pointId`.
3. Define `SpawnDefinition` entries in the Inspector, referencing prefab resource paths.
4. Call `SpawnManager.Instance.Spawn("green_spider_swarm")` from code or the Editor window.


## Editor Tools

Open via **JSON Editors → Spawn Manager** in the Unity menu bar, or via the **Open JSON Editor** button in the SpawnManager Inspector.

| Action | Result |
| ------ | ------ |
| **Load** | Reads all `*.json` from `StreamingAssets/spawns/`; creates the folder if missing |
| **Edit** | Add / remove / reorder entries using the Inspector list |
| **Save** | Writes each entry as `<id>.json` to `StreamingAssets/spawns/`; entries without an `id` are skipped. Calls `AssetDatabase.Refresh()` |

With **ODIN_INSPECTOR** active, the list uses Odin's enhanced drawer (drag-to-sort, collapsible entries).


## License

MIT
