using UnityEngine;
using UnityEditor;

namespace Elarion.VoxelEngine.Editor
{
    /// <summary>
    /// Custom inspector per WorldManager con debug tools.
    /// </summary>
    [CustomEditor(typeof(WorldManager))]
    public class WorldManagerEditor : UnityEditor.Editor
    {
        private WorldManager worldManager;
        
        private void OnEnable()
        {
            worldManager = (WorldManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField($"Loaded Chunks: {worldManager.LoadedChunkCount}");
                EditorGUILayout.LabelField($"Generation Queue: {worldManager.ChunksInGenerationQueue}");
                EditorGUILayout.LabelField($"Mesh Queue: {worldManager.ChunksInMeshQueue}");
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Force Regenerate All Chunks"))
                {
                    // TODO: implementare force regenerate
                    Debug.Log("Force regenerate requested");
                }
                
                if (GUILayout.Button("Clear All Chunks"))
                {
                    // TODO: implementare clear all
                    Debug.Log("Clear all chunks requested");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime statistics", MessageType.Info);
            }
        }
    }
}
