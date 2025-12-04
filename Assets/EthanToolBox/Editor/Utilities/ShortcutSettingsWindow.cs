using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EthanToolBox.Editor.Utilities
{
    public class ShortcutSettingsWindow : EditorWindow
    {
        private const string ScriptPath = "Assets/EthanToolBox/Editor/Utilities/PlayModeShortcuts.cs";
        private int _selectedKeyIndex = 4; // Default F5
        private readonly string[] _keys = { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" };

        [MenuItem("EthanToolBox/Shortcuts/Configure Shortcut")]
        public static void ShowWindow()
        {
            GetWindow<ShortcutSettingsWindow>("Shortcut Settings");
        }

        private void OnEnable()
        {
            // Try to detect current key from script
            string content = ReadScript();
            if (!string.IsNullOrEmpty(content))
            {
                for (int i = 0; i < _keys.Length; i++)
                {
                    if (content.Contains($"_{_keys[i]}\")]"))
                    {
                        _selectedKeyIndex = i;
                        break;
                    }
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Play Mode Shortcut Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _selectedKeyIndex = EditorGUILayout.Popup("Shortcut Key", _selectedKeyIndex, _keys);

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Shortcut"))
            {
                UpdateScript(_keys[_selectedKeyIndex]);
            }

            EditorGUILayout.HelpBox("Applying will recompile scripts to register the new native menu shortcut.", MessageType.Info);
        }

        private string ReadScript()
        {
            if (!File.Exists(ScriptPath))
            {
                Debug.LogError($"Could not find script at {ScriptPath}");
                return null;
            }
            return File.ReadAllText(ScriptPath);
        }

        private void UpdateScript(string newKey)
        {
            string content = ReadScript();
            if (string.IsNullOrEmpty(content)) return;

            // Regex to replace the MenuItem attribute
            // [MenuItem("EthanToolBox/Shortcuts/Play and Maximize _F5")]
            string pattern = @"\[MenuItem\(""EthanToolBox/Shortcuts/Play and Maximize _F\d+""\)\]";
            string replacement = $"[MenuItem(\"EthanToolBox/Shortcuts/Play and Maximize _{newKey}\")]";

            string newContent = Regex.Replace(content, pattern, replacement);

            if (content != newContent)
            {
                File.WriteAllText(ScriptPath, newContent);
                AssetDatabase.Refresh();
                Debug.Log($"Shortcut updated to {newKey}. Compiling...");
            }
            else
            {
                Debug.Log("Shortcut is already set to " + newKey);
            }
        }
    }
}
