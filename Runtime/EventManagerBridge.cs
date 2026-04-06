#if SPAWNMANAGER_EM
using EventManager.Runtime;
using UnityEngine;

namespace SpawnManager.Runtime
{
    /// <summary>
    /// <b>EventManagerBridge</b> connects SpawnManager to EventManager.
    /// <para>
    /// When <c>SPAWNMANAGER_EM</c> is defined, fires:
    /// <list type="bullet">
    ///   <item><c>spawn.spawned</c> — payload: instanceId</item>
    ///   <item><c>spawn.despawned</c> — payload: instanceId</item>
    ///   <item><c>spawn.wave.completed</c> — payload: definitionId</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SpawnManager/EventManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class EventManagerBridge : UnityEngine.MonoBehaviour
    {
        private SpawnManager _spawnManager;
        private EventManager _eventManager;

        private void Awake()
        {
            _spawnManager = GetComponent<SpawnManager>() ?? FindFirstObjectByType<SpawnManager>();
            _eventManager = GetComponent<EventManager>() ?? FindFirstObjectByType<EventManager>();

            if (_spawnManager == null) Debug.LogWarning("[SpawnManager/EventManagerBridge] SpawnManager not found.");
            if (_eventManager == null) Debug.LogWarning("[SpawnManager/EventManagerBridge] EventManager not found.");
        }

        private void OnEnable()
        {
            if (_spawnManager == null) return;
            _spawnManager.OnSpawned       += HandleSpawned;
            _spawnManager.OnDespawned     += HandleDespawned;
            _spawnManager.OnWaveCompleted += HandleWaveCompleted;
        }

        private void OnDisable()
        {
            if (_spawnManager == null) return;
            _spawnManager.OnSpawned       -= HandleSpawned;
            _spawnManager.OnDespawned     -= HandleDespawned;
            _spawnManager.OnWaveCompleted -= HandleWaveCompleted;
        }

        private void HandleSpawned(string defId, string instanceId, UnityEngine.GameObject go)
            => _eventManager?.FireEvent("spawn.spawned", instanceId);

        private void HandleDespawned(string defId, string instanceId)
            => _eventManager?.FireEvent("spawn.despawned", instanceId);

        private void HandleWaveCompleted(string defId)
            => _eventManager?.FireEvent("spawn.wave.completed", defId);
    }
}
#endif
