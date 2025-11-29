using System.Collections.Generic;
using UnityEngine;

namespace EthanToolBox.Core.SceneManagement
{
    [CreateAssetMenu(fileName = "NewSceneGroup", menuName = "EthanToolBox/Scene Management/Scene Group")]
    public class SceneGroup : ScriptableObject
    {
        [Tooltip("The name of the active scene in this group.")]
        public string ActiveSceneName;

        [Tooltip("List of all scenes to load in this group (including the active one).")]
        public List<string> Scenes = new List<string>();
    }
}
