using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using EthanToolBox.DependencyInjection;

namespace EthanToolBox.DependencyInjection.Editor
{
    [CustomEditor(typeof(DIBootstrapper))]
    [CanEditMultipleObjects]
    public class DIBootstrapperEditor : UnityEditor.Editor
    {
        private string _searchFilter = "";

        public override void OnInspectorGUI()
        {
            DIBootstrapper bootstrapper = (DIBootstrapper)target;

            serializedObject.Update();
            
            SerializedProperty showDebugProp = serializedObject.FindProperty("_showDebugServices");
            SerializedProperty servicesProp = serializedObject.FindProperty("_registeredServices");

            EditorGUILayout.PropertyField(showDebugProp, new GUIContent("Afficher les Services de Debug"));
            
            if (!showDebugProp.boolValue)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Les services sont enregistrés au démarrage.\nLance le mode Play pour voir les services actifs.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (servicesProp == null)
            {
                EditorGUILayout.HelpBox("Impossible de trouver la liste des services. Vérifie le script DIBootstrapper.", MessageType.Error);
                return;
            }

            // Section Recherche
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Recherche:", GUILayout.Width(70));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Utilisation d'une variable persistante pour le foldout si possible, sinon on utilise showDebugProp
            showDebugProp.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugProp.isExpanded, $"Services Enregistrés ({servicesProp.arraySize})");
            
            if (showDebugProp.isExpanded)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                if (servicesProp.arraySize == 0)
                {
                    EditorGUILayout.LabelField("Aucun service enregistr.", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    // Header de la table
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField("Instance (GameObject)", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(2);

                    for (int i = 0; i < servicesProp.arraySize; i++)
                    {
                        SerializedProperty entry = servicesProp.GetArrayElementAtIndex(i);
                        SerializedProperty typeName = entry.FindPropertyRelative("ServiceType");
                        SerializedProperty instance = entry.FindPropertyRelative("Instance");

                        if (!string.IsNullOrEmpty(_searchFilter) && !typeName.stringValue.ToLower().Contains(_searchFilter.ToLower()))
                            continue;

                        EditorGUILayout.BeginHorizontal();
                        
                        // Label du type avec style
                        GUIStyle typeStyle = new GUIStyle(EditorStyles.label);
                        typeStyle.normal.textColor = new Color(0.4f, 0.7f, 1f); // Un joli bleu
                        EditorGUILayout.LabelField(typeName.stringValue, typeStyle, GUILayout.Width(150));
                        
                        // Champ de l'instance
                        EditorGUILayout.PropertyField(instance, GUIContent.none);
                        
                        // Bouton Ping
                        if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40)))
                        {
                            EditorGUIUtility.PingObject(instance.objectReferenceValue);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
