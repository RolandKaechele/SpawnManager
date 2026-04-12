#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SpawnManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="SpawnManager.Runtime.SpawnManager"/>.
    /// Adds runtime spawn controls, live instance view, and pause/resume buttons.
    /// </summary>
    [CustomEditor(typeof(SpawnManager.Runtime.SpawnManager))]
    public class SpawnManagerEditor : UnityEditor.Editor
    {
        private string _spawnId       = "";
        private string _despawnAllId  = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Open JSON Editor")) SpawnJsonEditorWindow.ShowWindow();

            var mgr = (SpawnManager.Runtime.SpawnManager)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use runtime controls.", MessageType.Info);
                return;
            }

            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Live Instances", mgr.LiveCount);
            EditorGUILayout.Toggle("Paused", mgr.IsPaused);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            // Pause / Resume
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(mgr.IsPaused);
            if (GUILayout.Button("Pause Spawning"))  mgr.PauseSpawning();
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(!mgr.IsPaused);
            if (GUILayout.Button("Resume Spawning")) mgr.ResumeSpawning();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Spawn by definition id
            EditorGUILayout.LabelField("Spawn by Definition Id", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _spawnId = EditorGUILayout.TextField("Definition Id", _spawnId);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_spawnId));
            if (GUILayout.Button("Spawn", GUILayout.Width(70)))
                mgr.Spawn(_spawnId);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Despawn
            EditorGUILayout.LabelField("Despawn by Definition Id", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _despawnAllId = EditorGUILayout.TextField("Definition Id", _despawnAllId);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_despawnAllId));
            if (GUILayout.Button("Despawn All", GUILayout.Width(90)))
                mgr.DespawnAll(_despawnAllId);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (GUILayout.Button("Despawn ALL (global)"))
                mgr.DespawnAll();

            EditorGUILayout.Space(4);

            // Definition list
            EditorGUILayout.LabelField("Registered Definitions", EditorStyles.miniBoldLabel);
            foreach (var id in mgr.GetAllDefinitionIds())
            {
                int liveCount = mgr.GetLiveCount(id);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {id}  (live: {liveCount})");
                if (GUILayout.Button("Spawn", GUILayout.Width(60)))
                    mgr.Spawn(id);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif
