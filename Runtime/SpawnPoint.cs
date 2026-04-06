using UnityEngine;

namespace SpawnManager.Runtime
{
    /// <summary>
    /// Place on any scene GameObject to register it as a named spawn point with
    /// <see cref="SpawnManager"/>. SpawnManager will automatically use this position
    /// when a <see cref="SpawnDefinition"/> references this point's id.
    /// </summary>
    [AddComponentMenu("SpawnManager/Spawn Point")]
    public class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Unique id for this spawn point. Used in SpawnDefinition.spawnPointId.")]
        [SerializeField] private string pointId;

        [Tooltip("Optional tag for filtering spawn points by category.")]
        [SerializeField] private string pointTag;

        private void Awake()
        {
            var mgr = FindFirstObjectByType<SpawnManager>();
            if (mgr == null)
            {
                Debug.LogWarning($"[SpawnPoint:{pointId}] SpawnManager not found in scene.");
                return;
            }

            mgr.RegisterSpawnPoint(new SpawnPointData
            {
                id       = pointId,
                position = transform.position,
                rotation = transform.rotation,
                tag      = pointTag
            });
        }

        private void OnDestroy()
        {
            var mgr = FindFirstObjectByType<SpawnManager>();
            mgr?.UnregisterSpawnPoint(pointId);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
            Gizmos.DrawSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f,
                string.IsNullOrEmpty(pointId) ? "(no id)" : pointId);
#endif
        }
    }
}
