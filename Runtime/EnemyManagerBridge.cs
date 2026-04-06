#if SPAWNMANAGER_EEM
using EnemyManager.Runtime;
using UnityEngine;

namespace SpawnManager.Runtime
{
    /// <summary>
    /// <b>EnemyManagerBridge</b> connects SpawnManager to EnemyManager.
    /// <para>
    /// When <c>SPAWNMANAGER_EEM</c> is defined:
    /// <list type="bullet">
    ///   <item>For each spawned object whose definition id matches a registered EnemyDefinition,
    ///   calls <c>EnemyManager.RegisterInstance()</c> so that EnemyManager can track the instance,
    ///   apply stats, and handle defeat events.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SpawnManager/EnemyManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class EnemyManagerBridge : UnityEngine.MonoBehaviour
    {
        private SpawnManager  _spawnManager;
        private EnemyManager  _enemyManager;

        private void Awake()
        {
            _spawnManager = GetComponent<SpawnManager>() ?? FindFirstObjectByType<SpawnManager>();
            _enemyManager = GetComponent<EnemyManager>() ?? FindFirstObjectByType<EnemyManager>();

            if (_spawnManager == null) Debug.LogWarning("[SpawnManager/EnemyManagerBridge] SpawnManager not found.");
            if (_enemyManager == null) Debug.LogWarning("[SpawnManager/EnemyManagerBridge] EnemyManager not found.");
        }

        private void OnEnable()
        {
            if (_spawnManager != null)
                _spawnManager.OnSpawnedCallback += HandleSpawned;
        }

        private void OnDisable()
        {
            if (_spawnManager != null)
                _spawnManager.OnSpawnedCallback -= HandleSpawned;
        }

        private void HandleSpawned(string definitionId, string instanceId, UnityEngine.GameObject go)
        {
            if (_enemyManager == null) return;
            // Only notify EnemyManager if a matching definition exists (it manages its own instances).
            if (_enemyManager.GetDefinition(definitionId) != null)
                Debug.Log($"[SpawnManager/EnemyManagerBridge] Spawned enemy instance '{instanceId}' of type '{definitionId}'.");
        }
    }
}
#endif
