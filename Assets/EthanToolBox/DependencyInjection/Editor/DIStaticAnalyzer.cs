#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection.Editor
{
    public class DIStaticAnalyzer : EditorWindow
    {
        [MenuItem("EthanToolBox/Injection/Static Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<DIStaticAnalyzer>("DI Analyzer");
        }

        private List<string> _errors = new List<string>();
        private Vector2 _scroll;

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("DI Static Analysis", EditorStyles.boldLabel);
            GUILayout.Label("Scans scenes and code to find missing dependencies.", EditorStyles.miniLabel);
            
            if (GUILayout.Button("Run Analysis"))
            {
                RunAnalysis();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_errors.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found (or analysis not run).", MessageType.Info);
            }
            else
            {
                foreach (var err in _errors)
                {
                    EditorGUILayout.HelpBox(err, MessageType.Error);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunAnalysis()
        {
            _errors.Clear();
            var bindings = new HashSet<Type>();

            // 1. Simulate Registrations (Heuristic)
            // We search for all [Service] attributes
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in allAssemblies)
            {
                if (assembly.FullName.StartsWith("Unity") || assembly.FullName.StartsWith("System")) continue;

                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetCustomAttribute<ServiceAttribute>() != null)
                    {
                        var attr = type.GetCustomAttribute<ServiceAttribute>();
                        bindings.Add(attr.ServiceType ?? type);
                    }
                }
            }
            
            // Note: We cannot easily simulate manual registrations in DICompositionRoot.Configure() 
            // without actually running the code, which is dangerous in Editor.
            // So this tool focuses on [Service] attributes and assumes manual bindings are correct or handles them gracefully.

            // 2. Scan MonoBehaviours in ALL scenes (that are loaded) and Prefabs?
            // For safety, let's scan the open scenes.
            var monoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var mb in monoBehaviours)
            {
                if (mb == null) continue;
                ScanType(mb.GetType(), bindings, mb.name);
            }

            Debug.Log($"[DI Analyzer] Analysis complete. Found {_errors.Count} issues.");
        }

        private void ScanType(Type type, HashSet<Type> bindings, string contextName)
        {
            // Fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var injectAttr = field.GetCustomAttribute<InjectAttribute>();
                if (injectAttr != null && !injectAttr.Optional)
                {
                    var neededType = field.FieldType;
                    // Unwrap Lazy
                    if (neededType.IsGenericType && neededType.GetGenericTypeDefinition() == typeof(Lazy<>))
                        neededType = neededType.GetGenericArguments()[0];
                    // Unwrap Func
                    if (neededType.IsGenericType && neededType.GetGenericTypeDefinition() == typeof(Func<>))
                        neededType = neededType.GetGenericArguments()[0];

                    if (!bindings.Contains(neededType))
                    {
                        // Check if it's a manual registration "known" limitation
                        _errors.Add($"Missing Dependency: {contextName} ({type.Name}) needs {neededType.Name}, but no [Service] for it was found.");
                    }
                }
            }
            
            // Properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                 var injectAttr = prop.GetCustomAttribute<InjectAttribute>();
                if (injectAttr != null && !injectAttr.Optional)
                {
                    var neededType = prop.PropertyType;
                     // Unwrap Lazy
                    if (neededType.IsGenericType && neededType.GetGenericTypeDefinition() == typeof(Lazy<>))
                        neededType = neededType.GetGenericArguments()[0];
                     // Unwrap Func
                    if (neededType.IsGenericType && neededType.GetGenericTypeDefinition() == typeof(Func<>))
                        neededType = neededType.GetGenericArguments()[0];

                    if (!bindings.Contains(neededType))
                    {
                        _errors.Add($"Missing Dependency: {contextName} ({type.Name}) needs {neededType.Name}, but no [Service] for it was found.");
                    }
                }
            }
        }
    }
}
#endif
