using UnityEngine;
using UnityEditor;
using EthanToolBox.Core.DependencyInjection;

namespace EthanToolBox.Editor
{
    public static class DIEditorMenu
    {
        [MenuItem("EthanToolBox/Setup DI")]
        public static void SetupDI()
        {
            var existingRoot = UnityEngine.Object.FindFirstObjectByType<DICompositionRoot>();
            if (existingRoot != null)
            {
                EditorUtility.DisplayDialog("Setup DI", $"DI Composition Root already exists: {existingRoot.gameObject.name}", "OK");
                return;
            }

            var go = new GameObject("DICompositionRoot");
            go.AddComponent<DefaultCompositionRoot>();
            Undo.RegisterCreatedObjectUndo(go, "Create DI Composition Root");
            Selection.activeGameObject = go;

            Debug.Log("DI Composition Root created.");
        }
    }
}
