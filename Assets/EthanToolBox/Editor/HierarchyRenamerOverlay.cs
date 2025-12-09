using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
            var selectedObjects = Selection.gameObjects;
            bool shouldShow = selectedObjects.Length > 1;

            if (shouldShow && !isOverlayVisible)
            {
                ShowOverlay();
            }
            else if (!shouldShow && isOverlayVisible)
            {
                HideOverlay();
            }
        }

        private static void ShowOverlay()
        {
            var hierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var windows = Resources.FindObjectsOfTypeAll(hierarchyWindowType);

            if (windows.Length == 0) return;

            var window = windows[0] as EditorWindow;
            if (window == null) return;

            var rootInfo = window.GetType().GetProperty("rootVisualElement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (rootInfo == null) return;

            var root = window.rootVisualElement;

            if (root == null) return;

            if (overlayContainer == null)
            {
                CreateOverlay();
            }

            if (!root.Contains(overlayContainer))
            {
                root.Add(overlayContainer);
            }

            overlayContainer.style.display = DisplayStyle.Flex;
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
            overlayContainer = new VisualElement();
            overlayContainer.style.position = Position.Absolute;
            overlayContainer.style.bottom = 10;
            overlayContainer.style.right = 20;
            overlayContainer.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.9f));
            overlayContainer.style.borderTopLeftRadius = 5;
            overlayContainer.style.borderTopRightRadius = 5;
            overlayContainer.style.borderBottomLeftRadius = 5;
            overlayContainer.style.borderBottomRightRadius = 5;
            overlayContainer.style.paddingTop = 5;
            overlayContainer.style.paddingBottom = 5;
            overlayContainer.style.paddingLeft = 5;
            overlayContainer.style.paddingRight = 5;
            overlayContainer.style.borderTopWidth = 1;
            overlayContainer.style.borderBottomWidth = 1;
            overlayContainer.style.borderLeftWidth = 1;
            overlayContainer.style.borderRightWidth = 1;
            overlayContainer.style.borderTopColor = new StyleColor(Color.black);
            overlayContainer.style.borderBottomColor = new StyleColor(Color.black);
            overlayContainer.style.borderLeftColor = new StyleColor(Color.black);
            overlayContainer.style.borderRightColor = new StyleColor(Color.black);
            overlayContainer.style.flexDirection = FlexDirection.Row;
            overlayContainer.style.alignItems = Align.Center;

            prefixField = new TextField();
            prefixField.value = prefix;
            prefixField.RegisterValueChangedCallback(evt => prefix = evt.newValue);
            prefixField.style.width = 100;
            prefixField.style.marginRight = 5;
            overlayContainer.Add(prefixField);

            startIndexField = new IntegerField();
            startIndexField.value = startIndex;
            startIndexField.RegisterValueChangedCallback(evt => startIndex = evt.newValue);
            startIndexField.style.width = 40;
            startIndexField.style.marginRight = 5;
            overlayContainer.Add(startIndexField);

            renameButton = new Button(RenameSelectedObjects);
            renameButton.text = "Rename";
            overlayContainer.Add(renameButton);
        }

        private static void RenameSelectedObjects()
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length < 1) return;

            // Sort by sibling index
            var sortedObjects = selectedObjects.OrderBy(go => go.transform.GetSiblingIndex()).ToArray();

            Undo.RecordObjects(sortedObjects, "Bulk Rename Objects");

            for (int i = 0; i < sortedObjects.Length; i++)
            {
                sortedObjects[i].name = $"{prefix}{startIndex + i}";
            }

            Debug.Log($"[Hierarchy Renamer] Renamed {sortedObjects.Length} objects.");
        }
    }
}
