using UnityEngine;
using System;
using System.Collections.Generic;

namespace RTS.UI
{
    public class ObjectPool<T> : MonoBehaviour where T : Component
    {
        private Queue<T> pool = new Queue<T>();
        private List<T> activeObjects = new List<T>();
        private Func<T> createFunc;
        private int defaultCapacity;

        public void Initialize(Func<T> creator, int initialCapacity = 20)
        {
            createFunc = creator;
            defaultCapacity = initialCapacity;
            
            // Pre-instantiate objects
            for (int i = 0; i < initialCapacity; i++)
            {
                var obj = creator();
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;
            if (pool.Count == 0)
            {
                // Create new object if pool is empty
                obj = createFunc();
            }
            else
            {
                obj = pool.Dequeue();
            }

            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }

        public void ReturnToPool(T obj)
        {
            if (obj == null) return;

            obj.gameObject.SetActive(false);
            activeObjects.Remove(obj);
            pool.Enqueue(obj);
        }

        public void ReturnAllToPool()
        {
            foreach (var obj in activeObjects.ToArray())
            {
                ReturnToPool(obj);
            }
            activeObjects.Clear();
        }

        private void OnDestroy()
        {
            // Clean up all objects
            foreach (var obj in pool)
            {
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
            foreach (var obj in activeObjects)
            {
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
            
            pool.Clear();
            activeObjects.Clear();
        }
    }
}
