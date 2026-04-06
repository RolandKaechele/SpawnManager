using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SpawnManager.Runtime
{
    /// <summary>
    /// <b>SpawnManager</b> handles spawning and despawning of GameObjects using a pool,
    /// named spawn points, and JSON-authored spawn definitions.
    ///
    /// <para><b>Responsibilities:</b>
    /// <list type="number">
    ///   <item>Maintain an object pool per prefab id to avoid instantiation overhead.</item>
    ///   <item>Register named spawn points (via <see cref="SpawnPoint"/> components or API).</item>
    ///   <item>Load <see cref="SpawnDefinition"/> entries from the Inspector and optional JSON file.</item>
    ///   <item>Execute spawns — single, batched, or wave-style with intervals.</item>
    ///   <item>Track live instances by unique instance id.</item>
    ///   <item>Pause and resume spawning (e.g. during cutscenes or loading).</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Modding / JSON:</b> Enable <c>loadFromJson</c> and place a
    /// <c>spawns.json</c> in <c>StreamingAssets/</c>.
    /// JSON entries are <b>merged by id</b>: JSON overrides Inspector entries.</para>
    ///
    /// <para><b>Optional integration defines:</b>
    /// <list type="bullet">
    ///   <item><c>SPAWNMANAGER_EEM</c>  — EnemyManager: delegate enemy-type spawns to EnemyManager for stat/AI wiring.</item>
    ///   <item><c>SPAWNMANAGER_AIM</c>  — AiManager: register each spawned agent with the global AI registry.</item>
    ///   <item><c>SPAWNMANAGER_EM</c>   — EventManager: fire <c>spawn.spawned</c>, <c>spawn.despawned</c>, <c>spawn.wave.started</c> events.</item>
    ///   <item><c>SPAWNMANAGER_CSM</c>  — CutsceneManager: pause spawning while a cutscene is playing.</item>
    ///   <item><c>SPAWNMANAGER_STM</c>  — StateManager: pause spawning during non-Gameplay states.</item>
    ///   <item><c>SPAWNMANAGER_MLF</c>  — MapLoaderFramework: clear all live instances on chapter change.</item>
    ///   <item><c>SPAWNMANAGER_DOTWEEN</c> — DOTween Pro: scale/fade tween on spawn and despawn.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("SpawnManager/Spawn Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class SpawnManager : SerializedMonoBehaviour
#else
    public class SpawnManager : MonoBehaviour
#endif
    {
        // ─── Inspector ───────────────────────────────────────────────────────────

        [Header("Definitions")]
        [Tooltip("Spawn definitions. JSON entries are merged on top by id.")]
        [SerializeField] private List<SpawnDefinition> definitions = new List<SpawnDefinition>();

        [Header("Pool Settings")]
        [Tooltip("Initial pool size per prefab. Grows automatically if needed.")]
        [SerializeField] private int initialPoolSize = 8;

        [Tooltip("Parent transform for pooled objects. Leave null to use this GameObject.")]
        [SerializeField] private Transform poolParent;

        [Header("Modding / JSON")]
        [Tooltip("Merge spawn definitions from StreamingAssets/<jsonPath> at startup.")]
        [SerializeField] private bool loadFromJson = false;

        [Tooltip("Path relative to StreamingAssets/ (e.g. 'spawns.json').")]
        [SerializeField] private string jsonPath = "spawns.json";

        [Header("Debug")]
        [Tooltip("Log all spawn and despawn events to the Unity Console.")]
        [SerializeField] private bool verboseLogging = false;

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired when an object is spawned. Parameters: definitionId, instanceId, gameObject.</summary>
        public event Action<string, string, GameObject> OnSpawned;

        /// <summary>Fired when an object is despawned. Parameters: definitionId, instanceId.</summary>
        public event Action<string, string> OnDespawned;

        /// <summary>Fired when a batch/wave finishes all spawns for a definition. Parameter: definitionId.</summary>
        public event Action<string> OnWaveCompleted;

        // ─── Delegate hooks ──────────────────────────────────────────────────────

        /// <summary>
        /// Optional callback invoked for each spawned object so that bridge components
        /// (e.g. AiManager, EnemyManager) can register or configure it.
        /// Signature: (definitionId, instanceId, gameObject).
        /// </summary>
        public Action<string, string, GameObject> OnSpawnedCallback;

        /// <summary>
        /// Optional callback invoked just before an object is returned to the pool.
        /// Signature: (definitionId, instanceId, gameObject).
        /// </summary>
        public Action<string, string, GameObject> OnDespawnedCallback;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, SpawnDefinition> _defIndex =
            new Dictionary<string, SpawnDefinition>(StringComparer.OrdinalIgnoreCase);

        // Pool: prefabId → Queue of inactive GameObjects
        private readonly Dictionary<string, Queue<GameObject>> _pool =
            new Dictionary<string, Queue<GameObject>>(StringComparer.OrdinalIgnoreCase);

        // Pool: prefabId → loaded prefab
        private readonly Dictionary<string, GameObject> _prefabCache =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        // Live instances
        private readonly Dictionary<string, SpawnInstanceRecord> _live =
            new Dictionary<string, SpawnInstanceRecord>(StringComparer.OrdinalIgnoreCase);

        // Spawn-point registry
        private readonly Dictionary<string, SpawnPointData> _spawnPoints =
            new Dictionary<string, SpawnPointData>(StringComparer.OrdinalIgnoreCase);

        // Cooldown tracking: definitionId → Time.time of last spawn
        private readonly Dictionary<string, float> _cooldowns =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private bool _paused;
        private int  _instanceCounter;

        /// <summary>True while spawning is paused.</summary>
        public bool IsPaused => _paused;

        /// <summary>Number of currently live instances.</summary>
        public int LiveCount => _live.Count;

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (poolParent == null) poolParent = transform;
            BuildIndex();
            if (loadFromJson) LoadJsonDefinitions();
        }

        private void Start()
        {
            WarmUpPool();
            StartAutoTriggers();
        }

        // ─── Spawn point registration ─────────────────────────────────────────────

        /// <summary>Register a spawn point. Called automatically by <see cref="SpawnPoint"/> on Awake.</summary>
        public void RegisterSpawnPoint(SpawnPointData point)
        {
            if (point == null || string.IsNullOrEmpty(point.id)) return;
            _spawnPoints[point.id] = point;
        }

        /// <summary>Unregister a spawn point by id.</summary>
        public void UnregisterSpawnPoint(string id) => _spawnPoints.Remove(id);

        /// <summary>Return a registered spawn point, or null.</summary>
        public SpawnPointData GetSpawnPoint(string id) =>
            _spawnPoints.TryGetValue(id, out var p) ? p : null;

        // ─── Spawn API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawn objects according to the definition with the given id.
        /// Respects cooldown, maxLive, paused state, and count settings.
        /// </summary>
        public void Spawn(string definitionId)
        {
            if (_paused) { if (verboseLogging) Debug.Log($"[SpawnManager] Spawn paused — skipping '{definitionId}'."); return; }

            if (!_defIndex.TryGetValue(definitionId, out var def))
            {
                Debug.LogWarning($"[SpawnManager] No spawn definition '{definitionId}'.");
                return;
            }

            if (IsOnCooldown(def)) return;

            StartCoroutine(DoSpawnBatch(def));
        }

        /// <summary>
        /// Immediately spawn a single instance of the given prefab id at the given position,
        /// without requiring a <see cref="SpawnDefinition"/>.
        /// </summary>
        public GameObject SpawnAt(string prefabId, Vector3 position, Quaternion rotation = default)
        {
            var go = Acquire(prefabId);
            if (go == null) return null;
            go.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            go.SetActive(true);

            string instanceId = GenerateInstanceId(prefabId);
            TrackInstance(new SpawnInstanceRecord
            {
                instanceId   = instanceId,
                definitionId = prefabId,
                gameObject   = go,
                spawnTime    = Time.time
            });
            return go;
        }

        /// <summary>
        /// Return an object to the pool (despawn). Pass the instance or its instance id.
        /// </summary>
        public void Despawn(GameObject go)
        {
            if (go == null) return;
            SpawnInstanceRecord record = FindRecord(go);
            if (record != null)
            {
                OnDespawnedCallback?.Invoke(record.definitionId, record.instanceId, go);
                OnDespawned?.Invoke(record.definitionId, record.instanceId);
                _live.Remove(record.instanceId);
                if (verboseLogging)
                    Debug.Log($"[SpawnManager] Despawned instance '{record.instanceId}'.");
            }
            ReturnToPool(go);
        }

        /// <summary>Despawn a live instance by its instance id.</summary>
        public void DespawnById(string instanceId)
        {
            if (_live.TryGetValue(instanceId, out var record))
                Despawn(record.gameObject);
        }

        /// <summary>Despawn all live instances for a given definition id.</summary>
        public void DespawnAll(string definitionId)
        {
            var toRemove = new List<string>();
            foreach (var kv in _live)
                if (string.Equals(kv.Value.definitionId, definitionId, StringComparison.OrdinalIgnoreCase))
                    toRemove.Add(kv.Key);
            foreach (var id in toRemove)
                DespawnById(id);
        }

        /// <summary>Despawn all live instances.</summary>
        public void DespawnAll()
        {
            var instanceIds = new List<string>(_live.Keys);
            foreach (var id in instanceIds)
                DespawnById(id);
        }

        // ─── Pause / Resume ───────────────────────────────────────────────────────

        /// <summary>Pause all automatic spawning. In-progress batches are not interrupted.</summary>
        public void PauseSpawning()
        {
            _paused = true;
            if (verboseLogging) Debug.Log("[SpawnManager] Spawning paused.");
        }

        /// <summary>Resume automatic spawning.</summary>
        public void ResumeSpawning()
        {
            _paused = false;
            if (verboseLogging) Debug.Log("[SpawnManager] Spawning resumed.");
        }

        // ─── Query API ────────────────────────────────────────────────────────────

        /// <summary>Return all <see cref="SpawnInstanceRecord"/>s for a given definition id.</summary>
        public IEnumerable<SpawnInstanceRecord> GetLiveInstances(string definitionId)
        {
            foreach (var r in _live.Values)
                if (string.Equals(r.definitionId, definitionId, StringComparison.OrdinalIgnoreCase))
                    yield return r;
        }

        /// <summary>Number of live instances for a given definition id.</summary>
        public int GetLiveCount(string definitionId)
        {
            int count = 0;
            foreach (var r in _live.Values)
                if (string.Equals(r.definitionId, definitionId, StringComparison.OrdinalIgnoreCase))
                    count++;
            return count;
        }

        /// <summary>Return a <see cref="SpawnDefinition"/> by id, or null.</summary>
        public SpawnDefinition GetDefinition(string id) =>
            _defIndex.TryGetValue(id, out var d) ? d : null;

        /// <summary>All registered definition ids.</summary>
        public IEnumerable<string> GetAllDefinitionIds() => _defIndex.Keys;

        // ─── Internal coroutines ─────────────────────────────────────────────────

        private IEnumerator DoSpawnBatch(SpawnDefinition def)
        {
            int remaining = def.count;
            int spawned   = 0;

            _cooldowns[def.id] = Time.time;

            while (remaining > 0)
            {
                if (_paused) { yield return null; continue; }

                int liveCount = GetLiveCount(def.id);
                if (def.maxLive > 0 && liveCount >= def.maxLive) break;

                var go = Acquire(def.prefabId, def.prefabResource);
                if (go == null) break;

                var point = ResolveSpawnPoint(def);
                var pos   = point.position + UnityEngine.Random.insideUnitSphere * def.spawnRadius;
                pos.y     = point.position.y; // keep on ground plane for 2D/2.5D
                go.transform.SetPositionAndRotation(pos, point.rotation);
                if (!string.IsNullOrEmpty(def.spawnTag)) go.tag = def.spawnTag;
                go.SetActive(true);

                string instanceId = GenerateInstanceId(def.id);
                var record = new SpawnInstanceRecord
                {
                    instanceId   = instanceId,
                    definitionId = def.id,
                    gameObject   = go,
                    spawnTime    = Time.time
                };
                TrackInstance(record);

                OnSpawnedCallback?.Invoke(def.id, instanceId, go);
                OnSpawned?.Invoke(def.id, instanceId, go);

                if (verboseLogging)
                    Debug.Log($"[SpawnManager] Spawned '{def.id}' instance '{instanceId}'.");

                remaining--;
                spawned++;

                if (def.spawnInterval > 0f && remaining > 0)
                    yield return new WaitForSeconds(def.spawnInterval);
            }

            if (spawned > 0) OnWaveCompleted?.Invoke(def.id);
        }

        private IEnumerator RunTimer(SpawnDefinition def)
        {
            while (true)
            {
                yield return new WaitForSeconds(def.timerInterval);
                if (!_paused) Spawn(def.id);
            }
        }

        // ─── Pool helpers ─────────────────────────────────────────────────────────

        private GameObject Acquire(string prefabId, string prefabResource = null)
        {
            if (!_pool.TryGetValue(prefabId, out var queue))
            {
                queue = new Queue<GameObject>();
                _pool[prefabId] = queue;
            }

            if (queue.Count > 0)
                return queue.Dequeue();

            return InstantiateNew(prefabId, prefabResource);
        }

        private void ReturnToPool(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(poolParent);

            // Determine prefabId from the object name (strips clone suffix)
            string key = go.name.Replace("(Clone)", "").Trim();
            if (!_pool.TryGetValue(key, out var queue))
            {
                queue = new Queue<GameObject>();
                _pool[key] = queue;
            }
            queue.Enqueue(go);
        }

        private GameObject InstantiateNew(string prefabId, string prefabResource = null)
        {
            if (!_prefabCache.TryGetValue(prefabId, out var prefab))
            {
                string resource = !string.IsNullOrEmpty(prefabResource) ? prefabResource : prefabId;
                prefab = Resources.Load<GameObject>(resource);
                if (prefab == null)
                {
                    Debug.LogWarning($"[SpawnManager] Prefab not found: '{resource}'.");
                    return null;
                }
                _prefabCache[prefabId] = prefab;
            }

            var go = Instantiate(prefab, poolParent);
            go.SetActive(false);
            return go;
        }

        private void WarmUpPool()
        {
            foreach (var def in _defIndex.Values)
            {
                if (string.IsNullOrEmpty(def.prefabId)) continue;
                if (!_pool.ContainsKey(def.prefabId))
                    _pool[def.prefabId] = new Queue<GameObject>();

                for (int i = 0; i < initialPoolSize; i++)
                {
                    var go = InstantiateNew(def.prefabId, def.prefabResource);
                    if (go != null) _pool[def.prefabId].Enqueue(go);
                }
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private void StartAutoTriggers()
        {
            foreach (var def in _defIndex.Values)
            {
                if (def.triggerType == SpawnTriggerType.OnStart)
                    Spawn(def.id);
                else if (def.triggerType == SpawnTriggerType.Timer)
                    StartCoroutine(RunTimer(def));
            }
        }

        private bool IsOnCooldown(SpawnDefinition def)
        {
            if (def.cooldown <= 0f) return false;
            if (_cooldowns.TryGetValue(def.id, out float last))
                return Time.time - last < def.cooldown;
            return false;
        }

        private SpawnPointData ResolveSpawnPoint(SpawnDefinition def)
        {
            if (!string.IsNullOrEmpty(def.spawnPointId) &&
                _spawnPoints.TryGetValue(def.spawnPointId, out var pt))
                return pt;

            return new SpawnPointData
            {
                id       = "__default",
                position = transform.position,
                rotation = transform.rotation
            };
        }

        private void TrackInstance(SpawnInstanceRecord record) => _live[record.instanceId] = record;

        private SpawnInstanceRecord FindRecord(GameObject go)
        {
            foreach (var r in _live.Values)
                if (r.gameObject == go) return r;
            return null;
        }

        private string GenerateInstanceId(string definitionId) =>
            $"{definitionId}_{++_instanceCounter}";

        private void BuildIndex()
        {
            _defIndex.Clear();
            foreach (var def in definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                _defIndex[def.id] = def;
            }
        }

        private void LoadJsonDefinitions()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, jsonPath);
            if (!File.Exists(fullPath)) return;
            try
            {
                string json = File.ReadAllText(fullPath);
                var root = JsonUtility.FromJson<SpawnDefinitionList>(json);
                if (root?.spawns == null) return;
                foreach (var def in root.spawns)
                {
                    if (def == null || string.IsNullOrEmpty(def.id)) continue;
                    def.rawJson = json;
                    _defIndex[def.id] = def;
                    if (verboseLogging) Debug.Log($"[SpawnManager] JSON definition '{def.id}' loaded.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpawnManager] Failed to load JSON '{fullPath}': {e.Message}");
            }
        }

        // ─── JSON wrapper ─────────────────────────────────────────────────────────

        [Serializable]
        private class SpawnDefinitionList
        {
            public List<SpawnDefinition> spawns;
        }
    }
}
