#if SPAWNMANAGER_STM
using StateManager.Runtime;
using UnityEngine;

namespace SpawnManager.Runtime
{
    /// <summary>
    /// <b>StateManagerBridge</b> connects SpawnManager to StateManager.
    /// <para>
    /// When <c>SPAWNMANAGER_STM</c> is defined:
    /// <list type="bullet">
    ///   <item>Pauses spawning when a Cutscene, Loading, or Dialogue state becomes active.</item>
    ///   <item>Resumes spawning when those states pop.</item>
    /// </list>
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("SpawnManager/StateManager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class StateManagerBridge : UnityEngine.MonoBehaviour
    {
        [UnityEngine.SerializeField]
        [UnityEngine.Tooltip("State ids that pause spawning.")]
        private string[] pauseStateIds = { "Cutscene", "Loading", "Dialogue" };

        private SpawnManager _spawnManager;
        private StateManager _stateManager;

        private void Awake()
        {
            _spawnManager = GetComponent<SpawnManager>() ?? FindFirstObjectByType<SpawnManager>();
            _stateManager = GetComponent<StateManager>() ?? FindFirstObjectByType<StateManager>();

            if (_spawnManager == null) Debug.LogWarning("[SpawnManager/StateManagerBridge] SpawnManager not found.");
            if (_stateManager == null) Debug.LogWarning("[SpawnManager/StateManagerBridge] StateManager not found.");
        }

        private void OnEnable()
        {
            if (_stateManager == null) return;
            _stateManager.OnStatePushed += HandleStatePushed;
            _stateManager.OnStatePopped += HandleStatePopped;
        }

        private void OnDisable()
        {
            if (_stateManager == null) return;
            _stateManager.OnStatePushed -= HandleStatePushed;
            _stateManager.OnStatePopped -= HandleStatePopped;
        }

        private bool IsPauseState(string id)
        {
            foreach (var s in pauseStateIds)
                if (string.Equals(s, id, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void HandleStatePushed(string id) { if (IsPauseState(id)) _spawnManager?.PauseSpawning(); }
        private void HandleStatePopped(string id) { if (IsPauseState(id)) _spawnManager?.ResumeSpawning(); }
    }
}
#endif
