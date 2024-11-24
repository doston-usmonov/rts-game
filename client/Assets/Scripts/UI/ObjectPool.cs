using UnityEngine;
using System;
using System.Collections.Generic;

namespace RTS.UI
{
    public class ObjectPool<T> where T : Component
    {
        private Queue<T> pool = new Queue<T>();
        private List<T> activeObjects = new List<T>();
        private Func<T> createFunc;
        private int defaultCapacity;

        public void Initialize(Func<T> creator, int initialCapacity = 10)
        {
            createFunc = creator;
            defaultCapacity = initialCapacity;
            
            for (int i = 0; i < initialCapacity; i++)
            {
                T obj = createFunc();
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = createFunc();
            }
            
            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }

        public void Return(T obj)
        {
            if (activeObjects.Contains(obj))
            {
                activeObjects.Remove(obj);
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        public void ReturnAll()
        {
            foreach (T obj in activeObjects.ToArray())
            {
                Return(obj);
            }
        }
    }
}
