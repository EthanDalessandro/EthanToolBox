using UnityEditor;
using UnityEngine;
using EthanToolBox.Core.Extensions;

namespace EthanToolBox.Editor.Extensions
{
    // Note: CustomPropertyDrawer for generic types is tricky in Unity.
    // We usually need to define concrete types or use a different approach.
    // However, for ESerializableDictionary, we often just want to draw the lists if we can't do a full custom drawer easily without Odin.
    // But let's try to make a drawer that handles the "keys" and "values" lists nicely.
    
    // Since we can't easily target "ESerializableDictionary<TKey, TValue>" directly with CustomPropertyDrawer in a generic way for all types
    // without defining concrete classes, we will rely on the fact that ESerializableDictionary has "keys" and "values" fields.
    // But the user asked for an attribute [ESerialize]. 
    // If they use [ESerialize] on a Dictionary, it won't work because Dictionary is not serializable by Unity at all.
    // They MUST use ESerializableDictionary<TKey, TValue>.
    
    // So the workflow is:
    // 1. User uses ESerializableDictionary<K, V> myDict;
    // 2. Unity serializes "keys" and "values" lists naturally.
    // 3. We can make a drawer for ESerializableDictionary to show them side-by-side.

    // If the user specifically wants [ESerialize] to magically make a standard Dictionary work, that's impossible without ISerializationCallbackReceiver wrapper.
    // So I will assume [ESerialize] is a marker for our custom drawer, OR just a helper.
    
    // Actually, to support [ESerialize] Dictionary<K,V>, we would need to change the field type to ESerializableDictionary under the hood which we can't do.
    // So the user MUST use ESerializableDictionary.
    
    // Let's make a drawer that targets ESerializableDictionary.
    // We can't use [CustomPropertyDrawer(typeof(ESerializableDictionary<,>))] directly in older Unity versions, but in 2020+ it might work better.
    // Alternatively, we can just rely on the fact that it has lists.
    
    // Let's try to implement a drawer for the attribute, but the attribute needs to be on a field that IS serializable.
    // If they put [ESerialize] on a Dictionary, Unity won't even call the drawer because it ignores the field.
    
    // So I will provide the ESerializableDictionary class and a drawer for it.
    // The [ESerialize] attribute might be redundant if we just target the class, but I'll add it as requested.
    
    [CustomPropertyDrawer(typeof(ESerializableDictionary<,>), true)]
    public class ESerializableDictionaryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var keysProp = property.FindPropertyRelative("keys");
            var valuesProp = property.FindPropertyRelative("values");

            if (keysProp == null || valuesProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Use ESerializableDictionary");
                EditorGUI.EndProperty();
                return;
            }

            // Draw foldout
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Size field
                Rect sizeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                int size = keysProp.arraySize;
                int newSize = EditorGUI.IntField(sizeRect, "Size", size);
                
                if (newSize != size)
                {
                    keysProp.arraySize = newSize;
                    valuesProp.arraySize = newSize;
                }

                float yOffset = EditorGUIUtility.singleLineHeight * 2;

                for (int i = 0; i < keysProp.arraySize; i++)
                {
                    var keyProp = keysProp.GetArrayElementAtIndex(i);
                    var valueProp = valuesProp.GetArrayElementAtIndex(i);

                    float keyHeight = EditorGUI.GetPropertyHeight(keyProp);
                    float valueHeight = EditorGUI.GetPropertyHeight(valueProp);
                    float maxHeight = Mathf.Max(keyHeight, valueHeight);

                    Rect entryRect = new Rect(position.x, position.y + yOffset, position.width, maxHeight);
                    
                    float width = entryRect.width / 2 - 5;
                    Rect keyRect = new Rect(entryRect.x, entryRect.y, width, keyHeight);
                    Rect valueRect = new Rect(entryRect.x + width + 10, entryRect.y, width, valueHeight);

                    EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
                    EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);

                    yOffset += maxHeight + 2;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

            var keysProp = property.FindPropertyRelative("keys");
            var valuesProp = property.FindPropertyRelative("values");
            
            if (keysProp == null || valuesProp == null) return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight * 2; // Foldout + Size

            for (int i = 0; i < keysProp.arraySize; i++)
            {
                var keyProp = keysProp.GetArrayElementAtIndex(i);
                var valueProp = valuesProp.GetArrayElementAtIndex(i);
                float maxHeight = Mathf.Max(EditorGUI.GetPropertyHeight(keyProp), EditorGUI.GetPropertyHeight(valueProp));
                height += maxHeight + 2;
            }

            return height;
        }
    }
}
