#if SPAWNMANAGER_MLF
using MapLoaderFramework.Runtime;
using UnityEngine;

namespace SpawnManager.Runtime
{
    /// <summary>
    /// <b>MapLoaderBridge</b> connects SpawnManager to MapLoaderFramework.
    /// <para>
    /// When <c>SPAWNMANAGER_MLF</c> is defined:
    /// <list type="bullet">
    ///   <item>Despawns all live instances when a new chapter/map is loaded.</item>
    ///   <item>Optionally triggers the spawn definitions listed in the map's <c>autoSpawnIds</c> array.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SpawnManager/MapLoader Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MapLoaderBridge : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Tooltip("Clear all live instances when a new map is loaded.")]
        [UnityEngine.SerializeField] private bool clearOnMapLoad = true;

        [UnityEngine.Tooltip("Automatically trigger spawn definitions listed in the map's autoSpawnIds.")]
        [UnityEngine.SerializeField] private bool autoSpawnFromMap = true;

        private SpawnManager       _spawnManager;
        private MapLoaderFramework _mapLoader;

        private void Awake()
        {
            _spawnManager = GetComponent<SpawnManager>()    ?? FindFirstObjectByType<SpawnManager>();
            _mapLoader    = GetComponent<MapLoaderFramework>() ?? FindFirstObjectByType<MapLoaderFramework>();

            if (_spawnManager == null) Debug.LogWarning("[SpawnManager/MapLoaderBridge] SpawnManager not found.");
            if (_mapLoader    == null) Debug.LogWarning("[SpawnManager/MapLoaderBridge] MapLoaderFramework not found.");
        }

        private void OnEnable()
        {
            if (_mapLoader != null) _mapLoader.OnMapLoaded += HandleMapLoaded;
        }

        private void OnDisable()
        {
            if (_mapLoader != null) _mapLoader.OnMapLoaded -= HandleMapLoaded;
        }

        private void HandleMapLoaded(MapData mapData)
        {
            if (_spawnManager == null) return;

            if (clearOnMapLoad)
                _spawnManager.DespawnAll();

            if (autoSpawnFromMap && mapData?.autoSpawnIds != null)
                foreach (var id in mapData.autoSpawnIds)
                    _spawnManager.Spawn(id);
        }
    }
}
#endif
