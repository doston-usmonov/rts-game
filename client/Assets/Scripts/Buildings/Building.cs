using UnityEngine;
using RTS.Factions;

namespace RTS.Buildings
{
    public abstract class Building : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] protected float maxHealth = 1000f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float armor = 10f;
        [SerializeField] protected FactionType factionType;
        protected bool isSelected;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        public virtual void Initialize(FactionType faction)
        {
            factionType = faction;
        }

        public virtual void TakeDamage(float damage)
        {
            float damageAfterArmor = damage * (100 / (100 + armor));
            currentHealth = Mathf.Max(0, currentHealth - damageAfterArmor);

            if (currentHealth <= 0)
            {
                OnDestroyed();
            }
        }

        protected virtual void OnDestroyed()
        {
            Destroy(gameObject);
        }

        public FactionType GetFaction()
        {
            return factionType;
        }

        public float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }

        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            if (isSelected)
            {
                // Draw building footprint
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Bounds bounds = GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
