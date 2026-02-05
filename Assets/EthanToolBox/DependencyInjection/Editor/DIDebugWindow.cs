#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection.Editor
{
    public class DIDebugWindow : EditorWindow
    {
        // Colors - Modern dark theme
        private static readonly Color BackgroundColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color HeaderColor = new Color(0.12f, 0.12f, 0.14f);
        private static readonly Color AccentColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color ServiceColor = new Color(0.3f, 0.85f, 0.5f);
        private static readonly Color MonoBehaviourColor = new Color(1f, 0.7f, 0.3f);
        private static readonly Color ClassColor = new Color(0.85f, 0.5f, 0.85f);
        private static readonly Color SeparatorColor = new Color(0.25f, 0.25f, 0.28f);

        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private GUIStyle _headerStyle;
        private GUIStyle _serviceStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _countStyle;
        private GUIStyle _searchStyle;
        private bool _stylesInitialized;

        private DICompositionRoot _compositionRoot;
        private List<ServiceInfo> _services = new List<ServiceInfo>();
        private float _lastRefreshTime;

        private class ServiceInfo
        {
            public Type ServiceType;
            public object Instance;
            public bool IsMonoBehaviour;
            public string DisplayName;
        }

        [MenuItem("Tools/EthanToolBox/DI Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<DIDebugWindow>("DI Debug");
            window.minSize = new Vector2(350, 400);
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
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 10, 10)
            };
            _headerStyle.normal.textColor = AccentColor;

            _serviceStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                padding = new RectOffset(15, 10, 5, 5),
                richText = true
            };

            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                padding = new RectOffset(5, 5, 2, 2)
            };
            _labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            _countStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter
            };
            _countStyle.normal.textColor = AccentColor;

            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                fixedHeight = 25,
                fontSize = 12
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();
            DrawBackground();

            EditorGUILayout.BeginVertical();
            {
                DrawHeader();
                DrawSeparator();
                DrawStats();
                DrawSeparator();
                DrawSearchBar();
                DrawSeparator();
                DrawServicesList();
            }
            EditorGUILayout.EndVertical();

            // Auto-refresh in play mode
            if (Application.isPlaying && Time.realtimeSinceStartup - _lastRefreshTime > 2f)
            {
                RefreshServices();
                _lastRefreshTime = Time.realtimeSinceStartup;
            }
        }

        private void DrawBackground()
        {
            var rect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(rect, BackgroundColor);
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("⚡ DI Debug", _headerStyle);
                GUILayout.FlexibleSpace();
                
                if (Application.isPlaying)
                {
                    GUI.color = ServiceColor;
                    GUILayout.Label("● LIVE", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.5f);
                    GUILayout.Label("○ STOPPED", EditorStyles.label);
                    GUI.color = Color.white;
                }
                
                GUILayout.Space(10);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStats()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                
                // Services count
                DrawStatBox("Services", _services.Count.ToString(), AccentColor);
                GUILayout.Space(20);
                
                // MonoBehaviours count
                int mbCount = _services.Count(s => s.IsMonoBehaviour);
                DrawStatBox("MonoBehaviours", mbCount.ToString(), MonoBehaviourColor);
                GUILayout.Space(20);
                
                // Classes count
                int classCount = _services.Count(s => !s.IsMonoBehaviour);
                DrawStatBox("Classes", classCount.ToString(), ClassColor);
                
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void DrawStatBox(string label, string value, Color color)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            {
                _countStyle.normal.textColor = color;
                GUILayout.Label(value, _countStyle);
                
                _labelStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label(label, _labelStyle);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);
                
                GUI.SetNextControlName("SearchField");
                _searchFilter = EditorGUILayout.TextField(_searchFilter, _searchStyle, GUILayout.Height(25));
                
                if (string.IsNullOrEmpty(_searchFilter))
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    rect.x += 20;
                    GUI.color = new Color(0.5f, 0.5f, 0.5f);
                    GUI.Label(rect, "🔍 Search services...");
                    GUI.color = Color.white;
                }
                
                GUILayout.Space(10);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawServicesList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                if (!Application.isPlaying)
                {
                    DrawCenteredMessage("▶ Enter Play Mode to see services");
                }
                else if (_services.Count == 0)
                {
                    DrawCenteredMessage("No services registered");
                }
                else
                {
                    var filtered = string.IsNullOrEmpty(_searchFilter) 
                        ? _services 
                        : _services.Where(s => s.DisplayName.ToLower().Contains(_searchFilter.ToLower())).ToList();

                    foreach (var service in filtered)
                    {
                        DrawServiceItem(service);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawServiceItem(ServiceInfo service)
        {
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(35));
            {
                // Hover effect
                if (rect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.3f));
                }

                GUILayout.Space(15);

                // Type indicator
                var color = service.IsMonoBehaviour ? MonoBehaviourColor : ClassColor;
                var icon = service.IsMonoBehaviour ? "◆" : "●";
                
                GUI.color = color;
                GUILayout.Label(icon, GUILayout.Width(20));
                GUI.color = Color.white;

                // Service name
                GUILayout.Label(service.DisplayName, _serviceStyle);

                GUILayout.FlexibleSpace();

                // Click to ping MonoBehaviour
                if (service.IsMonoBehaviour && service.Instance is MonoBehaviour mb && mb != null)
                {
                    if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        EditorGUIUtility.PingObject(mb);
                        Selection.activeObject = mb;
                    }
                }

                GUILayout.Space(10);
            }
            EditorGUILayout.EndHorizontal();

            // Separator
            var sepRect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(sepRect, SeparatorColor);
        }

        private void DrawCenteredMessage(string message)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUILayout.Label(message, EditorStyles.largeLabel);
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void DrawSeparator()
        {
            GUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(rect, SeparatorColor);
            GUILayout.Space(5);
        }

        private void RefreshServices()
        {
            _services.Clear();
            
            if (!Application.isPlaying) return;

            _compositionRoot = FindFirstObjectByType<DICompositionRoot>();
            if (_compositionRoot == null) return;

            // Access container via reflection (since Container is protected)
            var containerField = typeof(DICompositionRoot).GetField("Container", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (containerField == null) return;

            var container = containerField.GetValue(_compositionRoot) as DIContainer;
            if (container == null) return;

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

            // Sort: MonoBehaviours first, then by name
            _services = _services
                .OrderByDescending(s => s.IsMonoBehaviour)
                .ThenBy(s => s.DisplayName)
                .ToList();

            Repaint();
        }
    }
}
#endif
