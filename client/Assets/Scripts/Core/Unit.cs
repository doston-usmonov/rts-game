using UnityEngine;
using UnityEngine.AI;

namespace RTS.Core
{
    public abstract class Unit : MonoBehaviour
    {
        [Header("Unit Properties")]
        public string unitName;
        public UnitType type;
        public FactionType factionType;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float currentHealth;
        public float armor = 10f;
        public float attackDamage = 20f;
        public float attackRange = 5f;
        public float attackSpeed = 1f;
        public float moveSpeed = 5f;

        [Header("Cost")]
        public float goldCost;
        public float powerCost;
        public float buildTime;

        protected NavMeshAgent agent;
        protected Unit currentTarget;
        protected Building targetBuilding;
        protected float lastAttackTime;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;
            if (agent != null)
            {
                agent.speed = moveSpeed;
            }
        }

        public virtual void Initialize(FactionType faction)
        {
            factionType = faction;
        }

        public virtual void MoveTo(Vector3 destination)
        {
            if (agent != null)
            {
                agent.SetDestination(destination);
            }
        }

        public virtual void Attack(Unit target)
        {
            if (target == null) return;

            currentTarget = target;
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance <= attackRange && Time.time >= lastAttackTime + attackSpeed)
            {
                PerformAttack();
            }
        }

        protected virtual void PerformAttack()
        {
            if (currentTarget != null)
            {
                currentTarget.TakeDamage(attackDamage);
                lastAttackTime = Time.time;
            }
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
            // Handle unit death (e.g., play animation, spawn effects)
            Destroy(gameObject);
        }

        public virtual void Stop()
        {
            if (agent != null)
            {
                agent.ResetPath();
            }
            currentTarget = null;
            targetBuilding = null;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            // Draw attack range in editor
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }

    public enum UnitType
    {
        // Tech Faction Units
        Drone,
        LaserTank,
        StealthJet,

        // Heavy Assault Units
        HeavyTank,
        Artillery,
        Helicopter,

        // Guerrilla Units
        Insurgent,
        TechnicalVehicle,
        SaboteurUnit
    }
}
