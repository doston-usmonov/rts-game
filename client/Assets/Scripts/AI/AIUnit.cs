using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace RTS.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIUnit : MonoBehaviour
    {
        [Header("Unit Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float attackRange = 10f;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackInterval = 1f;
        
        [Header("Movement")]
        [SerializeField] protected float baseSpeed = 5f;
        [SerializeField] protected float rotationSpeed = 120f;
        [SerializeField] protected float stoppingDistance = 0.1f;
        
        [Header("Combat Behavior")]
        [SerializeField] protected float aggressionRadius = 15f;
        [SerializeField] protected float flankingDistance = 8f;
        [SerializeField] protected float supportRange = 10f;

        protected float currentHealth;
        protected NavMeshAgent agent;
        protected bool isAggressive;
        protected bool isFlankingEnabled;
        protected bool isSupportingFire;
        protected Vector3? coverPosition;
        protected float lastAttackTime;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;
            ConfigureNavMeshAgent();
        }

        protected virtual void Start()
        {
            // Initialize default behavior
            isAggressive = false;
            isFlankingEnabled = false;
            isSupportingFire = false;
        }

        protected virtual void Update()
        {
            UpdateBehavior();
        }

        protected virtual void ConfigureNavMeshAgent()
        {
            if (agent != null)
            {
                agent.speed = baseSpeed;
                agent.angularSpeed = rotationSpeed;
                agent.stoppingDistance = stoppingDistance;
                agent.acceleration = 8f;
                agent.autoBraking = true;
            }
        }

        public virtual void SetDestination(Vector3 position)
        {
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.SetDestination(position);
            }
        }

        public virtual void SetAggressive(bool aggressive)
        {
            isAggressive = aggressive;
            if (aggressive)
            {
                agent.stoppingDistance = attackRange * 0.8f;
            }
            else
            {
                agent.stoppingDistance = stoppingDistance;
            }
        }

        public virtual void EnableFlanking(bool enable)
        {
            isFlankingEnabled = enable;
        }

        public virtual void ProvideSupportFire(bool enable)
        {
            isSupportingFire = enable;
        }

        public virtual void UseCover(Vector3 position)
        {
            coverPosition = position;
            SetDestination(position);
        }

        public virtual float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }

        public virtual float GetAttackRange()
        {
            return attackRange;
        }

        protected virtual void UpdateBehavior()
        {
            if (isAggressive)
            {
                UpdateAggressiveBehavior();
            }
            else if (isFlankingEnabled)
            {
                UpdateFlankingBehavior();
            }
            else if (isSupportingFire)
            {
                UpdateSupportBehavior();
            }
            else if (coverPosition.HasValue)
            {
                UpdateCoverBehavior();
            }
        }

        protected virtual void UpdateAggressiveBehavior()
        {
            var nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.position);
                if (distanceToEnemy <= aggressionRadius)
                {
                    SetDestination(nearestEnemy.position);
                    if (distanceToEnemy <= attackRange)
                    {
                        AttackTarget(nearestEnemy);
                    }
                }
            }
        }

        protected virtual void UpdateFlankingBehavior()
        {
            var nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                Vector3 flankPosition = CalculateFlankPosition(nearestEnemy.position);
                SetDestination(flankPosition);
            }
        }

        protected virtual void UpdateSupportBehavior()
        {
            var friendlyUnits = FindFriendlyUnits();
            if (friendlyUnits.Count > 0)
            {
                Vector3 supportPosition = CalculateSupportPosition(friendlyUnits);
                SetDestination(supportPosition);
            }
        }

        protected virtual void UpdateCoverBehavior()
        {
            if (!coverPosition.HasValue) return;

            var nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                Vector3 directionToEnemy = (nearestEnemy.position - transform.position).normalized;
                Vector3 coverDirection = (coverPosition.Value - transform.position).normalized;

                // If cover is between unit and enemy, stay put
                if (Vector3.Dot(directionToEnemy, coverDirection) > 0.7f)
                {
                    agent.isStopped = true;
                }
                else
                {
                    agent.isStopped = false;
                    SetDestination(coverPosition.Value);
                }
            }
        }

        protected virtual Transform FindNearestEnemy()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy.transform;
                }
            }

            return nearest;
        }

        protected virtual List<Transform> FindFriendlyUnits()
        {
            var friendlies = new List<Transform>();
            var units = GameObject.FindGameObjectsWithTag("Friendly");

            foreach (var unit in units)
            {
                if (unit.transform != transform)
                {
                    friendlies.Add(unit.transform);
                }
            }

            return friendlies;
        }

        protected virtual Vector3 CalculateFlankPosition(Vector3 targetPosition)
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            Vector3 perpendicularDirection = new Vector3(-directionToTarget.z, 0, directionToTarget.x);
            
            // Try both left and right flanking positions
            Vector3 leftFlank = targetPosition + (perpendicularDirection * flankingDistance);
            Vector3 rightFlank = targetPosition - (perpendicularDirection * flankingDistance);
            
            // Choose the flanking position that's further from other friendly units
            float leftScore = ScoreFlankingPosition(leftFlank);
            float rightScore = ScoreFlankingPosition(rightFlank);
            
            return leftScore > rightScore ? leftFlank : rightFlank;
        }

        protected virtual float ScoreFlankingPosition(Vector3 position)
        {
            float score = 0f;
            var friendlyUnits = FindFriendlyUnits();
            
            foreach (var friendly in friendlyUnits)
            {
                float distance = Vector3.Distance(position, friendly.position);
                score += distance; // Higher score for positions further from friendlies
            }
            
            return score;
        }

        protected virtual Vector3 CalculateSupportPosition(List<Transform> friendlyUnits)
        {
            Vector3 averagePosition = Vector3.zero;
            foreach (var friendly in friendlyUnits)
            {
                averagePosition += friendly.position;
            }
            averagePosition /= friendlyUnits.Count;

            // Position slightly behind the average position of friendly units
            var nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                Vector3 directionToEnemy = (nearestEnemy.position - averagePosition).normalized;
                return averagePosition - (directionToEnemy * supportRange);
            }

            return averagePosition;
        }

        protected virtual void AttackTarget(Transform target)
        {
            if (Time.time - lastAttackTime >= attackInterval)
            {
                // Implement attack logic here
                lastAttackTime = Time.time;
            }
        }

        public virtual void TakeDamage(float damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            // Implement death logic here
            Destroy(gameObject);
        }
    }
}
