using UnityEngine;
using RTS.Core;
using RTS.Factions;

namespace RTS.Units
{
    public class Unit : MonoBehaviour
    {
        [Header("Base Unit Properties")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float armor = 10f;
        [SerializeField] protected float moveSpeed = 5f;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackRange = 5f;
        [SerializeField] protected float attackSpeed = 1f;
        
        [Header("Unit State")]
        [SerializeField] protected bool isSelected = false;
        [SerializeField] protected bool isMoving = false;
        [SerializeField] protected bool isAttacking = false;

        protected FactionType currentFaction;
        protected Vector3 targetPosition;
        protected Unit targetUnit;
        protected float lastAttackTime;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            lastAttackTime = -attackSpeed; // Allow immediate first attack
        }

        public virtual void Initialize(FactionType faction)
        {
            currentFaction = faction;
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

        public virtual void Die()
        {
            // Base death behavior
            Destroy(gameObject);
        }

        public virtual void MoveTo(Vector3 position)
        {
            targetPosition = position;
            isMoving = true;
            isAttacking = false;
            targetUnit = null;
        }

        public virtual void Attack(Unit target)
        {
            if (target == null || target.currentFaction == currentFaction)
                return;

            targetUnit = target;
            isAttacking = true;
        }

        public virtual void Select()
        {
            isSelected = true;
        }

        public virtual void Deselect()
        {
            isSelected = false;
        }

        protected virtual void Update()
        {
            if (isMoving)
            {
                HandleMovement();
            }

            if (isAttacking)
            {
                HandleAttack();
            }
        }

        protected virtual void HandleMovement()
        {
            if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                transform.forward = direction;
            }
            else
            {
                isMoving = false;
            }
        }

        protected virtual void HandleAttack()
        {
            if (targetUnit == null || targetUnit.currentHealth <= 0)
            {
                isAttacking = false;
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, targetUnit.transform.position);
            
            if (distanceToTarget > attackRange)
            {
                // Move towards target if out of range
                MoveTo(targetUnit.transform.position);
                return;
            }

            // Attack if enough time has passed since last attack
            if (Time.time >= lastAttackTime + attackSpeed)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }

        protected virtual void PerformAttack()
        {
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(attackDamage);
            }
        }

        public float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }

        public bool IsAlive()
        {
            return currentHealth > 0;
        }

        public FactionType GetFaction()
        {
            return currentFaction;
        }
    }
}
