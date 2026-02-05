#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection.Editor
{
    /// <summary>
    /// Clean dependency graph showing only registered DI services.
    /// </summary>
    public class DependencyGraphWindow : EditorWindow
    {
        // Node appearance
        private const float NodeWidth = 180f;
        private const float NodeHeight = 60f;
        private const float NodeSpacingX = 250f;
        private const float NodeSpacingY = 120f;

        // Colors
        private static readonly Color BackgroundColor = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color GridColor = new Color(0.18f, 0.18f, 0.22f, 0.5f);
        
        private static readonly Color ServiceNodeColor = new Color(0.18f, 0.42f, 0.32f);
        private static readonly Color MonoNodeColor = new Color(0.48f, 0.32f, 0.15f);
        private static readonly Color SelectedColor = new Color(0.35f, 0.75f, 0.5f);
        private static readonly Color NodeBorderColor = new Color(0.4f, 0.4f, 0.45f);
        
        private static readonly Color ConnectionOutColor = new Color(0.3f, 0.7f, 0.95f, 0.9f);  // Blue: "depends on"
        private static readonly Color ConnectionInColor = new Color(0.95f, 0.6f, 0.3f, 0.9f);   // Orange: "used by"

        // Data
        private HashSet<Type> _registeredServices = new HashSet<Type>();
        private Dictionary<Type, HashSet<Type>> _dependencyGraph;
        private Dictionary<Type, Rect> _nodePositions = new Dictionary<Type, Rect>();
        private HashSet<Type> _monoTypes = new HashSet<Type>();
        
        // Interaction
        private Vector2 _panOffset = Vector2.zero;
        private Vector2 _dragStart;
        private bool _isPanning;
        private Type _selectedNode;
        private Type _hoveredNode;
        private float _zoom = 1f;
        private Type _draggingNode;

        // Styles
        private GUIStyle _nodeTitleStyle;
        private GUIStyle _nodeInfoStyle;
        private GUIStyle _connectionLabelStyle;
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
            window.RefreshData();
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
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _registeredServices.Clear();
                _nodePositions.Clear();
            }
        }

        private void RefreshData()
        {
            var root = FindFirstObjectByType<DICompositionRoot>();
            if (root == null) return;

            var container = root.Container;
            if (container == null) return;

            _dependencyGraph = container.DependencyGraph;
            _registeredServices.Clear();
            _monoTypes.Clear();
            
            // ONLY get registered services - this is the key filter!
            foreach (var type in container.GetAllRegisteredTypes())
            {
                _registeredServices.Add(type);
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    _monoTypes.Add(type);
            }

            BuildLayout();
            Repaint();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _nodeTitleStyle = new GUIStyle(EditorStyles.whiteBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = false
            };

            _nodeInfoStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };

            _connectionLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _stylesInitialized = true;
        }

        private void BuildLayout()
        {
            _nodePositions.Clear();
            
            if (_registeredServices.Count == 0) return;

            // Filter to only show registered services
            var servicesToShow = _registeredServices.ToList();
            
            // Sort: MonoBehaviours first (they tend to be consumers), then services
            servicesToShow = servicesToShow
                .OrderByDescending(t => _monoTypes.Contains(t))
                .ThenBy(t => t.Name)
                .ToList();

            // Simple grid layout
            float startX = 100f;
            float startY = 80f;
            int maxCols = Mathf.Max(2, Mathf.CeilToInt(Mathf.Sqrt(servicesToShow.Count)));
            
            int col = 0;
            int row = 0;
            
            foreach (var type in servicesToShow)
            {
                float x = startX + col * NodeSpacingX;
                float y = startY + row * NodeSpacingY;
                _nodePositions[type] = new Rect(x, y, NodeWidth, NodeHeight);
                
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
            UpdateHoverState();

            // Draw connections FIRST (behind nodes)
            DrawConnections();

            // Draw nodes
            foreach (var kvp in _nodePositions)
            {
                DrawNode(kvp.Key, kvp.Value);
            }

            // Legend
            DrawLegend();

            // Status
            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("⟳ Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshData();
            }

            if (GUILayout.Button("⊙ Reset", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                _panOffset = Vector2.zero;
                _zoom = 1f;
            }

            if (GUILayout.Button("⊞ Layout", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                BuildLayout();
            }

            GUILayout.FlexibleSpace();
            
            GUILayout.Label($"{_registeredServices.Count} Services", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            float gridSize = 50f * _zoom;
            
            Handles.BeginGUI();
            Handles.color = GridColor;

            float offsetX = _panOffset.x % gridSize;
            float offsetY = _panOffset.y % gridSize;

            for (float x = offsetX; x < position.width; x += gridSize)
                Handles.DrawLine(new Vector3(x, 20), new Vector3(x, position.height));

            for (float y = offsetY + 20; y < position.height; y += gridSize)
                Handles.DrawLine(new Vector3(0, y), new Vector3(position.width, y));

            Handles.EndGUI();
        }

        private void DrawNode(Type type, Rect baseRect)
        {
            Rect rect = GetScreenRect(baseRect);

            // Culling
            if (rect.xMax < 0 || rect.xMin > position.width || rect.yMax < 20 || rect.yMin > position.height)
                return;

            bool isMono = _monoTypes.Contains(type);
            bool isSelected = _selectedNode == type;
            bool isHovered = _hoveredNode == type;

            // Colors
            Color nodeCol = isMono ? MonoNodeColor : ServiceNodeColor;
            if (isSelected) nodeCol = SelectedColor;
            else if (isHovered) nodeCol = Color.Lerp(nodeCol, Color.white, 0.2f);

            // Shadow
            EditorGUI.DrawRect(new Rect(rect.x + 4, rect.y + 4, rect.width, rect.height), new Color(0, 0, 0, 0.4f));
            
            // Main background
            EditorGUI.DrawRect(rect, nodeCol);
            
            // Border
            float borderWidth = isSelected ? 3f : (isHovered ? 2f : 1f);
            Color borderCol = isSelected ? Color.white : (isHovered ? new Color(0.9f, 0.9f, 0.9f) : NodeBorderColor);
            DrawBorder(rect, borderCol, borderWidth);

            // Type indicator strip
            Color stripColor = isMono ? new Color(1f, 0.7f, 0.3f) : new Color(0.4f, 0.8f, 0.5f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 5, rect.height), stripColor);

            // Icon + Name
            string icon = isMono ? "🎮" : "⚙";
            string displayName = type.Name;
            if (displayName.Length > 18) displayName = displayName.Substring(0, 15) + "...";
            
            Rect titleRect = new Rect(rect.x + 10, rect.y + 12, rect.width - 20, 22);
            GUI.Label(titleRect, $"{icon} {displayName}", _nodeTitleStyle);

            // Connection counts
            int outCount = GetDependencyCount(type);
            int inCount = GetConsumerCount(type);
            
            string info = "";
            if (outCount > 0 || inCount > 0)
            {
                if (outCount > 0) info += $"→ {outCount} deps";
                if (inCount > 0) info += (info.Length > 0 ? "  •  " : "") + $"← {inCount} users";
            }
            else
            {
                info = "No connections";
            }
            
            Rect infoRect = new Rect(rect.x + 5, rect.y + rect.height - 22, rect.width - 10, 18);
            GUI.Label(infoRect, info, _nodeInfoStyle);
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void DrawConnections()
        {
            if (_dependencyGraph == null) return;

            Handles.BeginGUI();

            foreach (var kvp in _dependencyGraph)
            {
                Type consumer = kvp.Key;
                if (!_nodePositions.ContainsKey(consumer)) continue;

                foreach (var dependency in kvp.Value)
                {
                    // ONLY draw if BOTH are registered services
                    if (!_nodePositions.ContainsKey(dependency)) continue;

                    bool isHighlighted = _selectedNode == consumer || _selectedNode == dependency;
                    DrawConnectionLine(consumer, dependency, isHighlighted);
                }
            }

            Handles.EndGUI();
        }

        private void DrawConnectionLine(Type from, Type to, bool highlighted)
        {
            Rect fromRect = GetScreenRect(_nodePositions[from]);
            Rect toRect = GetScreenRect(_nodePositions[to]);

            // Calculate connection points on node edges
            Vector2 fromCenter = fromRect.center;
            Vector2 toCenter = toRect.center;
            
            Vector2 start = GetEdgePoint(fromRect, toCenter);
            Vector2 end = GetEdgePoint(toRect, fromCenter);

            Color lineColor = highlighted ? ConnectionOutColor : new Color(0.5f, 0.7f, 0.9f, 0.6f);
            float thickness = highlighted ? 4f : 2f;

            // Curved line
            float distance = Vector2.Distance(start, end);
            float curvature = Mathf.Min(distance * 0.3f, 80f);
            
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 midPoint = (start + end) / 2 + perpendicular * curvature * 0.3f;

            // Draw bezier
            Vector2 t1 = Vector2.Lerp(start, midPoint, 0.5f);
            Vector2 t2 = Vector2.Lerp(midPoint, end, 0.5f);
            
            Handles.DrawBezier(start, end, t1, t2, lineColor, null, thickness);

            // Arrow at the end
            DrawArrowHead(end, (end - t2).normalized, lineColor, highlighted ? 12f : 8f);

            // Label on connection
            if (highlighted)
            {
                Vector2 labelPos = midPoint;
                string label = "depends on";
                
                // Background for label
                Vector2 labelSize = _connectionLabelStyle.CalcSize(new GUIContent(label));
                Rect labelBg = new Rect(labelPos.x - labelSize.x / 2 - 4, labelPos.y - 8, labelSize.x + 8, 16);
                EditorGUI.DrawRect(labelBg, new Color(0.1f, 0.1f, 0.15f, 0.9f));
                
                GUI.Label(labelBg, label, _connectionLabelStyle);
            }
        }

        private Vector2 GetEdgePoint(Rect rect, Vector2 target)
        {
            Vector2 center = rect.center;
            Vector2 dir = (target - center).normalized;
            
            // Calculate intersection with rectangle edges
            float halfW = rect.width / 2;
            float halfH = rect.height / 2;
            
            float scaleX = dir.x != 0 ? Mathf.Abs(halfW / dir.x) : float.MaxValue;
            float scaleY = dir.y != 0 ? Mathf.Abs(halfH / dir.y) : float.MaxValue;
            float scale = Mathf.Min(scaleX, scaleY);
            
            return center + dir * scale;
        }

        private void DrawArrowHead(Vector2 tip, Vector2 direction, Color color, float size)
        {
            if (direction.magnitude < 0.01f) return;
            
            direction = direction.normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            
            Vector2 left = tip - direction * size + perpendicular * (size * 0.5f);
            Vector2 right = tip - direction * size - perpendicular * (size * 0.5f);

            Handles.color = color;
            Handles.DrawAAConvexPolygon(tip, left, right);
        }

        private void DrawLegend()
        {
            float legendW = 160f;
            float legendH = 80f;
            Rect legendRect = new Rect(10, position.height - legendH - 30, legendW, legendH);
            
            EditorGUI.DrawRect(legendRect, new Color(0.1f, 0.1f, 0.12f, 0.95f));
            DrawBorder(legendRect, NodeBorderColor, 1f);

            GUILayout.BeginArea(new Rect(legendRect.x + 10, legendRect.y + 8, legendW - 20, legendH - 16));
            
            GUILayout.Label("Legend", EditorStyles.whiteBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(new Rect(0, 22, 12, 12), new Color(0.4f, 0.8f, 0.5f));
            GUILayout.Space(18);
            GUILayout.Label("Service (Class)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(0);
            EditorGUI.DrawRect(new Rect(0, 40, 12, 12), new Color(1f, 0.7f, 0.3f));
            GUILayout.Space(18);
            GUILayout.Label("MonoBehaviour", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();

            // Draw colored squares manually
            EditorGUI.DrawRect(new Rect(legendRect.x + 10, legendRect.y + 30, 12, 12), new Color(0.4f, 0.8f, 0.5f));
            EditorGUI.DrawRect(new Rect(legendRect.x + 10, legendRect.y + 48, 12, 12), new Color(1f, 0.7f, 0.3f));
        }

        private void DrawStatusBar()
        {
            Rect barRect = new Rect(0, position.height - 22, position.width, 22);
            EditorGUI.DrawRect(barRect, new Color(0.08f, 0.08f, 0.1f));

            string status = Application.isPlaying ? "● Live" : "○ Enter Play Mode";
            Color statusColor = Application.isPlaying ? new Color(0.5f, 0.95f, 0.5f) : new Color(0.6f, 0.6f, 0.6f);

            GUI.color = statusColor;
            GUI.Label(new Rect(12, position.height - 19, 150, 18), status, EditorStyles.miniLabel);
            GUI.color = Color.white;

            if (_selectedNode != null)
            {
                string selected = $"Selected: {_selectedNode.Name}";
                GUI.Label(new Rect(position.width - 250, position.height - 19, 240, 18), selected, EditorStyles.miniLabel);
            }

            // Zoom indicator
            GUI.Label(new Rect(position.width / 2 - 30, position.height - 19, 60, 18), $"Zoom: {_zoom:F1}x", EditorStyles.miniLabel);
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

        private int GetDependencyCount(Type type)
        {
            if (_dependencyGraph == null || !_dependencyGraph.ContainsKey(type)) return 0;
            // Count only dependencies that are also registered services
            return _dependencyGraph[type].Count(d => _registeredServices.Contains(d));
        }

        private int GetConsumerCount(Type type)
        {
            if (_dependencyGraph == null) return 0;
            return _dependencyGraph.Count(kvp => _registeredServices.Contains(kvp.Key) && kvp.Value.Contains(type));
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
                Type clicked = null;
                foreach (var kvp in _nodePositions)
                {
                    if (GetScreenRect(kvp.Value).Contains(e.mousePosition))
                    {
                        clicked = kvp.Key;
                        break;
                    }
                }
                
                if (clicked != null)
                {
                    _selectedNode = clicked;
                    _draggingNode = clicked;
                }
                else
                {
                    _selectedNode = null;
                }
                
                e.Use();
                Repaint();
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

            // Pan
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

            // Zoom
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -e.delta.y * 0.05f;
                float oldZoom = _zoom;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.3f, 2f);

                Vector2 mouseWorld = (e.mousePosition - _panOffset) / oldZoom;
                _panOffset = e.mousePosition - mouseWorld * _zoom;

                e.Use();
                Repaint();
            }

            // Escape
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
