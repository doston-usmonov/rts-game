using UnityEngine;
using System;
using System.Collections.Generic;

namespace RTS.Core
{
    public class ObjectPool<T> where T : Component
    {
        private T prefab;
        private Transform parent;
        private Queue<T> pool;
        private List<T> activeObjects;
        private Action<T> onObjectSpawned;
        private Action<T> onObjectDespawned;

        public ObjectPool(T prefab, Transform parent = null, int initialSize = 10)
        {
            this.prefab = prefab;
            this.parent = parent;
            pool = new Queue<T>();
            activeObjects = new List<T>();

            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        public void SetCallbacks(Action<T> onSpawned = null, Action<T> onDespawned = null)
        {
            onObjectSpawned = onSpawned;
            onObjectDespawned = onDespawned;
        }

        private void CreateNewObject()
        {
            T obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }

        public T Get()
        {
            if (pool.Count == 0)
            {
                CreateNewObject();
            }

            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);
            onObjectSpawned?.Invoke(obj);
            return obj;
        }

        public T Get(Vector3 position)
        {
            T obj = Get();
            obj.transform.position = position;
            return obj;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null) return;

            obj.gameObject.SetActive(false);
            if (activeObjects.Remove(obj))
            {
                onObjectDespawned?.Invoke(obj);
                pool.Enqueue(obj);
            }
        }

        public void ReleaseAll()
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                Release(activeObjects[i]);
            }
        }

        public void Clear()
        {
            ReleaseAll();
            foreach (var obj in pool)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            pool.Clear();
            activeObjects.Clear();
        }

        public List<T> GetActiveObjects()
        {
            return new List<T>(activeObjects);
        }
    }
}
