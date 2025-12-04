using UnityEngine;
using UnityEditor;
using System.Linq;

namespace EthanToolBox.Editor
{
    [InitializeOnLoad]
    public static class HierarchyScriptIndicator
    {
        static HierarchyScriptIndicator()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
                return;

            var scripts = gameObject.GetComponents<MonoBehaviour>()
                .Where(c => c != null)
                .Where(c => 
                {
                    MonoScript script = MonoScript.FromMonoBehaviour(c);
                    return script != null && AssetDatabase.GetAssetPath(script).StartsWith("Assets/");
                })
                .ToList();

            if (scripts.Count > 0)
            {
                Rect r = new Rect(selectionRect);
                r.width = 50;
                r.x = selectionRect.xMax - 50;
                
                GUIContent icon = EditorGUIUtility.IconContent("cs Script Icon");
                icon.tooltip = string.Join("\n", scripts.Select(s => s.GetType().Name));
                
                GUI.Label(r, icon);

                if (scripts.Count > 1)
                {
                    GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel);
                    countStyle.alignment = TextAnchor.MiddleCenter;
                    countStyle.normal.textColor = Color.white;
                    countStyle.fontSize = 16;
                    countStyle.fontStyle = FontStyle.Bold;
                    
                    GUI.Label(r, scripts.Count.ToString(), countStyle);
                }
            }
        }
    }
}
