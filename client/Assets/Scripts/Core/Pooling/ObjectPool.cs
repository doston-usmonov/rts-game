using UnityEngine;
using System.Collections.Generic;

namespace RTS.Core.Pooling
{
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> pool;
        private readonly int initialSize;

        public ObjectPool(T prefab, Transform parent, int initialSize = 10)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.initialSize = initialSize;
            this.pool = new Queue<T>();
            Initialize();
        }

        private void Initialize()
        {
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewInstance();
            }
        }

        private void CreateNewInstance()
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            pool.Enqueue(instance);
        }

        public T Get()
        {
            if (pool.Count == 0)
            {
                CreateNewInstance();
            }

            T instance = pool.Dequeue();
            instance.OnSpawn();
            return instance;
        }

        public T Get(Vector3 position)
        {
            T instance = Get();
            instance.transform.position = position;
            return instance;
        }

        public void ReturnToPool(T instance)
        {
            if (instance != null)
            {
                instance.OnDespawn();
                pool.Enqueue(instance);
            }
        }

        public void Clear()
        {
            while (pool.Count > 0)
            {
                T instance = pool.Dequeue();
                if (instance != null)
                {
                    Object.Destroy(instance.gameObject);
                }
            }
        }
    }
}
