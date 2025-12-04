using EthanToolBox.Core.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Editor.SceneManagement
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var sceneAssetProp = property.FindPropertyRelative("_sceneAsset");
            var scenePathProp = property.FindPropertyRelative("_scenePath");

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            EditorGUI.BeginChangeCheck();
            var newSceneAsset = EditorGUI.ObjectField(position, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetProp.objectReferenceValue = newSceneAsset;
                if (newSceneAsset != null)
                {
                    scenePathProp.stringValue = AssetDatabase.GetAssetPath(newSceneAsset);
                }
                else
                {
                    scenePathProp.stringValue = string.Empty;
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
