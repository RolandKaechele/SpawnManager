#if SPAWNMANAGER_DOTWEEN
using System;
using UnityEngine;
using DG.Tweening;

namespace SpawnManager.Runtime
{
    /// <summary>
    /// Optional bridge that adds DOTween-driven scale-punch and fade-out effects
    /// to SpawnManager's spawn and despawn operations.
    /// Enable define <c>SPAWNMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Hooks <see cref="SpawnManager.OnSpawnedCallback"/> and
    /// <see cref="SpawnManager.OnDespawnedCallback"/> to animate spawned/despawned objects.
    /// Objects must have a <see cref="Renderer"/> for the fade-out effect.
    /// </para>
    /// </summary>
    [AddComponentMenu("SpawnManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenSpawnBridge : MonoBehaviour
    {
        [Header("Spawn Effect")]
        [Tooltip("Scale punch magnitude applied to spawned objects.")]
        [SerializeField] private float spawnPunchScale = 0.3f;

        [Tooltip("Duration of the spawn scale-punch.")]
        [SerializeField] private float spawnPunchDuration = 0.3f;

        [Tooltip("Ease applied to the spawn punch.")]
        [SerializeField] private Ease  spawnEase = Ease.OutElastic;

        [Header("Despawn Effect")]
        [Tooltip("Duration of the despawn scale-shrink tween.")]
        [SerializeField] private float despawnDuration = 0.25f;

        [Tooltip("Ease applied to the despawn shrink.")]
        [SerializeField] private Ease  despawnEase = Ease.InBack;

        private SpawnManager _spawnManager;

        private void Awake()
        {
            _spawnManager = GetComponent<SpawnManager>() ?? FindFirstObjectByType<SpawnManager>();
            if (_spawnManager == null)
                Debug.LogWarning("[SpawnManager/DotweenSpawnBridge] SpawnManager not found.");
        }

        private void OnEnable()
        {
            if (_spawnManager == null) return;
            _spawnManager.OnSpawnedCallback  += HandleSpawned;
            _spawnManager.OnDespawnedCallback += HandleDespawned;
        }

        private void OnDisable()
        {
            if (_spawnManager == null) return;
            if (_spawnManager.OnSpawnedCallback  == (Action<string, string, GameObject>)HandleSpawned)
                _spawnManager.OnSpawnedCallback = null;
            if (_spawnManager.OnDespawnedCallback == (Action<string, string, GameObject>)HandleDespawned)
                _spawnManager.OnDespawnedCallback = null;
        }

        private void HandleSpawned(string defId, string instanceId, GameObject go)
        {
            if (go == null) return;
            go.transform.localScale = Vector3.zero;
            go.transform.DOScale(Vector3.one, spawnPunchDuration)
              .SetEase(spawnEase);
        }

        private void HandleDespawned(string defId, string instanceId, GameObject go)
        {
            if (go == null) return;
            DOTween.Kill(go.transform);
            go.transform.DOScale(Vector3.zero, despawnDuration)
              .SetEase(despawnEase)
              .OnComplete(() => go.SetActive(false));
        }
    }
}
#else
namespace SpawnManager.Runtime
{
    /// <summary>No-op stub — enable define <c>SPAWNMANAGER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("SpawnManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenSpawnBridge : UnityEngine.MonoBehaviour { }
}
#endif
