#if SPAWNMANAGER_CSM
using CutsceneManager.Runtime;
using UnityEngine;

namespace SpawnManager.Runtime
{
    /// <summary>
    /// <b>CutsceneManagerBridge</b> connects SpawnManager to CutsceneManager.
    /// <para>
    /// When <c>SPAWNMANAGER_CSM</c> is defined:
    /// <list type="bullet">
    ///   <item>Pauses spawning when a cutscene starts.</item>
    ///   <item>Resumes spawning when the cutscene ends or is skipped.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SpawnManager/CutsceneManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class CutsceneManagerBridge : UnityEngine.MonoBehaviour
    {
        private SpawnManager    _spawnManager;
        private CutsceneManager _cutsceneManager;

        private void Awake()
        {
            _spawnManager    = GetComponent<SpawnManager>()    ?? FindFirstObjectByType<SpawnManager>();
            _cutsceneManager = GetComponent<CutsceneManager>() ?? FindFirstObjectByType<CutsceneManager>();

            if (_spawnManager    == null) Debug.LogWarning("[SpawnManager/CutsceneManagerBridge] SpawnManager not found.");
            if (_cutsceneManager == null) Debug.LogWarning("[SpawnManager/CutsceneManagerBridge] CutsceneManager not found.");
        }

        private void OnEnable()
        {
            if (_cutsceneManager == null) return;
            _cutsceneManager.OnCutsceneStarted  += HandleCutsceneStarted;
            _cutsceneManager.OnCutsceneFinished += HandleCutsceneFinished;
        }

        private void OnDisable()
        {
            if (_cutsceneManager == null) return;
            _cutsceneManager.OnCutsceneStarted  -= HandleCutsceneStarted;
            _cutsceneManager.OnCutsceneFinished -= HandleCutsceneFinished;
        }

        private void HandleCutsceneStarted(string id)  => _spawnManager?.PauseSpawning();
        private void HandleCutsceneFinished(string id) => _spawnManager?.ResumeSpawning();
    }
}
#endif
