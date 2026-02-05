#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection.Editor
{
    /// <summary>
    /// Visual dependency graph window with node-based layout.
    /// </summary>
    public class DependencyGraphWindow : EditorWindow
    {
        // Node appearance
        private const float NodeWidth = 150f;
        private const float NodeHeight = 40f;
        private const float NodeSpacingX = 200f;
        private const float NodeSpacingY = 80f;

        // Colors
        private static readonly Color BackgroundColor = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color GridColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        private static readonly Color NodeColor = new Color(0.25f, 0.25f, 0.28f);
        private static readonly Color NodeBorderColor = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color ServiceNodeColor = new Color(0.2f, 0.5f, 0.3f);
        private static readonly Color MonoNodeColor = new Color(0.5f, 0.35f, 0.15f);
        private static readonly Color ConnectionColor = new Color(0.4f, 0.7f, 0.9f, 0.8f);
        private static readonly Color HighlightColor = new Color(0.3f, 0.8f, 0.5f);

        // Data
        private Dictionary<Type, HashSet<Type>> _dependencyGraph;
        private Dictionary<Type, Rect> _nodePositions = new Dictionary<Type, Rect>();
        private List<Type> _allTypes = new List<Type>();
        private HashSet<Type> _monoTypes = new HashSet<Type>();
        
        // Interaction
        private Vector2 _panOffset = Vector2.zero;
        private Vector2 _dragStart;
        private bool _isPanning;
        private Type _selectedNode;
        private float _zoom = 1f;

        // Styles
        private GUIStyle _nodeStyle;
        private GUIStyle _nodeLabelStyle;
        private bool _stylesInitialized;

        [MenuItem("EthanToolBox/Injection/Dependency Graph")]
        public static void ShowWindow()
        {
            var window = GetWindow<DependencyGraphWindow>("Dependency Graph");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        public static void ShowWithData(Dictionary<Type, HashSet<Type>> graph, HashSet<Type> monoTypes)
        {
            var window = GetWindow<DependencyGraphWindow>("Dependency Graph");
            window._dependencyGraph = graph;
            window._monoTypes = monoTypes ?? new HashSet<Type>();
            window.BuildLayout();
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (Application.isPlaying) RefreshData();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RefreshData();
            }
        }

        private void RefreshData()
        {
            var root = FindFirstObjectByType<DICompositionRoot>();
            if (root == null) return;

            var container = root.Container;
            if (container == null) return;

            _dependencyGraph = container.DependencyGraph;
            _monoTypes.Clear();
            
            foreach (var type in container.GetAllRegisteredTypes())
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    _monoTypes.Add(type);
            }

            BuildLayout();
            Repaint();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _nodeStyle = new GUIStyle("flow node 0")
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _nodeLabelStyle = new GUIStyle(EditorStyles.whiteBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                wordWrap = false
            };

            _stylesInitialized = true;
        }

        private void BuildLayout()
        {
            if (_dependencyGraph == null) return;

            _nodePositions.Clear();
            _allTypes.Clear();

            // Collect all types
            foreach (var kvp in _dependencyGraph)
            {
                if (!_allTypes.Contains(kvp.Key)) _allTypes.Add(kvp.Key);
                foreach (var dep in kvp.Value)
                {
                    if (!_allTypes.Contains(dep)) _allTypes.Add(dep);
                }
            }

            // Simple grid layout with dependency-aware ordering
            var roots = _allTypes.Where(t => !_dependencyGraph.Values.Any(set => set.Contains(t))).ToList();
            var leaves = _allTypes.Except(roots).ToList();

            float startX = 50f;
            float startY = 50f;
            int col = 0;
            int row = 0;
            int maxCols = Mathf.Max(3, (int)Mathf.Sqrt(_allTypes.Count));

            // Place roots first (top rows)
            foreach (var type in roots)
            {
                _nodePositions[type] = new Rect(startX + col * NodeSpacingX, startY + row * NodeSpacingY, NodeWidth, NodeHeight);
                col++;
                if (col >= maxCols) { col = 0; row++; }
            }

            row++; col = 0;

            // Then leaves
            foreach (var type in leaves)
            {
                if (_nodePositions.ContainsKey(type)) continue;
                _nodePositions[type] = new Rect(startX + col * NodeSpacingX, startY + row * NodeSpacingY, NodeWidth, NodeHeight);
                col++;
                if (col >= maxCols) { col = 0; row++; }
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // Background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BackgroundColor);
            DrawGrid();

            // Toolbar
            DrawToolbar();

            // Handle input
            HandleInput();

            // Draw connections first (behind nodes)
            if (_dependencyGraph != null)
            {
                foreach (var kvp in _dependencyGraph)
                {
                    if (!_nodePositions.ContainsKey(kvp.Key)) continue;
                    foreach (var dep in kvp.Value)
                    {
                        if (!_nodePositions.ContainsKey(dep)) continue;
                        DrawConnection(kvp.Key, dep);
                    }
                }
            }

            // Draw nodes
            foreach (var kvp in _nodePositions)
            {
                DrawNode(kvp.Key, kvp.Value);
            }

            // Info panel
            if (!Application.isPlaying)
            {
                EditorGUI.HelpBox(new Rect(10, position.height - 40, 300, 30), 
                    "Enter Play Mode to see the dependency graph", MessageType.Info);
            }
            else if (_nodePositions.Count == 0)
            {
                EditorGUI.HelpBox(new Rect(10, position.height - 40, 300, 30),
                    "No dependencies detected. Services may not have resolved yet.", MessageType.Warning);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshData();
            }

            if (GUILayout.Button("Reset View", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                _panOffset = Vector2.zero;
                _zoom = 1f;
            }

            if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                BuildLayout();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label($"Nodes: {_nodePositions.Count}", EditorStyles.toolbarButton);
            GUILayout.Label($"Zoom: {_zoom:F1}x", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            float gridSize = 50f * _zoom;
            int widthDivs = Mathf.CeilToInt(position.width / gridSize);
            int heightDivs = Mathf.CeilToInt(position.height / gridSize);

            Handles.BeginGUI();
            Handles.color = GridColor;

            float offsetX = _panOffset.x % gridSize;
            float offsetY = _panOffset.y % gridSize;

            for (int i = 0; i <= widthDivs; i++)
            {
                float x = i * gridSize + offsetX;
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, position.height));
            }

            for (int j = 0; j <= heightDivs; j++)
            {
                float y = j * gridSize + offsetY + 20; // Offset for toolbar
                Handles.DrawLine(new Vector3(0, y), new Vector3(position.width, y));
            }

            Handles.EndGUI();
        }

        private void DrawNode(Type type, Rect baseRect)
        {
            Rect rect = new Rect(
                baseRect.x * _zoom + _panOffset.x,
                baseRect.y * _zoom + _panOffset.y + 20, // Toolbar offset
                baseRect.width * _zoom,
                baseRect.height * _zoom
            );

            // Node background
            bool isMono = _monoTypes.Contains(type);
            bool isSelected = _selectedNode == type;
            Color nodeCol = isMono ? MonoNodeColor : ServiceNodeColor;
            if (isSelected) nodeCol = HighlightColor;

            EditorGUI.DrawRect(rect, nodeCol);
            
            // Border
            Handles.BeginGUI();
            Handles.color = isSelected ? Color.white : NodeBorderColor;
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, isSelected ? Color.white : NodeBorderColor);
            Handles.EndGUI();

            // Label
            string label = type.Name;
            if (label.Length > 18) label = label.Substring(0, 15) + "...";
            
            GUI.Label(rect, label, _nodeLabelStyle);

            // Icon indicator
            string icon = isMono ? "🎮" : "⚙";
            GUI.Label(new Rect(rect.x + 5, rect.y + 2, 20, 20), icon);

            // Click detection
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedNode = type;
                Event.current.Use();
                Repaint();
            }
        }

        private void DrawConnection(Type from, Type to)
        {
            if (!_nodePositions.ContainsKey(from) || !_nodePositions.ContainsKey(to)) return;

            Rect fromRect = _nodePositions[from];
            Rect toRect = _nodePositions[to];

            Vector2 start = new Vector2(
                fromRect.x * _zoom + _panOffset.x + (fromRect.width * _zoom) / 2,
                fromRect.y * _zoom + _panOffset.y + 20 + (fromRect.height * _zoom)
            );

            Vector2 end = new Vector2(
                toRect.x * _zoom + _panOffset.x + (toRect.width * _zoom) / 2,
                toRect.y * _zoom + _panOffset.y + 20
            );

            // Bezier curve
            Handles.BeginGUI();
            
            bool isHighlighted = _selectedNode == from || _selectedNode == to;
            Handles.color = isHighlighted ? HighlightColor : ConnectionColor;
            
            float tangentStrength = Mathf.Abs(end.y - start.y) * 0.5f;
            Vector2 startTangent = start + Vector2.down * tangentStrength;
            Vector2 endTangent = end + Vector2.up * tangentStrength;

            Handles.DrawBezier(start, end, startTangent, endTangent, 
                isHighlighted ? HighlightColor : ConnectionColor, 
                null, 
                isHighlighted ? 3f : 2f);

            // Arrow head
            Vector2 dir = (end - endTangent).normalized;
            Vector2 arrow1 = end - dir * 10 + new Vector2(-dir.y, dir.x) * 5;
            Vector2 arrow2 = end - dir * 10 + new Vector2(dir.y, -dir.x) * 5;
            
            Handles.DrawLine(end, arrow1);
            Handles.DrawLine(end, arrow2);

            Handles.EndGUI();
        }

        private void HandleInput()
        {
            Event e = Event.current;

            // Pan with middle mouse or right mouse
            if (e.type == EventType.MouseDown && (e.button == 2 || e.button == 1))
            {
                _isPanning = true;
                _dragStart = e.mousePosition;
                e.Use();
            }

            if (e.type == EventType.MouseDrag && _isPanning)
            {
                _panOffset += e.mousePosition - _dragStart;
                _dragStart = e.mousePosition;
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp && (e.button == 2 || e.button == 1))
            {
                _isPanning = false;
                e.Use();
            }

            // Zoom with scroll wheel
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -e.delta.y * 0.05f;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.3f, 2f);
                e.Use();
                Repaint();
            }

            // Deselect with escape
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _selectedNode = null;
                e.Use();
                Repaint();
            }
        }
    }
}
#endif
