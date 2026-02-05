#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection.Editor
{
    public class DIDebugWindow : EditorWindow
    {
        // Colors - Modern dark theme
        private static readonly Color BackgroundColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color HeaderColor = new Color(0.12f, 0.12f, 0.14f);
        private static readonly Color SplitterColor = new Color(0.1f, 0.1f, 0.1f);
        
        private static readonly Color ServiceColor = new Color(0.3f, 0.85f, 0.5f);
        private static readonly Color MonoBehaviourColor = new Color(1f, 0.7f, 0.3f);
        private static readonly Color ClassColor = new Color(0.85f, 0.5f, 0.85f);
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _serviceStyle;
        private GUIStyle _selectedServiceStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _searchStyle;
        private bool _stylesInitialized;

        // Data
        private DICompositionRoot _compositionRoot;
        private List<ServiceInfo> _services = new List<ServiceInfo>();
        private Dictionary<Type, HashSet<Type>> _dependencyGraph;
        
        private float _lastRefreshTime;
        private string _searchFilter = "";
        
        // Selection
        private ServiceInfo _selectedService;
        private Vector2 _listScrollPosition;
        private Vector2 _inspectorScrollPosition;

        // Layout
        private float _sidebarWidth = 300f;
        private bool _isResizing;

        private class ServiceInfo
        {
            public Type ServiceType;
            public object Instance;
            public bool IsMonoBehaviour;
            public string DisplayName;
        }

        [MenuItem("EthanToolBox/Injection/Debug Injection Panel")]
        public static void ShowWindow()
        {
            var window = GetWindow<DIDebugWindow>("DI Debug");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RefreshServices();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _services.Clear();
                _compositionRoot = null;
                _selectedService = null;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 10, 10),
                normal = { textColor = new Color(0.4f, 0.7f, 1f) }
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                padding = new RectOffset(5, 5, 5, 5),
                normal = { textColor = Color.white }
            };

            _serviceStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                padding = new RectOffset(10, 10, 5, 5),
                richText = true,
                fixedHeight = 30,
                alignment = TextAnchor.MiddleLeft
            };

            _selectedServiceStyle = new GUIStyle(_serviceStyle);
            _selectedServiceStyle.normal.background = MakeTex(1, 1, new Color(0.25f, 0.35f, 0.5f));
            _selectedServiceStyle.active.background = MakeTex(1, 1, new Color(0.25f, 0.35f, 0.5f));

            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                padding = new RectOffset(5, 5, 2, 2),
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                fixedHeight = 25,
                fontSize = 12
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) 
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            InitStyles();
            DrawBackground();

            EditorGUILayout.BeginHorizontal();
            {
                // Left Panel: Service List
                EditorGUILayout.BeginVertical(GUILayout.Width(_sidebarWidth));
                {
                    DrawHeader();
                    DrawSearchBar();
                    DrawServicesList();
                }
                EditorGUILayout.EndVertical();

                // Splitter
                DrawSplitter();

                // Right Panel: Inspector & Graph
                EditorGUILayout.BeginVertical();
                {
                    if (_selectedService != null)
                    {
                        DrawServiceInspector(_selectedService);
                    }
                    else
                    {
                        DrawEmptyInspector();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            // Auto-refresh in play mode
            if (Application.isPlaying && Time.realtimeSinceStartup - _lastRefreshTime > 1f)
            {
                RefreshServices(false); // Silent refresh
                _lastRefreshTime = Time.realtimeSinceStartup;
            }
            
            HandleResize();
        }

        private void DrawBackground()
        {
            var rect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(rect, BackgroundColor);
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("⚡ Services", _headerStyle);
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox($"Active Services: {_services.Count}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect services", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, _searchStyle, GUILayout.Height(25));
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawServicesList()
        {
            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);
            
            var filtered = string.IsNullOrEmpty(_searchFilter) 
                ? _services 
                : _services.Where(s => s.DisplayName.ToLower().Contains(_searchFilter.ToLower())).ToList();

            foreach (var service in filtered)
            {
                var isSelected = _selectedService == service;
                var style = isSelected ? _selectedServiceStyle : _serviceStyle;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(GUIContent.none, style, GUILayout.ExpandWidth(true)))
                {
                    _selectedService = service;
                    GUI.FocusControl(null); // Unfocus search
                }
                
                // Draw Icon & Text over button
                var rect = GUILayoutUtility.GetLastRect();
                var iconRect = new Rect(rect.x + 5, rect.y + 7, 16, 16);
                var labelRect = new Rect(rect.x + 25, rect.y, rect.width - 25, rect.height);
                
                var iconColor = service.IsMonoBehaviour ? MonoBehaviourColor : ClassColor;
                EditorGUI.DrawRect(iconRect, iconColor); // Placeholder for icon

                GUI.Label(labelRect, service.DisplayName, _serviceStyle);

                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawSplitter()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(2), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, SplitterColor);
            
            // Interaction logic for resizing
             EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        }
        
        private void HandleResize()
        {
             // Simple resize logic could go here if needed, keeping it fixed-ish for simplicity in V1
        }

        private void DrawEmptyInspector()
        {
            GUILayout.FlexibleSpace();
            var style = new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.gray } };
            GUILayout.Label("Select a service to view details", style);
            GUILayout.FlexibleSpace();
        }

        private void DrawServiceInspector(ServiceInfo service)
        {
            _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);
            
            // Header
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label(service.DisplayName, _headerStyle);
            GUILayout.Label(service.ServiceType.FullName, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 1. Dependency Graph (Simple Visualization)
            DrawDependenciesSection(service);

            GUILayout.Space(20);

            // 2. Object Inspector (Reflection)
            DrawReflectionInspector(service);

            EditorGUILayout.EndScrollView();
        }

        private void DrawDependenciesSection(ServiceInfo service)
        {
            GUILayout.Label("🔗 Dependency Graph", _subHeaderStyle);
            EditorGUILayout.BeginVertical("HelpBox");

            // Dependencies (What this service uses)
            GUILayout.Label("Depends On:", EditorStyles.boldLabel);
            if (_dependencyGraph != null && _dependencyGraph.TryGetValue(service.ServiceType, out var dependencies) && dependencies.Count > 0)
            {
                foreach (var depType in dependencies)
                {
                    if (GUILayout.Button($"➡ {depType.Name}", EditorStyles.label))
                    {
                        SelectServiceByType(depType);
                    }
                }
            }
            else
            {
                GUILayout.Label("  (None detected at resolution time)", EditorStyles.miniLabel);
            }

            GUILayout.Space(10);

            // Consumers (Who uses this service)
            GUILayout.Label("Used By:", EditorStyles.boldLabel);
            var consumers = GetConsumers(service.ServiceType);
            if (consumers.Count > 0)
            {
                foreach (var consumer in consumers)
                {
                    if (GUILayout.Button($"⬅ {consumer.Name}", EditorStyles.label))
                    {
                        SelectServiceByType(consumer);
                    }
                }
            }
            else
            {
                GUILayout.Label("  (None detected)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawReflectionInspector(ServiceInfo service)
        {
            GUILayout.Label("🔍 Inspector", _subHeaderStyle);
            EditorGUILayout.BeginVertical("HelpBox");

            if (service.IsMonoBehaviour && service.Instance is MonoBehaviour mb)
            {
                if (GUILayout.Button("Ping in Scene/Project"))
                {
                    EditorGUIUtility.PingObject(mb);
                }
            }

            // Public Properties
            var props = service.ServiceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (props.Length > 0)
            {
                foreach (var prop in props)
                {
                    if (!prop.CanRead) continue;
                    try
                    {
                        var val = prop.GetValue(service.Instance);
                        EditorGUILayout.LabelField(prop.Name, val?.ToString() ?? "null");
                    }
                    catch { }
                }
            }

            // Public Fields
            var fields = service.ServiceType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length > 0)
            {
                foreach (var field in fields)
                {
                    var val = field.GetValue(service.Instance);
                    EditorGUILayout.LabelField(field.Name, val?.ToString() ?? "null");
                }
            }

            if (props.Length == 0 && fields.Length == 0)
            {
                GUILayout.Label("No public properties to display.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private List<Type> GetConsumers(Type dependency)
        {
            var list = new List<Type>();
            if (_dependencyGraph == null) return list;

            foreach (var kvp in _dependencyGraph)
            {
                if (kvp.Value.Contains(dependency))
                {
                    list.Add(kvp.Key);
                }
            }
            return list;
        }

        private void SelectServiceByType(Type type)
        {
            var found = _services.FirstOrDefault(s => s.ServiceType == type);
            if (found != null)
            {
                _selectedService = found;
            }
        }

        private void RefreshServices(bool forceRepaint = true)
        {
            if (!Application.isPlaying) return;

            // Only rebuild list if count changed (basic check)
            // Ideally we'd do a more robust check or event-based
            _compositionRoot = FindFirstObjectByType<DICompositionRoot>();
            if (_compositionRoot == null) return;

            var containerField = typeof(DICompositionRoot).GetField("Container", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (containerField == null) return;

            var container = containerField.GetValue(_compositionRoot) as DIContainer;
            if (container == null) return;

            // Update dependency graph ref
            _dependencyGraph = container.DependencyGraph;

            // If first load or validation needed
            if (_services.Count == 0 || forceRepaint)
            {
                _services.Clear();
                foreach (var type in container.GetAllRegisteredTypes())
                {
                    var instance = container.GetInstance(type);
                    _services.Add(new ServiceInfo
                    {
                        ServiceType = type,
                        Instance = instance,
                        IsMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(type),
                        DisplayName = type.Name
                    });
                }

                _services = _services
                    .OrderByDescending(s => s.IsMonoBehaviour)
                    .ThenBy(s => s.DisplayName)
                    .ToList();
            }

            if (forceRepaint) Repaint();
        }
    }
}
#endif
