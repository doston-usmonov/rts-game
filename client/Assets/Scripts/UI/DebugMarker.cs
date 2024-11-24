using UnityEngine;
using RTS.Core.Pooling;

namespace RTS.UI
{
    public class DebugMarker : MonoBehaviour, IPoolable
    {
        private MeshRenderer meshRenderer;
        private ObjectPool<DebugMarker> pool;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Initialize(ObjectPool<DebugMarker> pool)
        {
            this.pool = pool;
        }

        public void SetColor(Color color)
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = color;
            }
        }

        public void SetScale(Vector3 scale)
        {
            transform.localScale = scale;
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
        }

        public void OnDespawn()
        {
            gameObject.SetActive(false);
        }

        public void ReturnToPool()
        {
            if (pool != null)
            {
                pool.ReturnToPool(this);
            }
        }
    }
}
