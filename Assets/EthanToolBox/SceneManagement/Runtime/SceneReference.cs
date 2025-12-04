using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EthanToolBox.Core.SceneManagement
{
    [Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset _sceneAsset;
#endif
        [SerializeField] private string _scenePath;

        public string ScenePath => _scenePath;

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (_sceneAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(_sceneAsset);
                if (!string.IsNullOrEmpty(path))
                {
                    _scenePath = path;
                }
            }
#endif
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
