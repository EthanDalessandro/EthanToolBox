#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection.Editor
{
    /// <summary>
    /// Professional visual dependency graph window with node-based layout.
    /// </summary>
    public class DependencyGraphWindow : EditorWindow
    {
        // Node appearance
        private const float NodeWidth = 160f;
        private const float NodeHeight = 50f;
        private const float NodeSpacingX = 220f;
        private const float NodeSpacingY = 100f;

        // Colors - Professional dark theme
        private static readonly Color BackgroundColor = new Color(0.118f, 0.118f, 0.149f);
        private static readonly Color GridColorMinor = new Color(0.18f, 0.18f, 0.22f, 0.4f);
        private static readonly Color GridColorMajor = new Color(0.22f, 0.22f, 0.28f, 0.6f);
        
        private static readonly Color ServiceNodeColor = new Color(0.22f, 0.45f, 0.35f);
        private static readonly Color MonoNodeColor = new Color(0.5f, 0.35f, 0.18f);
        private static readonly Color NodeBorderColor = new Color(0.35f, 0.35f, 0.4f);
        private static readonly Color SelectedBorderColor = new Color(0.4f, 0.8f, 0.5f);
        
        private static readonly Color ConnectionColor = new Color(0.45f, 0.65f, 0.85f, 0.7f);
        private static readonly Color HighlightConnectionColor = new Color(0.4f, 0.9f, 0.6f, 1f);
        
        private static readonly Color StatsBackgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        private static readonly Color MinimapBgColor = new Color(0.08f, 0.08f, 0.1f, 0.9f);

        // Data
        private Dictionary<Type, HashSet<Type>> _dependencyGraph;
        private Dictionary<Type, Rect> _nodePositions = new Dictionary<Type, Rect>();
        private Dictionary<Type, NodeData> _nodeData = new Dictionary<Type, NodeData>();
        private List<Type> _allTypes = new List<Type>();
        private HashSet<Type> _monoTypes = new HashSet<Type>();
        
        // Interaction
        private Vector2 _panOffset = Vector2.zero;
        private Vector2 _dragStart;
        private bool _isPanning;
        private Type _selectedNode;
        private Type _hoveredNode;
        private float _zoom = 1f;
        private Type _draggingNode;

        // UI State
        private bool _showMinimap = true;
        private bool _showStats = true;
        private string _searchFilter = "";
        private Vector2 _statsScrollPos;

        // Styles
        private GUIStyle _nodeLabelStyle;
        private GUIStyle _nodeSubLabelStyle;
        private GUIStyle _tooltipStyle;
        private GUIStyle _statsLabelStyle;
        private bool _stylesInitialized;

        private class NodeData
        {
            public int IncomingCount;
            public int OutgoingCount;
            public bool IsHighlighted;
        }

        [MenuItem("EthanToolBox/Injection/Dependency Graph")]
        public static void ShowWindow()
        {
            var window = GetWindow<DependencyGraphWindow>("Dependency Graph");
            window.minSize = new Vector2(700, 500);
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

            _nodeLabelStyle = new GUIStyle(EditorStyles.whiteBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                wordWrap = false,
                clipping = TextClipping.Clip
            };

            _nodeSubLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            _tooltipStyle = new GUIStyle("Tooltip")
            {
                fontSize = 11,
                padding = new RectOffset(8, 8, 5, 5)
            };

            _statsLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            _stylesInitialized = true;
        }

        private void BuildLayout()
        {
            if (_dependencyGraph == null) return;

            _nodePositions.Clear();
            _nodeData.Clear();
            _allTypes.Clear();

            // Collect all types and build node data
            foreach (var kvp in _dependencyGraph)
            {
                if (!_allTypes.Contains(kvp.Key)) _allTypes.Add(kvp.Key);
                foreach (var dep in kvp.Value)
                {
                    if (!_allTypes.Contains(dep)) _allTypes.Add(dep);
                }
            }

            // Build node data
            foreach (var type in _allTypes)
            {
                var data = new NodeData();
                data.OutgoingCount = _dependencyGraph.ContainsKey(type) ? _dependencyGraph[type].Count : 0;
                data.IncomingCount = _dependencyGraph.Values.Count(set => set.Contains(type));
                _nodeData[type] = data;
            }

            // Hierarchical layout - roots at top, dependencies below
            var roots = _allTypes.Where(t => _nodeData[t].IncomingCount == 0).OrderByDescending(t => _nodeData[t].OutgoingCount).ToList();
            var middle = _allTypes.Where(t => _nodeData[t].IncomingCount > 0 && _nodeData[t].OutgoingCount > 0).ToList();
            var leaves = _allTypes.Where(t => _nodeData[t].OutgoingCount == 0 && _nodeData[t].IncomingCount > 0).ToList();

            float startX = 80f;
            float startY = 80f;
            int row = 0;

            // Place roots
            PlaceRow(roots, startX, startY + row * NodeSpacingY);
            row++;

            // Place middle tier
            if (middle.Count > 0)
            {
                PlaceRow(middle, startX, startY + row * NodeSpacingY);
                row++;
            }

            // Place leaves
            if (leaves.Count > 0)
            {
                PlaceRow(leaves, startX, startY + row * NodeSpacingY);
            }

            // Place any remaining
            var placed = new HashSet<Type>(_nodePositions.Keys);
            var remaining = _allTypes.Where(t => !placed.Contains(t)).ToList();
            if (remaining.Count > 0)
            {
                row++;
                PlaceRow(remaining, startX, startY + row * NodeSpacingY);
            }
        }

        private void PlaceRow(List<Type> types, float startX, float y)
        {
            float x = startX;
            foreach (var type in types)
            {
                _nodePositions[type] = new Rect(x, y, NodeWidth, NodeHeight);
                x += NodeSpacingX;
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

            // Handle input first
            HandleInput();

            // Update hover state
            UpdateHoverState();

            // Draw connections (behind nodes)
            DrawAllConnections();

            // Draw nodes
            foreach (var kvp in _nodePositions)
            {
                DrawNode(kvp.Key, kvp.Value);
            }

            // Draw overlays
            if (_showMinimap) DrawMinimap();
            if (_showStats) DrawStatsPanel();
            DrawTooltip();

            // Status bar
            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("⟳ Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshData();
            }

            if (GUILayout.Button("⊙ Reset View", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                _panOffset = Vector2.zero;
                _zoom = 1f;
            }

            if (GUILayout.Button("⊞ Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(85)))
            {
                BuildLayout();
            }

            GUILayout.Space(10);
            
            _showMinimap = GUILayout.Toggle(_showMinimap, "Minimap", EditorStyles.toolbarButton, GUILayout.Width(65));
            _showStats = GUILayout.Toggle(_showStats, "Stats", EditorStyles.toolbarButton, GUILayout.Width(50));

            GUILayout.FlexibleSpace();

            // Search
            GUILayout.Label("🔍", GUILayout.Width(20));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(120));

            GUILayout.Space(5);
            GUILayout.Label($"{_nodePositions.Count} nodes", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            float gridSizeMinor = 25f * _zoom;
            float gridSizeMajor = 100f * _zoom;

            Handles.BeginGUI();

            // Minor grid
            Handles.color = GridColorMinor;
            DrawGridLines(gridSizeMinor);

            // Major grid
            Handles.color = GridColorMajor;
            DrawGridLines(gridSizeMajor);

            Handles.EndGUI();
        }

        private void DrawGridLines(float gridSize)
        {
            float offsetX = _panOffset.x % gridSize;
            float offsetY = _panOffset.y % gridSize;

            for (float x = offsetX; x < position.width; x += gridSize)
            {
                Handles.DrawLine(new Vector3(x, 20), new Vector3(x, position.height));
            }

            for (float y = offsetY + 20; y < position.height; y += gridSize)
            {
                Handles.DrawLine(new Vector3(0, y), new Vector3(position.width, y));
            }
        }

        private void DrawNode(Type type, Rect baseRect)
        {
            Rect rect = GetScreenRect(baseRect);

            // Off-screen culling
            if (rect.xMax < 0 || rect.xMin > position.width || rect.yMax < 20 || rect.yMin > position.height)
                return;

            bool isMono = _monoTypes.Contains(type);
            bool isSelected = _selectedNode == type;
            bool isHovered = _hoveredNode == type;
            bool matchesSearch = string.IsNullOrEmpty(_searchFilter) || type.Name.ToLower().Contains(_searchFilter.ToLower());

            // Node background with rounded corners effect
            Color nodeCol = isMono ? MonoNodeColor : ServiceNodeColor;
            if (!matchesSearch) nodeCol *= 0.4f;
            if (isHovered && !isSelected) nodeCol = Color.Lerp(nodeCol, Color.white, 0.15f);
            
            // Shadow
            EditorGUI.DrawRect(new Rect(rect.x + 3, rect.y + 3, rect.width, rect.height), new Color(0, 0, 0, 0.3f));
            
            // Main rect
            EditorGUI.DrawRect(rect, nodeCol);

            // Border
            DrawNodeBorder(rect, isSelected ? SelectedBorderColor : (isHovered ? Color.white : NodeBorderColor), isSelected ? 2f : 1f);

            // Icon badge
            string icon = isMono ? "🎮" : "⚙";
            GUI.Label(new Rect(rect.x + 6, rect.y + 4, 20, 16), icon);

            // Labels
            string label = type.Name;
            if (label.Length > 16) label = label.Substring(0, 13) + "...";
            
            Rect labelRect = new Rect(rect.x, rect.y + 8, rect.width, 20);
            GUI.Label(labelRect, label, _nodeLabelStyle);

            // Sub-label (connection info)
            if (_nodeData.TryGetValue(type, out var data))
            {
                string subLabel = $"↓{data.IncomingCount} ↑{data.OutgoingCount}";
                Rect subRect = new Rect(rect.x, rect.y + rect.height - 18, rect.width, 16);
                GUI.Label(subRect, subLabel, _nodeSubLabelStyle);
            }
        }

        private void DrawNodeBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void DrawAllConnections()
        {
            if (_dependencyGraph == null) return;

            Handles.BeginGUI();

            foreach (var kvp in _dependencyGraph)
            {
                if (!_nodePositions.ContainsKey(kvp.Key)) continue;
                foreach (var dep in kvp.Value)
                {
                    if (!_nodePositions.ContainsKey(dep)) continue;
                    DrawConnection(kvp.Key, dep);
                }
            }

            Handles.EndGUI();
        }

        private void DrawConnection(Type from, Type to)
        {
            Rect fromRect = GetScreenRect(_nodePositions[from]);
            Rect toRect = GetScreenRect(_nodePositions[to]);

            Vector2 start = new Vector2(fromRect.center.x, fromRect.yMax);
            Vector2 end = new Vector2(toRect.center.x, toRect.y);

            bool isHighlighted = _selectedNode == from || _selectedNode == to || _hoveredNode == from || _hoveredNode == to;
            Color color = isHighlighted ? HighlightConnectionColor : ConnectionColor;
            float thickness = isHighlighted ? 3f : 2f;

            // Bezier curve
            float tangentStrength = Mathf.Abs(end.y - start.y) * 0.4f;
            tangentStrength = Mathf.Max(tangentStrength, 30f);
            
            Vector2 startTangent = start + Vector2.down * tangentStrength;
            Vector2 endTangent = end + Vector2.up * tangentStrength;

            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, thickness);

            // Arrow head
            DrawArrow(end, Vector2.down, color, 8f);
        }

        private void DrawArrow(Vector2 tip, Vector2 direction, Color color, float size)
        {
            direction = direction.normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            
            Vector2 left = tip - direction * size + perpendicular * (size * 0.5f);
            Vector2 right = tip - direction * size - perpendicular * (size * 0.5f);

            Handles.color = color;
            Handles.DrawAAConvexPolygon(tip, left, right);
        }

        private void DrawMinimap()
        {
            float mapWidth = 150f;
            float mapHeight = 100f;
            Rect mapRect = new Rect(position.width - mapWidth - 10, position.height - mapHeight - 30, mapWidth, mapHeight);

            EditorGUI.DrawRect(mapRect, MinimapBgColor);
            DrawNodeBorder(mapRect, NodeBorderColor, 1f);

            if (_nodePositions.Count == 0) return;

            // Calculate bounds
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var rect in _nodePositions.Values)
            {
                minX = Mathf.Min(minX, rect.x);
                maxX = Mathf.Max(maxX, rect.xMax);
                minY = Mathf.Min(minY, rect.y);
                maxY = Mathf.Max(maxY, rect.yMax);
            }

            float graphWidth = maxX - minX + 100;
            float graphHeight = maxY - minY + 100;
            float scale = Mathf.Min((mapWidth - 10) / graphWidth, (mapHeight - 10) / graphHeight);

            // Draw mini nodes
            foreach (var kvp in _nodePositions)
            {
                float x = mapRect.x + 5 + (kvp.Value.x - minX + 50) * scale;
                float y = mapRect.y + 5 + (kvp.Value.y - minY + 50) * scale;
                float w = Mathf.Max(4, kvp.Value.width * scale);
                float h = Mathf.Max(3, kvp.Value.height * scale);

                Color col = _monoTypes.Contains(kvp.Key) ? MonoNodeColor : ServiceNodeColor;
                EditorGUI.DrawRect(new Rect(x, y, w, h), col);
            }

            // Draw viewport indicator
            float vpX = mapRect.x + 5 + (-_panOffset.x / _zoom - minX + 50) * scale;
            float vpY = mapRect.y + 5 + (-_panOffset.y / _zoom - minY + 50 - 20 / _zoom) * scale;
            float vpW = (position.width / _zoom) * scale;
            float vpH = ((position.height - 50) / _zoom) * scale;

            Handles.BeginGUI();
            Handles.color = new Color(1, 1, 1, 0.5f);
            Handles.DrawSolidRectangleWithOutline(new Rect(vpX, vpY, vpW, vpH), Color.clear, Color.white);
            Handles.EndGUI();
        }

        private void DrawStatsPanel()
        {
            float panelWidth = 180f;
            float panelHeight = 120f;
            Rect panelRect = new Rect(10, position.height - panelHeight - 30, panelWidth, panelHeight);

            EditorGUI.DrawRect(panelRect, StatsBackgroundColor);
            DrawNodeBorder(panelRect, NodeBorderColor, 1f);

            GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 5, panelRect.width - 16, panelRect.height - 10));
            
            GUILayout.Label("📊 Graph Statistics", EditorStyles.whiteBoldLabel);
            GUILayout.Space(3);

            int totalNodes = _nodePositions.Count;
            int totalEdges = _dependencyGraph?.Sum(kvp => kvp.Value.Count) ?? 0;
            int monoCount = _monoTypes.Count;
            int serviceCount = totalNodes - monoCount;

            GUILayout.Label($"Total Nodes: {totalNodes}", _statsLabelStyle);
            GUILayout.Label($"Total Connections: {totalEdges}", _statsLabelStyle);
            GUILayout.Label($"Services: {serviceCount}", _statsLabelStyle);
            GUILayout.Label($"MonoBehaviours: {monoCount}", _statsLabelStyle);
            GUILayout.Label($"Zoom: {_zoom:F1}x", _statsLabelStyle);

            GUILayout.EndArea();
        }

        private void DrawTooltip()
        {
            if (_hoveredNode == null || _selectedNode == _hoveredNode) return;

            string tooltip = $"{_hoveredNode.FullName}";
            if (_nodeData.TryGetValue(_hoveredNode, out var data))
            {
                tooltip += $"\n\nDependencies: {data.OutgoingCount}";
                tooltip += $"\nUsed by: {data.IncomingCount}";
            }

            Vector2 mousePos = Event.current.mousePosition;
            Vector2 size = _tooltipStyle.CalcSize(new GUIContent(tooltip));
            Rect tooltipRect = new Rect(mousePos.x + 15, mousePos.y + 10, size.x + 10, size.y + 6);

            // Keep on screen
            if (tooltipRect.xMax > position.width) tooltipRect.x = position.width - tooltipRect.width;
            if (tooltipRect.yMax > position.height) tooltipRect.y = mousePos.y - tooltipRect.height - 5;

            EditorGUI.DrawRect(tooltipRect, new Color(0.15f, 0.15f, 0.18f, 0.95f));
            DrawNodeBorder(tooltipRect, NodeBorderColor, 1f);
            GUI.Label(new Rect(tooltipRect.x + 5, tooltipRect.y + 3, tooltipRect.width - 10, tooltipRect.height - 6), tooltip, _tooltipStyle);
        }

        private void DrawStatusBar()
        {
            Rect barRect = new Rect(0, position.height - 22, position.width, 22);
            EditorGUI.DrawRect(barRect, new Color(0.1f, 0.1f, 0.12f));

            string status = Application.isPlaying ? "● Live" : "○ Enter Play Mode";
            Color statusColor = Application.isPlaying ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.7f, 0.7f, 0.7f);

            GUI.color = statusColor;
            GUI.Label(new Rect(10, position.height - 20, 200, 20), status, EditorStyles.miniLabel);
            GUI.color = Color.white;

            if (_selectedNode != null)
            {
                GUI.Label(new Rect(position.width - 300, position.height - 20, 290, 20), 
                    $"Selected: {_selectedNode.Name}", EditorStyles.miniLabel);
            }
        }

        private Rect GetScreenRect(Rect baseRect)
        {
            return new Rect(
                baseRect.x * _zoom + _panOffset.x,
                baseRect.y * _zoom + _panOffset.y + 20,
                baseRect.width * _zoom,
                baseRect.height * _zoom
            );
        }

        private void UpdateHoverState()
        {
            _hoveredNode = null;
            Vector2 mousePos = Event.current.mousePosition;

            foreach (var kvp in _nodePositions)
            {
                if (GetScreenRect(kvp.Value).Contains(mousePos))
                {
                    _hoveredNode = kvp.Key;
                    Repaint();
                    break;
                }
            }
        }

        private void HandleInput()
        {
            Event e = Event.current;

            // Node click
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                foreach (var kvp in _nodePositions)
                {
                    if (GetScreenRect(kvp.Value).Contains(e.mousePosition))
                    {
                        _selectedNode = kvp.Key;
                        _draggingNode = kvp.Key;
                        e.Use();
                        Repaint();
                        return;
                    }
                }
                // Click on empty space deselects
                _selectedNode = null;
            }

            // Node drag
            if (e.type == EventType.MouseDrag && _draggingNode != null && e.button == 0)
            {
                var rect = _nodePositions[_draggingNode];
                rect.x += e.delta.x / _zoom;
                rect.y += e.delta.y / _zoom;
                _nodePositions[_draggingNode] = rect;
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _draggingNode = null;
            }

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
                float oldZoom = _zoom;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.25f, 2.5f);

                // Zoom towards mouse
                Vector2 mouseWorld = (e.mousePosition - _panOffset) / oldZoom;
                _panOffset = e.mousePosition - mouseWorld * _zoom;

                e.Use();
                Repaint();
            }

            // Escape deselects
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _selectedNode = null;
                e.Use();
                Repaint();
            }

            // F to frame selected
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && _selectedNode != null)
            {
                FrameNode(_selectedNode);
                e.Use();
            }
        }

        private void FrameNode(Type type)
        {
            if (!_nodePositions.ContainsKey(type)) return;

            Rect nodeRect = _nodePositions[type];
            _panOffset = new Vector2(
                position.width / 2 - (nodeRect.center.x * _zoom),
                (position.height - 40) / 2 - (nodeRect.center.y * _zoom)
            );
            Repaint();
        }
    }
}
#endif
