using UnityEngine;
using UnityEditor;
using EthanToolBox.Core.DependencyInjection;

namespace EthanToolBox.Editor
{
    public static class DIEditorMenu
    {
        [MenuItem("EthanToolBox/Injection/Setup DI")]
        public static void SetupDI()
        {
            var existing = Object.FindFirstObjectByType<DIBootstrapper>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Setup DI", $"DI Bootstrapper already exists: {existing.gameObject.name}", "OK");
                return;
            }

            var go = new GameObject("DIBootstrapper");
            go.AddComponent<DIBootstrapper>();
            Undo.RegisterCreatedObjectUndo(go, "Create DI Bootstrapper");
            Selection.activeGameObject = go;

            Debug.Log("[DI] Bootstrapper created.");
        }
    }
}
