using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Editor
{
    [InitializeOnLoad]
    public class HierarchyEnhancer
    {
        private static readonly Color headerColor = new(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color headerTextColor = new(0.8f, 0.8f, 0.8f, 1f);

        static HierarchyEnhancer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            // 1. Headers: [NAME]
            if (obj.name.StartsWith("[") && obj.name.EndsWith("]"))
            {
                DrawHeader(selectionRect, obj.name);
            }
            else
            {
                DrawStandardItem(selectionRect, obj);
            }
        }

        private static void DrawHeader(Rect rect, string name)
        {
            // Background
            // Create a unique-ish color based on the name hash
            int hash = name.GetHashCode();
            float hue = Mathf.Abs((hash % 100) / 100f);
            // Saturation 0.4-0.6, Value 0.3-0.5 for nice muted colors
            Color color = Color.HSVToRGB(hue, 0.5f, 0.4f);
            color.a = 1f;

            EditorGUI.DrawRect(rect, color);

            // Centered Text
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = Color.white
                }
            };

            EditorGUI.LabelField(rect, name.ToUpper(), style);
        }

        private static void DrawStandardItem(Rect rect, GameObject obj)
        {
            float currentX = rect.xMax;
            float height = rect.height;

            // --- Layer Selector ---
            string layerName = LayerMask.LayerToName(obj.layer);
            GUIContent layerContent = new GUIContent($"{layerName}");
            float layerWidth = EditorStyles.miniLabel.CalcSize(layerContent).x + 5;
            if (layerWidth > 100) layerWidth = 100;

            Rect layerRect = new Rect(currentX - layerWidth, rect.y, layerWidth, height);
            currentX -= layerWidth;

            GUIStyle layerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal =
                {
                    textColor = new Color(0.7f, 0.7f, 0.7f, 1f)
                }
            };

            if (GUI.Button(layerRect, layerContent, layerStyle))
            {
                ShowLayerMenu(obj);
            }

            currentX -= 5;

            // --- Components ---
            List<Component> allComponents = obj.GetComponents<Component>().ToList();

            // 1. Separate "Unity/Standard" components from "Personal Scripts"
            
            List<Component> standardComponents = new List<Component>();
            List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();

            foreach (Component comp in allComponents)
            {
                if (comp == null || comp is Transform || comp is RectTransform || comp is CanvasRenderer) continue;

                if (comp is MonoBehaviour mb && !IsStandardUnityType(comp.GetType()))
                {
                    scriptComponents.Add(mb);
                }
                else
                {
                    standardComponents.Add(comp);
                }
            }

            // Draw Scripts (Grouped)
            if (scriptComponents.Count > 0)
            {
                float iconSize = height;
                Rect iconRect = new Rect(currentX - iconSize, rect.y, iconSize, height);
                currentX -= iconSize;

                // Icon
                Texture scriptIcon;
                if (scriptComponents.Count == 1)
                {
                    scriptIcon = AssetPreview.GetMiniThumbnail(scriptComponents[0]) ?? EditorGUIUtility.IconContent("cs Script Icon").image;
                }
                else
                {
                    scriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image;
                }

                bool anyEnabled = scriptComponents.Any(s => s.enabled);
                Color iconColor = anyEnabled ? Color.white : new Color(1, 1, 1, 0.4f);
                Color oldColor = GUI.color;
                GUI.color = iconColor;

                if (GUI.Button(iconRect, new GUIContent(scriptIcon, "Scripts"), GUIStyle.none))
                {
                    if (scriptComponents.Count == 1)
                    {
                        ToggleComponent(scriptComponents[0]);
                    }
                    else
                    {
                        ShowScriptMenu(scriptComponents);
                    }
                }
                GUI.color = oldColor;

                if (scriptComponents.Count > 1)
                {
                    GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        normal =
                        {
                            textColor = Color.black
                        },
                        alignment = TextAnchor.LowerRight
                    };
                    Rect countRect = new Rect(iconRect.x, iconRect.y, iconRect.width + 2, iconRect.height);
                    GUI.Label(countRect, scriptComponents.Count.ToString(), countStyle);
                }

                currentX -= 2;
            }

            foreach (Component comp in standardComponents.Reverse<Component>())
            {
                Texture icon = AssetPreview.GetMiniThumbnail(comp);
                if (icon == null) continue;

                float iconSize = height;
                Rect iconRect = new Rect(currentX - iconSize, rect.y, iconSize, height);
                currentX -= iconSize;

                bool isEnabled = IsEnabled(comp);
                Color iconColor = isEnabled ? Color.white : new Color(1, 1, 1, 0.4f);
                Color oldColor = GUI.color;
                GUI.color = iconColor;

                if (GUI.Button(iconRect, new GUIContent(icon, comp.GetType().Name), GUIStyle.none))
                {
                    ToggleComponent(comp);
                }

                GUI.color = oldColor;
                currentX -= 2;

                if (currentX < rect.x + 200) break;
            }

            float toggleSize = height;
            Rect activeRect = new Rect(currentX - toggleSize - 5, rect.y, toggleSize, height);
            
            if (!(activeRect.x > rect.x + 100)) return;
            
            Texture eyeIcon = EditorGUIUtility.IconContent(obj.activeSelf ? "d_scenevis_visible_hover" : "d_scenevis_hidden_hover").image;
            
            if (!GUI.Button(activeRect, new GUIContent(eyeIcon, "Toggle Active"), GUIStyle.none)) return;
            
            Undo.RecordObject(obj, "Toggle Active");
            obj.SetActive(!obj.activeSelf);
        }

        private static bool IsStandardUnityType(System.Type type)
        {
            return type.Namespace != null && (type.Namespace.StartsWith("UnityEngine") || type.Namespace.StartsWith("UnityEditor"));
        }

        private static void ShowScriptMenu(List<MonoBehaviour> scripts)
        {
            GenericMenu menu = new GenericMenu();
            foreach (MonoBehaviour script in scripts)
            {
                string name = script.GetType().Name;
                menu.AddItem(new GUIContent(name), script.enabled, () =>
                {
                    Undo.RecordObject(script, "Toggle Script");
                    script.enabled = !script.enabled;
                });
            }
            menu.ShowAsContext();
        }

        private static bool IsEnabled(Component comp)
        {
            if (comp is Behaviour behaviour) return behaviour.enabled;
            if (comp is Renderer renderer) return renderer.enabled;
            if (comp is Collider collider) return collider.enabled;
            return true;
        }

        private static void ToggleComponent(Component comp)
        {
            Undo.RecordObject(comp, "Toggle Component");
            if (comp is Behaviour behaviour) behaviour.enabled = !behaviour.enabled;
            else if (comp is Renderer renderer) renderer.enabled = !renderer.enabled;
            else if (comp is Collider collider) collider.enabled = !collider.enabled;
        }

        private static void ShowLayerMenu(GameObject obj)
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName)) continue;

                int layerIndex = i;
                bool on = obj.layer == layerIndex;

                menu.AddItem(new GUIContent(layerName), on, () =>
                {
                    Undo.RecordObject(obj, "Change Layer");
                    obj.layer = layerIndex;
                });
            }
            menu.ShowAsContext();
        }
    }
}
