using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpawnManager.Runtime
{
    // -------------------------------------------------------------------------
    // SpawnTriggerType
    // -------------------------------------------------------------------------

    /// <summary>When a spawn definition is automatically triggered.</summary>
    public enum SpawnTriggerType
    {
        /// <summary>Never triggered automatically — use the API only.</summary>
        Manual,
        /// <summary>Triggered when the scene / level starts.</summary>
        OnStart,
        /// <summary>Triggered when the player enters a trigger volume.</summary>
        OnTriggerEnter,
        /// <summary>Triggered on a recurring timer.</summary>
        Timer
    }

    // -------------------------------------------------------------------------
    // SpawnDefinition
    // -------------------------------------------------------------------------

    /// <summary>
    /// Defines a single spawn entry: what to spawn, where, how many, and under what conditions.
    /// Serializable so it can be authored in the Inspector and loaded from JSON.
    /// </summary>
    [Serializable]
    public class SpawnDefinition
    {
        /// <summary>Unique identifier (e.g. "spider_wave_01").</summary>
        public string id;

        /// <summary>Human-readable label shown in Editor UI.</summary>
        public string label;

        /// <summary>Id of the prefab in the object-pool registry (key used to look up the prefab to instantiate).</summary>
        public string prefabId;

        /// <summary>
        /// Resources-relative path to the prefab (used when <see cref="prefabId"/> is not found in the pool registry).
        /// </summary>
        public string prefabResource;

        /// <summary>Spawn-point tag or id. If empty, uses transform.position of the SpawnManager.</summary>
        public string spawnPointId;

        /// <summary>How many objects to spawn per trigger.</summary>
        public int count = 1;

        /// <summary>Maximum number of live instances for this definition at one time (0 = unlimited).</summary>
        public int maxLive = 0;

        /// <summary>Delay in seconds between individual spawns within a batch.</summary>
        public float spawnInterval = 0f;

        /// <summary>Minimum cooldown in seconds before this definition can trigger again.</summary>
        public float cooldown = 0f;

        /// <summary>Trigger condition for automatic spawning.</summary>
        public SpawnTriggerType triggerType = SpawnTriggerType.Manual;

        /// <summary>
        /// For <see cref="SpawnTriggerType.Timer"/>: interval in seconds between batches.
        /// </summary>
        public float timerInterval = 10f;

        /// <summary>Radius around the spawn point within which each object is randomly offset.</summary>
        public float spawnRadius = 0f;

        /// <summary>Whether spawned objects are managed by the pool and returned on despawn.</summary>
        public bool usePool = true;

        /// <summary>Optional tag applied to spawned GameObjects.</summary>
        public string spawnTag;

        /// <summary>Raw JSON stored during deserialisation (non-serialised).</summary>
        [NonSerialized] public string rawJson;
    }

    // -------------------------------------------------------------------------
    // SpawnPoint
    // -------------------------------------------------------------------------

    /// <summary>
    /// A named world-space spawn location registered with SpawnManager.
    /// Attach to any GameObject in the scene.
    /// </summary>
    [Serializable]
    public class SpawnPointData
    {
        /// <summary>Unique id of this spawn point.</summary>
        public string id;

        /// <summary>World position.</summary>
        public Vector3 position;

        /// <summary>World rotation.</summary>
        public Quaternion rotation;

        /// <summary>Optional tag for filtering.</summary>
        public string tag;
    }

    // -------------------------------------------------------------------------
    // SpawnInstanceRecord
    // -------------------------------------------------------------------------

    /// <summary>Tracks a live spawned instance.</summary>
    public class SpawnInstanceRecord
    {
        /// <summary>Unique instance id assigned at spawn time.</summary>
        public string instanceId;

        /// <summary>Definition id that produced this instance.</summary>
        public string definitionId;

        /// <summary>The spawned GameObject.</summary>
        public GameObject gameObject;

        /// <summary>Time (Time.time) when this instance was spawned.</summary>
        public float spawnTime;
    }
}
