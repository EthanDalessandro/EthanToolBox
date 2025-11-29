using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Gets the component of type T. If it doesn't exist, adds it and returns the new instance.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Checks if the GameObject has a component of type T.
        /// </summary>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }
        
        /// <summary>
        /// Sets the layer of the GameObject and all its children.
        /// </summary>
        public static void SetLayerRecursive(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursive(layer);
            }
        }

        /// <summary>
        /// Activates the GameObject.
        /// </summary>
        public static void Show(this GameObject gameObject)
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Deactivates the GameObject.
        /// </summary>
        public static void Hide(this GameObject gameObject)
        {
            gameObject.SetActive(false);
        }
    }
}
