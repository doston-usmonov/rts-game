using UnityEngine;
using System.Collections.Generic;
using RTS.Units;

namespace RTS.UI.Components
{
    public class MapElementManager : MonoBehaviour
    {
        private Dictionary<int, GameObject> elementPool = new Dictionary<int, GameObject>();
        private List<GameObject> activeElements = new List<GameObject>();
        
        public GameObject CreateElement(GameObject prefab, Vector3 position)
        {
            GameObject element = GetFromPool(prefab);
            element.transform.position = position;
            activeElements.Add(element);
            return element;
        }

        private GameObject GetFromPool(GameObject prefab)
        {
            int prefabId = prefab.GetInstanceID();
            
            if (elementPool.ContainsKey(prefabId) && elementPool[prefabId] != null)
            {
                GameObject element = elementPool[prefabId];
                elementPool.Remove(prefabId);
                element.SetActive(true);
                return element;
            }
            
            return Instantiate(prefab);
        }

        public void ReturnToPool(GameObject element)
        {
            if (element == null) return;
            
            element.SetActive(false);
            int prefabId = element.GetInstanceID();
            elementPool[prefabId] = element;
            activeElements.Remove(element);
        }

        public void ClearAll()
        {
            foreach (var element in activeElements)
            {
                ReturnToPool(element);
            }
            activeElements.Clear();
        }
    }
}
