using System.Collections.Generic;
using UnityEngine;

namespace EthanToolBox.Core.SceneManagement
{
    [CreateAssetMenu(fileName = "NewSceneGroup", menuName = "EthanToolBox/Scene Management/Scene Group")]
    public class SceneGroup : ScriptableObject
    {
        public List<SceneReference> Scenes = new List<SceneReference>();
    }
}
