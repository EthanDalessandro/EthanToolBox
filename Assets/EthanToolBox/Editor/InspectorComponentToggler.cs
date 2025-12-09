using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace EthanToolBox.Editor
{
    [InitializeOnLoad]
    public class InspectorComponentToggler
    {
        private static VisualElement togglerContainer;
        private static int lastComponentHash = 0;
        private static int lastSelectionID = 0;

        static InspectorComponentToggler()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.delayCall += OnSelectionChanged;
            EditorApplication.update += OnUpdate;
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
                RefreshIcons(go);
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
                if (togglerContainer != null) togglerContainer.Clear();
                return;
            }

            lastSelectionID = go.GetInstanceID();
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

            if (togglerContainer == null || !root.Contains(togglerContainer))
            {
                togglerContainer = new VisualElement();
                togglerContainer.style.flexDirection = FlexDirection.Row;
                togglerContainer.style.flexWrap = Wrap.Wrap;
                togglerContainer.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
                togglerContainer.style.paddingTop = 5;
                togglerContainer.style.paddingBottom = 5;
                togglerContainer.style.paddingLeft = 5;
                togglerContainer.style.paddingRight = 5;
                root.Insert(0, togglerContainer);
            }

            RefreshIcons(go);
        }

        private static void RefreshIcons(GameObject go)
        {
            lastComponentHash = GenerateComponentHash(go);
            togglerContainer.Clear();

            var components = go.GetComponents<Component>();

            foreach (var comp in components)
            {
                if (comp == null) continue;

                Texture icon = AssetPreview.GetMiniThumbnail(comp);
                if (icon == null) icon = EditorGUIUtility.IconContent("cs Script Icon").image;

                Button btn = new Button(() => ToggleVisibility(comp));
                btn.style.width = 24;
                btn.style.height = 24;
                btn.style.marginRight = 2;
                btn.style.backgroundImage = (Texture2D)icon;

                bool isHidden = (comp.hideFlags & HideFlags.HideInInspector) != 0;
                btn.style.opacity = isHidden ? 0.3f : 1.0f;
                btn.tooltip = isHidden ? $"{comp.GetType().Name} (Hidden)" : comp.GetType().Name;

                togglerContainer.Add(btn);
            }
        }

        private static void ToggleVisibility(Component comp)
        {
            if (comp == null) return;

            Undo.RegisterCompleteObjectUndo(comp, "Toggle Visibility"); // Support Undo

            bool isHidden = (comp.hideFlags & HideFlags.HideInInspector) != 0;

            if (isHidden)
            {
                comp.hideFlags &= ~HideFlags.HideInInspector;
            }
            else
            {
                comp.hideFlags |= HideFlags.HideInInspector;
            }

            InternalEditorUtility.RepaintAllViews();

            var tracker = ActiveEditorTracker.sharedTracker;
            tracker.ForceRebuild();

            RefreshIcons(comp.gameObject);
        }
    }
}
