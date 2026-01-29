using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Editor
{
    [InitializeOnLoad]
    public class HierarchyEnhancer
    {
        public enum HierarchyMode
        {
            TreeLines,  // Lignes de connexion
            Full,       // Icônes à droite (original)
            Compact     // Barres de couleur
        }

        private static HierarchyMode _currentMode = HierarchyMode.TreeLines;

        // Tree Lines settings
        private static readonly Color TreeLineColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        private static readonly float TreeLineWidth = 1f;

        // Couleurs par type (pour les icônes)
        private static readonly Color CameraColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        private static readonly Color LightColor = new Color(1f, 0.8f, 0.2f, 1f);
        private static readonly Color AudioColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        private static readonly Color UIColor = new Color(0.9f, 0.4f, 0.9f, 1f);
        private static readonly Color MeshColor = new Color(0.5f, 0.7f, 0.9f, 1f);
        private static readonly Color PhysicsColor = new Color(0.9f, 0.5f, 0.2f, 1f);
        private static readonly Color AnimatorColor = new Color(0.7f, 0.3f, 0.9f, 1f);
        private static readonly Color ScriptColor = new Color(0.3f, 0.7f, 0.3f, 1f);
        private static readonly Color EmptyColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        private static Dictionary<int, string> _tooltipCache = new Dictionary<int, string>();
        private static Dictionary<int, Color> _colorCache = new Dictionary<int, Color>();

        static HierarchyEnhancer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyChanged += ClearCache;
        }

        private static void ClearCache()
        {
            _tooltipCache.Clear();
            _colorCache.Clear();
        }

        #region Menu Items

        [MenuItem("EthanToolBox/Hierarchy/Tree Lines Mode")]
        public static void SetTreeLinesMode()
        {
            _currentMode = HierarchyMode.TreeLines;
            ClearCache();
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem("EthanToolBox/Hierarchy/Full Mode (Icons)")]
        public static void SetFullMode()
        {
            _currentMode = HierarchyMode.Full;
            ClearCache();
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem("EthanToolBox/Hierarchy/Compact Mode (Color Bars)")]
        public static void SetCompactMode()
        {
            _currentMode = HierarchyMode.Compact;
            ClearCache();
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem("EthanToolBox/Hierarchy/Tree Lines Mode", true)]
        public static bool ValidateTreeLinesMode()
        {
            Menu.SetChecked("EthanToolBox/Hierarchy/Tree Lines Mode", _currentMode == HierarchyMode.TreeLines);
            return true;
        }

        [MenuItem("EthanToolBox/Hierarchy/Full Mode (Icons)", true)]
        public static bool ValidateFullMode()
        {
            Menu.SetChecked("EthanToolBox/Hierarchy/Full Mode (Icons)", _currentMode == HierarchyMode.Full);
            return true;
        }

        [MenuItem("EthanToolBox/Hierarchy/Compact Mode (Color Bars)", true)]
        public static bool ValidateCompactMode()
        {
            Menu.SetChecked("EthanToolBox/Hierarchy/Compact Mode (Color Bars)", _currentMode == HierarchyMode.Compact);
            return true;
        }

        #endregion

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            // Headers: [NAME]
            if (obj.name.StartsWith("[") && obj.name.EndsWith("]"))
            {
                DrawHeader(selectionRect, obj.name);
                return;
            }

            switch (_currentMode)
            {
                case HierarchyMode.TreeLines:
                    DrawTreeLinesItem(selectionRect, obj, instanceID);
                    break;
                case HierarchyMode.Full:
                    DrawStandardItem(selectionRect, obj);
                    break;
                case HierarchyMode.Compact:
                    DrawCompactItem(selectionRect, obj, instanceID);
                    break;
            }
        }

        #region Tree Lines Mode

        // Couleurs pour les lignes par profondeur
        private static readonly Color[] DepthColors = new Color[]
        {
            new Color(0.5f, 0.7f, 0.9f, 0.8f),  // Bleu clair - niveau 1
            new Color(0.5f, 0.9f, 0.5f, 0.8f),  // Vert - niveau 2
            new Color(0.9f, 0.7f, 0.4f, 0.8f),  // Orange - niveau 3
            new Color(0.9f, 0.5f, 0.9f, 0.8f),  // Violet - niveau 4
            new Color(0.9f, 0.9f, 0.5f, 0.8f),  // Jaune - niveau 5
            new Color(0.5f, 0.9f, 0.9f, 0.8f),  // Cyan - niveau 6+
        };

        private static void DrawTreeLinesItem(Rect rect, GameObject obj, int instanceID)
        {
            Transform transform = obj.transform;
            int depth = GetDepth(transform);
            float height = rect.height;
            float currentX = rect.xMax;

            // 1. Bouton de visibilité (œil) - à droite
            float eyeSize = height;
            Rect eyeRect = new Rect(currentX - eyeSize - 2, rect.y, eyeSize, height);
            currentX -= eyeSize + 4;

            Texture eyeIcon = EditorGUIUtility.IconContent(obj.activeSelf ? "d_scenevis_visible_hover" : "d_scenevis_hidden_hover").image;
            if (GUI.Button(eyeRect, new GUIContent(eyeIcon, "Toggle Active"), GUIStyle.none))
            {
                Undo.RecordObject(obj, "Toggle Active");
                obj.SetActive(!obj.activeSelf);
            }

            // 3. Layer affiché avec menu
            string layerName = LayerMask.LayerToName(obj.layer);
            GUIContent layerContent = new GUIContent(layerName);
            float layerWidth = Mathf.Min(EditorStyles.miniLabel.CalcSize(layerContent).x + 4, 80);
            Rect layerRect = new Rect(currentX - layerWidth, rect.y, layerWidth, height);
            currentX -= layerWidth + 2;

            GUIStyle layerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 1f) },
                alignment = TextAnchor.MiddleRight
            };

            if (GUI.Button(layerRect, layerContent, layerStyle))
            {
                ShowLayerMenu(obj);
            }

            // 4. Dessiner les lignes de connexion avec couleurs par profondeur
            if (depth > 0)
            {
                DrawTreeLinesColored(rect, transform, depth);
            }

            // Tooltip au survol
            if (rect.Contains(Event.current.mousePosition))
            {
                string tooltip = GetCachedTooltip(obj, instanceID);
                GUI.Label(rect, new GUIContent("", tooltip));
            }
        }

        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            Transform parent = transform.parent;
            while (parent != null)
            {
                depth++;
                parent = parent.parent;
            }
            return depth;
        }

        private static Color GetDepthColor(int depth)
        {
            if (depth <= 0) return TreeLineColor;
            int index = Mathf.Min(depth - 1, DepthColors.Length - 1);
            return DepthColors[index];
        }

        private static void DrawTreeLinesColored(Rect rect, Transform transform, int depth)
        {
            float indentPerLevel = 14f;
            float baseX = rect.x - (depth * indentPerLevel) - 14f;

            // Couleur basée sur la profondeur actuelle
            Color lineColor = GetDepthColor(depth);

            // Ligne horizontale vers l'objet
            float horizontalY = rect.y + rect.height / 2f;
            float horizontalStartX = baseX + (depth - 1) * indentPerLevel + indentPerLevel / 2f + 7f;

            // Position de fin : juste avant l'icône de la flèche (si enfants) ou avant l'icône du GO
            bool hasChildren = transform.childCount > 0;
            float horizontalEndX = rect.x - (hasChildren ? 14f : 2f);

            // S'assurer que la ligne a une longueur positive
            float lineWidth = horizontalEndX - horizontalStartX;
            if (lineWidth > 0)
            {
                EditorGUI.DrawRect(new Rect(horizontalStartX, horizontalY, lineWidth, TreeLineWidth), lineColor);
            }

            // Ligne verticale
            Transform parent = transform.parent;
            if (parent != null)
            {
                float verticalX = horizontalStartX;
                float verticalTopY = rect.y;

                int siblingIndex = transform.GetSiblingIndex();
                bool isLastChild = siblingIndex == parent.childCount - 1;

                float verticalBottomY = isLastChild ? horizontalY : rect.y + rect.height;

                EditorGUI.DrawRect(new Rect(verticalX, verticalTopY, TreeLineWidth, verticalBottomY - verticalTopY), lineColor);
            }

            // Lignes verticales pour les parents (avec leurs propres couleurs)
            Transform current = transform.parent;
            int currentDepth = depth - 1;
            while (current != null && currentDepth > 0)
            {
                Transform grandParent = current.parent;
                if (grandParent != null)
                {
                    int parentSiblingIndex = current.GetSiblingIndex();
                    bool parentIsLastChild = parentSiblingIndex == grandParent.childCount - 1;

                    if (!parentIsLastChild)
                    {
                        float lineX = baseX + (currentDepth - 1) * indentPerLevel + indentPerLevel / 2f + 7f;
                        Color parentLineColor = GetDepthColor(currentDepth);
                        EditorGUI.DrawRect(new Rect(lineX, rect.y, TreeLineWidth, rect.height), parentLineColor);
                    }
                }
                current = grandParent;
                currentDepth--;
            }
        }

        private static Component GetMainComponent(GameObject obj)
        {
            if (obj.GetComponent<Camera>()) return obj.GetComponent<Camera>();
            if (obj.GetComponent<Light>()) return obj.GetComponent<Light>();
            if (obj.GetComponent<AudioSource>()) return obj.GetComponent<AudioSource>();
            if (obj.GetComponent<Canvas>()) return obj.GetComponent<Canvas>();
            if (obj.GetComponent<ParticleSystem>()) return obj.GetComponent<ParticleSystem>();
            if (obj.GetComponent<Animator>()) return obj.GetComponent<Animator>();
            if (obj.GetComponent<MeshRenderer>()) return obj.GetComponent<MeshRenderer>();
            if (obj.GetComponent<SpriteRenderer>()) return obj.GetComponent<SpriteRenderer>();

            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp == null) continue;
                if (comp is MonoBehaviour && !IsStandardUnityType(comp.GetType()))
                {
                    return comp;
                }
            }

            return null;
        }

        #endregion

        #region Compact Mode (Color Bars + Tooltips)

        private static void DrawCompactItem(Rect rect, GameObject obj, int instanceID)
        {
            Color barColor = GetCachedColor(obj, instanceID);
            DrawColorBar(rect, barColor);

            if (!obj.activeInHierarchy)
            {
                DrawInactiveOverlay(rect);
            }

            if (rect.Contains(Event.current.mousePosition))
            {
                string tooltip = GetCachedTooltip(obj, instanceID);
                GUI.Label(rect, new GUIContent("", tooltip));
            }
        }

        private static Color GetCachedColor(GameObject go, int instanceID)
        {
            if (_colorCache.TryGetValue(instanceID, out Color cached))
                return cached;

            Color color = DetermineColor(go);
            _colorCache[instanceID] = color;
            return color;
        }

        private static Color DetermineColor(GameObject go)
        {
            if (go.GetComponent<Camera>()) return CameraColor;
            if (go.GetComponent<Light>()) return LightColor;
            if (go.GetComponent<AudioSource>() || go.GetComponent<AudioListener>()) return AudioColor;
            if (go.GetComponent<Canvas>()) return UIColor;
            if (go.GetComponent<ParticleSystem>()) return new Color(0.9f, 0.6f, 0.7f, 1f);
            if (go.GetComponent<Animator>() || go.GetComponent<Animation>()) return AnimatorColor;
            if (go.GetComponent<Rigidbody>() || go.GetComponent<Rigidbody2D>() ||
                go.GetComponent<Collider>() || go.GetComponent<Collider2D>()) return PhysicsColor;
            if (go.GetComponent<MeshRenderer>() || go.GetComponent<SkinnedMeshRenderer>() ||
                go.GetComponent<SpriteRenderer>()) return MeshColor;

            var components = go.GetComponents<Component>();
            bool hasCustomScript = false;
            foreach (var comp in components)
            {
                if (comp == null) continue;
                if (comp is MonoBehaviour && !IsStandardUnityType(comp.GetType()))
                {
                    hasCustomScript = true;
                    break;
                }
            }

            if (hasCustomScript) return ScriptColor;
            if (components.Length <= 1) return EmptyColor;

            return new Color(0.4f, 0.4f, 0.4f, 0.6f);
        }

        private static void DrawColorBar(Rect rect, Color color)
        {
            Rect barRect = new Rect(rect.x - 2, rect.y + 1, 3, rect.height - 2);
            EditorGUI.DrawRect(barRect, color);
        }

        private static void DrawInactiveOverlay(Rect rect)
        {
            Rect indicatorRect = new Rect(rect.xMax - 16, rect.y + 2, 12, 12);
            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Label(indicatorRect, "○");
            GUI.color = Color.white;
        }

        #endregion

        #region Tooltip

        private static string GetCachedTooltip(GameObject go, int instanceID)
        {
            if (_tooltipCache.TryGetValue(instanceID, out string cached))
                return cached;

            string tooltip = BuildTooltip(go);
            _tooltipCache[instanceID] = tooltip;
            return tooltip;
        }

        private static string BuildTooltip(GameObject go)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"{go.name}");
            sb.AppendLine("─────────────────");

            if (!go.activeInHierarchy) sb.AppendLine("⚠ Inactive");
            if (go.isStatic) sb.AppendLine("📌 Static");
            if (go.tag != "Untagged") sb.AppendLine($"Tag: {go.tag}");

            string layerName = LayerMask.LayerToName(go.layer);
            if (layerName != "Default") sb.AppendLine($"Layer: {layerName}");

            sb.AppendLine();
            sb.AppendLine("Components:");

            var components = go.GetComponents<Component>();
            int count = 0;
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    sb.AppendLine("  ⚠ Missing Script");
                    continue;
                }
                if (comp is Transform) continue;

                sb.AppendLine($"  • {comp.GetType().Name}");
                count++;
                if (count >= 8)
                {
                    int remaining = components.Length - count - 1;
                    if (remaining > 0) sb.AppendLine($"  ... +{remaining} more");
                    break;
                }
            }

            int childCount = go.transform.childCount;
            if (childCount > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Children: {childCount}");
            }

            return sb.ToString().TrimEnd();
        }

        #endregion

        #region Header

        private static void DrawHeader(Rect rect, string name)
        {
            int hash = name.GetHashCode();
            float hue = Mathf.Abs((hash % 100) / 100f);
            Color color = Color.HSVToRGB(hue, 0.5f, 0.4f);
            color.a = 1f;

            EditorGUI.DrawRect(rect, color);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            EditorGUI.LabelField(rect, name.ToUpper(), style);
        }

        #endregion

        #region Full Mode (Icons)

        private static void DrawStandardItem(Rect rect, GameObject obj)
        {
            float currentX = rect.xMax;
            float height = rect.height;

            string layerName = LayerMask.LayerToName(obj.layer);
            GUIContent layerContent = new GUIContent($"{layerName}");
            float layerWidth = EditorStyles.miniLabel.CalcSize(layerContent).x + 5;
            if (layerWidth > 100) layerWidth = 100;

            Rect layerRect = new Rect(currentX - layerWidth, rect.y, layerWidth, height);
            currentX -= layerWidth;

            GUIStyle layerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 1f) }
            };

            if (GUI.Button(layerRect, layerContent, layerStyle))
            {
                ShowLayerMenu(obj);
            }

            currentX -= 5;

            List<Component> allComponents = obj.GetComponents<Component>().ToList();

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

            if (scriptComponents.Count > 0)
            {
                float iconSize = height;
                Rect iconRect = new Rect(currentX - iconSize, rect.y, iconSize, height);
                currentX -= iconSize;

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
                        normal = { textColor = Color.black },
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

        #endregion

        #region Helpers

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

        #endregion
    }
}
