using UnityEngine;
using RTS.Factions;
using RTS.Commands;
using RTS.Vision;
using RTS.Units.Combat;

namespace RTS.Units
{
    public abstract class Unit : MonoBehaviour
    {
        [Header("Unit Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackRange = 5f;
        [SerializeField] protected float attackSpeed = 1f;
        [SerializeField] protected float moveSpeed = 5f;
        [SerializeField] protected float armor = 5f;
        [SerializeField] protected float visionRange = 10f;

        protected float currentHealth;
        protected FactionType faction;
        protected bool isSelected;
        protected Vector3 targetPosition;
        protected bool isMoving;
        protected float nextAttackTime;

        // Public properties for external access
        public FactionType FactionType => faction;
        public float Health => currentHealth;
        public float MaxHealth => maxHealth;

        // Health management methods
        public virtual void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        public virtual float GetHealthPercentage()
        {
            return (currentHealth / maxHealth) * 100f;
        }

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        public virtual void Initialize(FactionType faction)
        {
            this.faction = faction;
        }

        protected virtual void Update()
        {
            if (isMoving)
            {
                UpdateMovement();
            }
        }

        public virtual void Stop()
        {
            isMoving = false;
            targetPosition = transform.position;
        }

        public virtual void MoveTo(Vector3 position)
        {
            targetPosition = position;
            isMoving = true;
        }

        protected virtual void UpdateMovement()
        {
            if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                transform.forward = direction;
            }
            else
            {
                Stop();
            }
        }

        public virtual void Attack(Unit target)
        {
            if (Time.time >= nextAttackTime)
            {
                target.TakeDamage(attackDamage);
                nextAttackTime = Time.time + 1f / attackSpeed;
                OnAttack(target);
            }
        }

        protected virtual void OnAttack(Unit target)
        {
            // Override in derived classes for attack effects
        }

        public virtual void TakeDamage(float damage)
        {
            float actualDamage = Mathf.Max(0, damage - armor);
            currentHealth -= actualDamage;

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        public virtual void Select()
        {
            isSelected = true;
        }

        public virtual void Deselect()
        {
            isSelected = false;
        }

        public FactionType GetFaction()
        {
            return faction;
        }

        public float GetHealth()
        {
            return currentHealth;
        }

        public float GetMaxHealth()
        {
            return maxHealth;
        }

        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw attack range
            if (isSelected)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, attackRange);
            }

            // Draw vision range
            if (isSelected)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, visionRange);
            }
        }

        public virtual void OnFormationBroken()
        {
            // Override in derived classes for formation break behavior
        }
    }
}
