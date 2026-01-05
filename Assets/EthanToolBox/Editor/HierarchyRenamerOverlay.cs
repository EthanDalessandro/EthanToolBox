using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace EthanToolBox.Editor
{
    [InitializeOnLoad]
    public class HierarchyRenamerOverlay
    {
        private static bool isOverlayVisible = false;
        private static VisualElement overlayContainer;
        private static TextField prefixField;
        private static IntegerField startIndexField;
        private static Button renameButton;

        private static string prefix = "GameObject";
        private static int startIndex = 0;

        static HierarchyRenamerOverlay()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.delayCall += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            switch (selectedObjects.Length > 1)
            {
                case true when !isOverlayVisible:
                    ShowOverlay();
                    break;
                case false when isOverlayVisible:
                    HideOverlay();
                    break;
            }
        }
        
        /// <summary>
        /// Locates the internal Hierarchy window via Reflection and injects the renaming overlay into its root VisualElement.
        /// Ensures the overlay is instantiated and sets its display style to Flex.
        /// </summary>
        private static void ShowOverlay()
        {
            Type hierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            Object[] windows = Resources.FindObjectsOfTypeAll(hierarchyWindowType);

            if (windows.Length == 0) return;

            EditorWindow window = windows[0] as EditorWindow;
            if (window == null) return;

            PropertyInfo rootInfo = window.GetType().GetProperty("rootVisualElement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (rootInfo == null) return;

            VisualElement root = window.rootVisualElement;

            if (root == null) return;

            if (overlayContainer == null)
            {
                CreateOverlay();
            }

            if (!root.Contains(overlayContainer))
            {
                root.Add(overlayContainer);
            }

            if (overlayContainer != null) overlayContainer.style.display = DisplayStyle.Flex;
            isOverlayVisible = true;
        }

        private static void HideOverlay()
        {
            if (overlayContainer != null)
            {
                overlayContainer.style.display = DisplayStyle.None;
            }
            isOverlayVisible = false;
        }

        private static void CreateOverlay()
        {
            overlayContainer = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    bottom = 10,
                    right = 20,
                    backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.9f)),
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                    paddingTop = 5,
                    paddingBottom = 5,
                    paddingLeft = 5,
                    paddingRight = 5,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new StyleColor(Color.black),
                    borderBottomColor = new StyleColor(Color.black),
                    borderLeftColor = new StyleColor(Color.black),
                    borderRightColor = new StyleColor(Color.black),
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            prefixField = new TextField
            {
                value = prefix
            };
            prefixField.RegisterValueChangedCallback(evt => prefix = evt.newValue);
            prefixField.style.width = 100;
            prefixField.style.marginRight = 5;
            overlayContainer.Add(prefixField);

            startIndexField = new IntegerField
            {
                value = startIndex
            };
            startIndexField.RegisterValueChangedCallback(evt => startIndex = evt.newValue);
            startIndexField.style.width = 40;
            startIndexField.style.marginRight = 5;
            overlayContainer.Add(startIndexField);

            renameButton = new Button(RenameSelectedObjects)
            {
                text = "Rename"
            };
            overlayContainer.Add(renameButton);
        }

        private static void RenameSelectedObjects()
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length < 1) return;

            GameObject[] sortedObjects = selectedObjects.OrderBy(go => go.transform.GetSiblingIndex()).ToArray();

            Undo.RecordObjects(sortedObjects, "Bulk Rename Objects");

            for (int i = 0; i < sortedObjects.Length; i++)
            {
                sortedObjects[i].name = $"{prefix}{startIndex + i}";
            }

            Debug.Log($"[Hierarchy Renamer] Renamed {sortedObjects.Length} objects.");
        }
    }
}
