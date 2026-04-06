#if SPAWNMANAGER_AIM
using AiManager.Runtime;
using UnityEngine;

namespace SpawnManager.Runtime
{
    /// <summary>
    /// <b>AiManagerBridge</b> connects SpawnManager to AiManager.
    /// <para>
    /// When <c>SPAWNMANAGER_AIM</c> is defined:
    /// <list type="bullet">
    ///   <item>Registers each spawned object as an AI agent with AiManager on spawn.</item>
    ///   <item>Unregisters the agent on despawn.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SpawnManager/AiManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class AiManagerBridge : UnityEngine.MonoBehaviour
    {
        private SpawnManager _spawnManager;
        private AiManager    _aiManager;

        private void Awake()
        {
            _spawnManager = GetComponent<SpawnManager>() ?? FindFirstObjectByType<SpawnManager>();
            _aiManager    = GetComponent<AiManager>()    ?? FindFirstObjectByType<AiManager>();

            if (_spawnManager == null) Debug.LogWarning("[SpawnManager/AiManagerBridge] SpawnManager not found.");
            if (_aiManager    == null) Debug.LogWarning("[SpawnManager/AiManagerBridge] AiManager not found.");
        }

        private void OnEnable()
        {
            if (_spawnManager == null) return;
            _spawnManager.OnSpawnedCallback   += HandleSpawned;
            _spawnManager.OnDespawnedCallback += HandleDespawned;
        }

        private void OnDisable()
        {
            if (_spawnManager == null) return;
            _spawnManager.OnSpawnedCallback   -= HandleSpawned;
            _spawnManager.OnDespawnedCallback -= HandleDespawned;
        }

        private void HandleSpawned(string defId, string instanceId, UnityEngine.GameObject go)
            => _aiManager?.RegisterAgent(instanceId, go);

        private void HandleDespawned(string defId, string instanceId, UnityEngine.GameObject go)
            => _aiManager?.DeregisterAgent(instanceId);
    }
}
#endif
