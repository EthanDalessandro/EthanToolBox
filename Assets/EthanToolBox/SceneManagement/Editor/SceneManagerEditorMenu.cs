using EthanToolBox.Core.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Editor.SceneManagement
{
    public static class SceneManagerEditorMenu
    {
        [MenuItem("EthanToolBox/Setup Scene Manager")]
        public static void SetupSceneManager()
        {
            var existingManager = UnityEngine.Object.FindFirstObjectByType<SceneLoader>();
            if (existingManager != null)
            {
                EditorUtility.DisplayDialog("Setup Scene Manager", $"Scene Manager already exists: {existingManager.gameObject.name}", "OK");
                return;
            }

            var go = new GameObject("SceneManager");
            go.AddComponent<SceneLoader>();
            Undo.RegisterCreatedObjectUndo(go, "Create Scene Manager");
            Selection.activeGameObject = go;

            Debug.Log("Scene Manager created and ready for injection.");
        }
    }
}
