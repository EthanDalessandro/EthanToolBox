using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace EthanToolBox.Editor
{
    [InitializeOnLoad]
    public class InspectorComponentToggler
    {
        private static VisualElement mainContainer;
        private static VisualElement iconsContainer;
        private static VisualElement filtersContainer;
        private static int lastComponentHash = 0;
        private static int lastSelectionID = 0;
        private static Component focusedComponent = null;
        public static Component copiedComponent = null;
        private static HashSet<ComponentCategory> activeFilters = new HashSet<ComponentCategory>();

        public enum ComponentCategory
        {
            All,
            Rendering,
            Physics,
            Animation,
            Audio,
            UI,
            Scripts,
            Other
        }

        private static readonly Dictionary<ComponentCategory, Color> categoryColors = new Dictionary<ComponentCategory, Color>
        {
            { ComponentCategory.All, new Color(0.4f, 0.4f, 0.4f) },
            { ComponentCategory.Rendering, new Color(0.2f, 0.6f, 0.8f) },
            { ComponentCategory.Physics, new Color(0.8f, 0.4f, 0.2f) },
            { ComponentCategory.Animation, new Color(0.6f, 0.2f, 0.8f) },
            { ComponentCategory.Audio, new Color(0.2f, 0.8f, 0.4f) },
            { ComponentCategory.UI, new Color(0.8f, 0.8f, 0.2f) },
            { ComponentCategory.Scripts, new Color(0.3f, 0.6f, 0.3f) },
            { ComponentCategory.Other, new Color(0.5f, 0.5f, 0.5f) }
        };

        static InspectorComponentToggler()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.delayCall += () => OnSelectionChanged();
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }


        private static ComponentCategory GetComponentCategory(Component comp)
        {
            if (comp == null) return ComponentCategory.Other;

            Type type = comp.GetType();
            string typeName = type.Name;
            string fullName = type.FullName ?? "";

            // Rendering
            if (type == typeof(Camera) || type == typeof(Light) ||
                typeName.Contains("Renderer") || typeName.Contains("MeshFilter") ||
                typeName.Contains("Light") || type == typeof(Canvas))
                return ComponentCategory.Rendering;

            // Physics
            if (typeName.Contains("Rigidbody") || typeName.Contains("Collider") ||
                typeName.Contains("Joint") || typeName.Contains("ConstantForce"))
                return ComponentCategory.Physics;

            // Animation
            if (type == typeof(Animator) || type == typeof(Animation) ||
                typeName.Contains("Animator") || typeName.Contains("PlayableDirector"))
                return ComponentCategory.Animation;

            // Audio
            if (type == typeof(AudioSource) || type == typeof(AudioListener) ||
                typeName.Contains("Audio"))
                return ComponentCategory.Audio;

            // UI
            if (fullName.StartsWith("UnityEngine.UI") || fullName.StartsWith("TMPro") ||
                typeName.Contains("Canvas") || typeName.Contains("Text") ||
                typeName.Contains("Image") || typeName.Contains("Button") ||
                typeName.Contains("Slider") || typeName.Contains("EventSystem"))
                return ComponentCategory.UI;

            // Scripts
            if (typeof(MonoBehaviour).IsAssignableFrom(type) && !fullName.StartsWith("UnityEngine"))
                return ComponentCategory.Scripts;

            return ComponentCategory.Other;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += () =>
                {
                    var go = Selection.activeGameObject;
                    if (go != null) RestoreComponentVisibility(go);
                };
            }
            else if (state == PlayModeStateChange.ExitingEditMode)
            {
                var go = Selection.activeGameObject;
                if (go != null) SaveComponentVisibility(go);
            }
        }

        private static string GetComponentKey(GameObject go, Component comp)
        {
            if (go == null || comp == null) return null;
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            string scenePath = go.scene.path ?? "UnsavedScene";
            return $"InspectorToggler_{scenePath}_{path}_{comp.GetType().FullName}";
        }

        private static void SaveComponentVisibility(GameObject go)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                string key = GetComponentKey(go, comp);
                if (key != null)
                {
                    bool isHidden = (comp.hideFlags & HideFlags.HideInInspector) != 0;
                    EditorPrefs.SetBool(key, isHidden);
                }
            }
        }

        private static void RestoreComponentVisibility(GameObject go)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                string key = GetComponentKey(go, comp);
                if (key != null && EditorPrefs.HasKey(key))
                {
                    bool shouldBeHidden = EditorPrefs.GetBool(key);
                    if (shouldBeHidden)
                        comp.hideFlags |= HideFlags.HideInInspector;
                    else
                        comp.hideFlags &= ~HideFlags.HideInInspector;
                }
            }
            InternalEditorUtility.RepaintAllViews();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private static void OnUpdate()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;

            if (go.GetInstanceID() != lastSelectionID)
            {
                OnSelectionChanged();
                return;
            }

            int currentHash = GenerateComponentHash(go);
            if (currentHash != lastComponentHash)
            {
                RefreshUI(go);
            }
        }

        private static int GenerateComponentHash(GameObject go)
        {
            var comps = go.GetComponents<Component>();
            unchecked
            {
                int hash = 17;
                foreach (var c in comps)
                {
                    if (c != null)
                    {
                        hash = hash * 23 + c.GetInstanceID();
                        hash = hash * 23 + (int)c.hideFlags;
                    }
                }
                return hash;
            }
        }

        private static void OnSelectionChanged()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                lastSelectionID = 0;
                lastComponentHash = 0;
                focusedComponent = null;
                if (mainContainer != null) mainContainer.Clear();
                return;
            }

            lastSelectionID = go.GetInstanceID();
            focusedComponent = null;
            activeFilters.Clear();

            RestoreComponentVisibility(go);
            InjectToggler(go);
        }

        private static void InjectToggler(GameObject go)
        {
            var inspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            var windows = Resources.FindObjectsOfTypeAll(inspectorWindowType);

            if (windows.Length == 0) return;

            var window = windows[0] as EditorWindow;
            if (window == null) return;

            var root = window.rootVisualElement;

            // Toujours recréer le container pour éviter les problèmes de cache
            if (mainContainer != null && root.Contains(mainContainer))
            {
                root.Remove(mainContainer);
            }

            mainContainer = new VisualElement();
            mainContainer.name = "inspector-component-toggler";
            mainContainer.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            mainContainer.style.paddingTop = 5;
            mainContainer.style.paddingBottom = 5;
            mainContainer.style.paddingLeft = 5;
            mainContainer.style.paddingRight = 5;

            iconsContainer = new VisualElement();
            iconsContainer.style.flexDirection = FlexDirection.Row;
            iconsContainer.style.flexWrap = Wrap.Wrap;
            iconsContainer.style.marginBottom = 5;
            mainContainer.Add(iconsContainer);

            filtersContainer = new VisualElement();
            filtersContainer.style.flexDirection = FlexDirection.Row;
            filtersContainer.style.flexWrap = Wrap.Wrap;
            mainContainer.Add(filtersContainer);

            root.Insert(0, mainContainer);

            RefreshUI(go);
        }

        private static void RefreshUI(GameObject go)
        {
            if (iconsContainer == null || filtersContainer == null) return;

            lastComponentHash = GenerateComponentHash(go);
            RefreshIcons(go);
            RefreshFilters(go);
        }

        private static void RefreshIcons(GameObject go)
        {
            iconsContainer.Clear();
            var components = go.GetComponents<Component>();

            foreach (var comp in components)
            {
                if (comp == null) continue;

                // Container pour l'icône + bouton de copie
                var compContainer = new VisualElement();
                compContainer.style.flexDirection = FlexDirection.Row;
                compContainer.style.marginRight = 4;
                compContainer.style.marginBottom = 2;

                Texture icon = AssetPreview.GetMiniThumbnail(comp);
                if (icon == null) icon = EditorGUIUtility.IconContent("cs Script Icon").image;

                var btn = new Button();
                btn.style.width = 24;
                btn.style.height = 24;
                btn.style.backgroundImage = (Texture2D)icon;
                btn.style.borderTopLeftRadius = 3;
                btn.style.borderTopRightRadius = 3;
                btn.style.borderBottomLeftRadius = 3;
                btn.style.borderBottomRightRadius = 3;

                bool isHidden = (comp.hideFlags & HideFlags.HideInInspector) != 0;
                bool isFocused = focusedComponent == comp;
                bool isCopied = copiedComponent == comp;
                var category = GetComponentCategory(comp);

                if (isFocused)
                {
                    btn.style.borderTopWidth = 2;
                    btn.style.borderBottomWidth = 2;
                    btn.style.borderLeftWidth = 2;
                    btn.style.borderRightWidth = 2;
                    btn.style.borderTopColor = new StyleColor(Color.cyan);
                    btn.style.borderBottomColor = new StyleColor(Color.cyan);
                    btn.style.borderLeftColor = new StyleColor(Color.cyan);
                    btn.style.borderRightColor = new StyleColor(Color.cyan);
                }
                else if (isCopied)
                {
                    btn.style.borderTopWidth = 2;
                    btn.style.borderBottomWidth = 2;
                    btn.style.borderLeftWidth = 2;
                    btn.style.borderRightWidth = 2;
                    btn.style.borderTopColor = new StyleColor(Color.green);
                    btn.style.borderBottomColor = new StyleColor(Color.green);
                    btn.style.borderLeftColor = new StyleColor(Color.green);
                    btn.style.borderRightColor = new StyleColor(Color.green);
                }
                else if (categoryColors.ContainsKey(category))
                {
                    btn.style.borderBottomWidth = 2;
                    btn.style.borderBottomColor = new StyleColor(categoryColors[category]);
                }

                btn.style.opacity = isHidden ? 0.3f : 1.0f;

                string tooltip = $"{comp.GetType().Name} [{category}]";
                if (isHidden) tooltip += " (Hidden)";
                if (isCopied) tooltip += " (Copied)";
                tooltip += "\nClic gauche = toggle visibilité";
                tooltip += "\nClic droit = focus";
                tooltip += "\nClic molette = copy component";
                btn.tooltip = tooltip;

                Component currentComp = comp;
                btn.clicked += () => ToggleVisibility(currentComp);
                btn.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 1) // Clic droit
                    {
                        evt.StopPropagation();
                        FocusOnComponent(currentComp);
                    }
                    else if (evt.button == 2) // Clic molette
                    {
                        evt.StopPropagation();
                        CopyComponent(currentComp);
                        RefreshUI(go);
                    }
                });

                compContainer.Add(btn);
                iconsContainer.Add(compContainer);
            }
        }

        private static void RefreshFilters(GameObject go)
        {
            filtersContainer.Clear();

            var components = go.GetComponents<Component>();
            HashSet<ComponentCategory> presentCategories = new HashSet<ComponentCategory>();
            foreach (var comp in components)
            {
                if (comp != null) presentCategories.Add(GetComponentCategory(comp));
            }

            var categoriesToShow = new[] { ComponentCategory.All, ComponentCategory.Rendering,
                ComponentCategory.Physics, ComponentCategory.Animation, ComponentCategory.Audio,
                ComponentCategory.UI, ComponentCategory.Scripts };

            foreach (var category in categoriesToShow)
            {
                if (category != ComponentCategory.All && !presentCategories.Contains(category))
                    continue;

                var filterBtn = new Button();
                filterBtn.text = category.ToString();
                filterBtn.style.marginRight = 3;
                filterBtn.style.marginBottom = 3;
                filterBtn.style.paddingLeft = 6;
                filterBtn.style.paddingRight = 6;
                filterBtn.style.paddingTop = 2;
                filterBtn.style.paddingBottom = 2;
                filterBtn.style.borderTopLeftRadius = 8;
                filterBtn.style.borderTopRightRadius = 8;
                filterBtn.style.borderBottomLeftRadius = 8;
                filterBtn.style.borderBottomRightRadius = 8;
                filterBtn.style.fontSize = 10;

                bool isActive = activeFilters.Contains(category) ||
                               (category == ComponentCategory.All && activeFilters.Count == 0);

                Color btnColor = categoryColors.ContainsKey(category) ? categoryColors[category] : new Color(0.4f, 0.4f, 0.4f);

                if (isActive)
                {
                    filterBtn.style.backgroundColor = new StyleColor(btnColor);
                    filterBtn.style.color = new StyleColor(Color.white);
                }
                else
                {
                    filterBtn.style.backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f));
                    filterBtn.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                }

                var capturedCategory = category;
                filterBtn.clicked += () => ToggleFilter(capturedCategory, go);
                filtersContainer.Add(filterBtn);
            }

            if (activeFilters.Count > 0)
            {
                var clearBtn = new Button();
                clearBtn.text = "✕";
                clearBtn.style.marginLeft = 5;
                clearBtn.style.paddingLeft = 6;
                clearBtn.style.paddingRight = 6;
                clearBtn.style.paddingTop = 2;
                clearBtn.style.paddingBottom = 2;
                clearBtn.style.borderTopLeftRadius = 8;
                clearBtn.style.borderTopRightRadius = 8;
                clearBtn.style.borderBottomLeftRadius = 8;
                clearBtn.style.borderBottomRightRadius = 8;
                clearBtn.style.fontSize = 10;
                clearBtn.style.backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f));
                clearBtn.style.color = new StyleColor(Color.white);
                clearBtn.tooltip = "Clear filters";
                clearBtn.clicked += () =>
                {
                    activeFilters.Clear();
                    ApplyFilters(go);
                    RefreshUI(go);
                };
                filtersContainer.Add(clearBtn);
            }
        }

        private static void ToggleFilter(ComponentCategory category, GameObject go)
        {
            if (category == ComponentCategory.All)
            {
                activeFilters.Clear();
            }
            else
            {
                if (activeFilters.Contains(category))
                    activeFilters.Remove(category);
                else
                    activeFilters.Add(category);
            }

            ApplyFilters(go);
            RefreshUI(go);
        }

        private static void ApplyFilters(GameObject go)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null || comp is Transform) continue;

                bool shouldBeVisible = activeFilters.Count == 0 || activeFilters.Contains(GetComponentCategory(comp));

                if (shouldBeVisible)
                    comp.hideFlags &= ~HideFlags.HideInInspector;
                else
                    comp.hideFlags |= HideFlags.HideInInspector;
            }

            SaveComponentVisibility(go);
            InternalEditorUtility.RepaintAllViews();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private static void FocusOnComponent(Component comp)
        {
            if (comp == null) return;
            var go = comp.gameObject;

            if (focusedComponent == comp)
            {
                focusedComponent = null;
                RestoreComponentVisibility(go);
            }
            else
            {
                SaveComponentVisibility(go);
                focusedComponent = comp;

                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null) continue;
                    if (c == comp)
                        c.hideFlags &= ~HideFlags.HideInInspector;
                    else
                        c.hideFlags |= HideFlags.HideInInspector;
                }
            }

            InternalEditorUtility.RepaintAllViews();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
            RefreshUI(go);
        }

        private static void ToggleVisibility(Component comp)
        {
            if (comp == null) return;

            Undo.RegisterCompleteObjectUndo(comp, "Toggle Visibility");

            bool isHidden = (comp.hideFlags & HideFlags.HideInInspector) != 0;
            if (isHidden)
                comp.hideFlags &= ~HideFlags.HideInInspector;
            else
                comp.hideFlags |= HideFlags.HideInInspector;

            SaveComponentVisibility(comp.gameObject);
            InternalEditorUtility.RepaintAllViews();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
            RefreshUI(comp.gameObject);
        }

        public static void CopyComponent(Component comp)
        {
            if (comp == null) return;

            ComponentUtility.CopyComponent(comp);
            copiedComponent = comp;
        }

        public static void PasteComponentValues(Component targetComp)
        {
            if (targetComp == null || copiedComponent == null) return;

            Undo.RegisterCompleteObjectUndo(targetComp, "Paste Component Values");
            ComponentUtility.PasteComponentValues(targetComp);
        }

        public static void PasteComponentAsNew(GameObject go)
        {
            if (go == null || copiedComponent == null) return;

            Undo.RegisterCompleteObjectUndo(go, "Paste Component As New");
            ComponentUtility.PasteComponentAsNew(go);
            RefreshUI(go);
        }
    }
}
