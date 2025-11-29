using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;

namespace EthanToolBox.Installer
{
    [InitializeOnLoad]
    public static class ToolBoxInstaller
    {
        private static AddRequest addRequest;
        private static ListRequest listRequest;

        static ToolBoxInstaller()
        {
            // Run on next frame to ensure Editor is fully initialized
            EditorApplication.delayCall += CheckDependencies;
        }

        private static void CheckDependencies()
        {
            // Check if we already checked this session to avoid spam
            if (SessionState.GetBool("EthanToolBox_CheckedDependencies", false)) return;
            SessionState.SetBool("EthanToolBox_CheckedDependencies", true);

            listRequest = Client.List();
            EditorApplication.update += ProgressCheck;
        }

        private static void ProgressCheck()
        {
            if (listRequest != null && listRequest.IsCompleted)
            {
                if (listRequest.Status == StatusCode.Success)
                {
                    bool hasTMP = listRequest.Result.Any(p => p.name == "com.unity.textmeshpro" || p.name == "com.unity.ugui");
                    if (!hasTMP)
                    {
                        if (EditorUtility.DisplayDialog("EthanToolBox Setup", 
                            "EthanToolBox requires TextMeshPro to function correctly.\n\nWould you like to install it now?", 
                            "Yes, Install TextMeshPro", "No, Later"))
                        {
                            InstallTMP();
                        }
                    }
                    else
                    {
                        CheckTMPEssentials();
                    }
                }
                else
                {
                    Debug.LogError("EthanToolBox: Failed to check packages: " + listRequest.Error.message);
                }
                
                EditorApplication.update -= ProgressCheck;
                listRequest = null;
            }

            if (addRequest != null && addRequest.IsCompleted)
            {
                if (addRequest.Status == StatusCode.Success)
                {
                    Debug.Log("EthanToolBox: TextMeshPro installed successfully!");
                    // Trigger recompile or check essentials
                    CheckTMPEssentials();
                }
                else
                {
                    Debug.LogError("EthanToolBox: Failed to install TextMeshPro: " + addRequest.Error.message);
                }
                
                EditorApplication.update -= ProgressCheck;
                addRequest = null;
            }
        }

        private static void InstallTMP()
        {
            Debug.Log("EthanToolBox: Installing TextMeshPro...");
            
            // In Unity 2023.2+ (and Unity 6), TMP is part of com.unity.ugui
#if UNITY_2023_2_OR_NEWER
            addRequest = Client.Add("com.unity.ugui");
#else
            addRequest = Client.Add("com.unity.textmeshpro");
#endif
            EditorApplication.update += ProgressCheck;
        }

        private static void CheckTMPEssentials()
        {
            // Check if TMP Settings exist (proxy for Essentials)
            // We can't access TMPro types here because we don't reference the assembly.
            // But we can check for the asset.
            string[] guids = AssetDatabase.FindAssets("TMP Settings t:ScriptableObject");
            
            if (guids.Length == 0)
            {
                if (EditorUtility.DisplayDialog("EthanToolBox Setup", 
                    "TextMeshPro is installed, but 'TMP Essentials' (Fonts, Shaders) seem missing.\n\nWithout them, text will be invisible (pink squares).\n\nWould you like to import them now?", 
                    "Yes, Import Essentials", "No, Later"))
                {
                    ImportEssentials();
                }
            }
        }

        private static void ImportEssentials()
        {
            // We use Reflection to call TMPro.EditorUtilities.TMP_PackageResourceImporter.ImportResources
            // This avoids a hard dependency on the assembly.
            
            System.Type importerType = System.Type.GetType("TMPro.EditorUtilities.TMP_PackageResourceImporter, Unity.TextMeshPro.Editor");
            if (importerType != null)
            {
                var method = importerType.GetMethod("ImportResources", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    // Arguments: bool interactive, bool includeExamples, bool includeEssentials
                    // Actually, the signature might vary by version.
                    // In 2021+, it's often just ImportResources(bool interactive, bool includeExamples, bool includeEssentials)
                    // Let's try to invoke it.
                    
                    // Wait, usually it opens a window.
                    // "TMPro.EditorUtilities.TMP_PackageResourceImporterWindow.ShowPackageImporterWindow()" is the standard way.
                    
                    System.Type windowType = System.Type.GetType("TMPro.EditorUtilities.TMP_PackageResourceImporterWindow, Unity.TextMeshPro.Editor");
                    if (windowType != null)
                    {
                        var showMethod = windowType.GetMethod("ShowPackageImporterWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (showMethod != null)
                        {
                            showMethod.Invoke(null, null);
                            return;
                        }
                    }
                }
            }
            
            Debug.LogWarning("EthanToolBox: Could not auto-open TMP Importer. Please go to Window > TextMeshPro > Import TMP Essentials.");
        }
    }
}
