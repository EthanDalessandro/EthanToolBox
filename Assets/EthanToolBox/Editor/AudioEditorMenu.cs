using UnityEngine;
using UnityEditor;
using EthanToolBox.Core.Audio;

namespace EthanToolBox.Editor
{
    public static class AudioEditorMenu
    {
        [MenuItem("EthanToolBox/Setup Audio Manager")]
        public static void SetupAudioManager()
        {
            var existingManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
            if (existingManager != null)
            {
                EditorUtility.DisplayDialog("Setup Audio Manager", $"Audio Manager already exists: {existingManager.gameObject.name}", "OK");
                return;
            }

            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create Audio Manager");
            Selection.activeGameObject = go;
            
            Debug.Log("Audio Manager created and ready for injection.");
        }
    }
}
