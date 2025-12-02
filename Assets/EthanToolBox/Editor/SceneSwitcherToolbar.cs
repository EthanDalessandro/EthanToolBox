using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;

namespace EthanToolBox.Editor
{
    [InitializeOnLoad]
    public static class SceneSwitcherToolbar
    {
        private static ScriptableObject _toolbar;
        private static VisualElement _root;
        private static ToolbarButton _sceneButton;

        static SceneSwitcherToolbar()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (_toolbar == null)
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type toolbarType = editorAssembly.GetType("UnityEditor.Toolbar");

                if (toolbarType != null)
                {
                    UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
                    if (toolbars.Length > 0)
                    {
                        _toolbar = (ScriptableObject)toolbars[0];
                    }
                }
            }

            if (_toolbar != null && _root == null)
            {
                Type toolbarType = _toolbar.GetType();
                FieldInfo rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);

                if (rootField != null)
                {
                    _root = rootField.GetValue(_toolbar) as VisualElement;
                }
            }

            if (_root != null)
            {
                if (_sceneButton == null || _sceneButton.parent == null)
                {
                    CreateToolbarButton();
                }
                else
                {
                    string currentScene = SceneManager.GetActiveScene().name;
                    if (string.IsNullOrEmpty(currentScene)) currentScene = "Unsaved Scene";

                    if (_sceneButton.text != currentScene)
                    {
                        _sceneButton.text = currentScene;
                    }
                }
            }
        }

        private static void CreateToolbarButton()
        {
            VisualElement playModeZone = _root.Q("ToolbarZonePlayMode");

            if (playModeZone == null)
            {
                playModeZone = _root.Q("ToolbarZoneLeftAlign");
            }

            if (playModeZone != null)
            {
                _sceneButton = new ToolbarButton(ShowScenePopup);
                _sceneButton.name = "SceneSwitcherBtn";
                _sceneButton.text = SceneManager.GetActiveScene().name;
                _sceneButton.tooltip = "Switch Scene";

                // Styling
                _sceneButton.style.width = 150;
                _sceneButton.style.unityTextAlign = TextAnchor.MiddleCenter;
                _sceneButton.style.marginRight = 10;

                Color normalColor = new Color(0.25f, 0.25f, 0.25f, 0.2f);
                Color hoverColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);

                _sceneButton.style.backgroundColor = new StyleColor(normalColor);
                _sceneButton.style.borderTopWidth = 1;
                _sceneButton.style.borderBottomWidth = 1;
                _sceneButton.style.borderLeftWidth = 1;
                _sceneButton.style.borderRightWidth = 1;
                _sceneButton.style.borderTopColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
                _sceneButton.style.borderBottomColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
                _sceneButton.style.borderLeftColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
                _sceneButton.style.borderRightColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
                _sceneButton.style.borderTopLeftRadius = 3;
                _sceneButton.style.borderTopRightRadius = 3;
                _sceneButton.style.borderBottomLeftRadius = 3;
                _sceneButton.style.borderBottomRightRadius = 3;
                _sceneButton.style.paddingLeft = 6;

                _sceneButton.RegisterCallback<MouseEnterEvent>(evt => _sceneButton.style.backgroundColor = new StyleColor(hoverColor));
                _sceneButton.RegisterCallback<MouseLeaveEvent>(evt => _sceneButton.style.backgroundColor = new StyleColor(normalColor));

                if (playModeZone.parent != null)
                {
                    int index = playModeZone.parent.IndexOf(playModeZone);
                    playModeZone.parent.Insert(index, _sceneButton);
                }
                else
                {
                    playModeZone.Add(_sceneButton);
                }
            }
        }

        private static void ShowScenePopup()
        {
            Rect buttonRect = _sceneButton.worldBound;
            Rect guiRect = new Rect(buttonRect.x, buttonRect.y + 20, buttonRect.width, buttonRect.height);
            UnityEditor.PopupWindow.Show(guiRect, new SceneSwitcherPopup());
        }
    }

    public class SceneSwitcherPopup : PopupWindowContent
    {
        private class SceneNode
        {
            public string Name;
            public string Path;
            public List<SceneNode> Children = new List<SceneNode>();
            public bool IsFolder;
            public bool IsExpanded = true;
        }

        private SceneNode _rootNode;
        private Vector2 _scrollPosition;
        private const float Width = 300;
        private const float Height = 400;

        public override Vector2 GetWindowSize()
        {
            return new Vector2(Width, Height);
        }

        public override void OnOpen()
        {
            BuildTree();
        }

        private void BuildTree()
        {
            _rootNode = new SceneNode { Name = "Root", IsFolder = true };
            string[] guids = AssetDatabase.FindAssets("t:Scene");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                string relativePath = path.Replace("Assets/", "");
                string[] parts = relativePath.Split('/');

                SceneNode currentNode = _rootNode;

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    bool isFile = i == parts.Length - 1;

                    SceneNode child = currentNode.Children.FirstOrDefault(c => c.Name == part);

                    if (child == null)
                    {
                        child = new SceneNode
                        {
                            Name = part,
                            IsFolder = !isFile,
                            Path = isFile ? path : null
                        };
                        currentNode.Children.Add(child);
                    }

                    currentNode = child;
                }
            }

            SortTree(_rootNode);
        }

        private void SortTree(SceneNode node)
        {
            node.Children = node.Children
                .OrderByDescending(c => c.IsFolder)
                .ThenBy(c => c.Name)
                .ToList();

            foreach (var child in node.Children)
            {
                if (child.IsFolder) SortTree(child);
            }
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginArea(new Rect(0, 0, Width, Height));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawNode(_rootNode, 0);

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNode(SceneNode node, int indentLevel)
        {
            if (node == _rootNode)
            {
                foreach (var child in node.Children) DrawNode(child, indentLevel);
                return;
            }

            EditorGUI.indentLevel = indentLevel;

            if (node.IsFolder)
            {
                node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, node.Name, true);
                if (node.IsExpanded)
                {
                    foreach (var child in node.Children)
                    {
                        DrawNode(child, indentLevel + 1);
                    }
                }
            }
            else
            {
                // Scene Node
                Rect rowRect = EditorGUILayout.GetControlRect(false, 18);

                // Hover effect
                if (rowRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rowRect, new Color(0.35f, 0.35f, 0.35f, 1f)); // Hover background
                }

                // Layout calculations
                float indent = indentLevel * 15;
                float addButtonWidth = 25;

                Rect labelRect = new Rect(rowRect.x + indent, rowRect.y, rowRect.width - indent - addButtonWidth, rowRect.height);
                Rect addRect = new Rect(rowRect.x + rowRect.width - addButtonWidth, rowRect.y, addButtonWidth, rowRect.height);

                string displayName = node.Name.Replace(".unity", "");

                // Main Open Button
                if (GUI.Button(labelRect, displayName, EditorStyles.label))
                {
                    OpenScene(node.Path, OpenSceneMode.Single);
                    editorWindow.Close();
                }

                // Additive Load Button (+)
                if (GUI.Button(addRect, "+"))
                {
                    OpenScene(node.Path, OpenSceneMode.Additive);
                    editorWindow.Close();
                }
            }
        }

        private void OpenScene(string path, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single)
            {
                if (SceneManager.GetActiveScene().path != path)
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(path);
                    }
                }
            }
            else
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
        }
    }
}
