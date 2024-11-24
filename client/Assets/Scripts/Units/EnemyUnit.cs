using UnityEngine;
using RTS.Units.Combat;
using RTS.Factions;

namespace RTS.Units
{
    public class EnemyUnit : Unit
    {
        [Header("Enemy Unit Settings")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float aggroRange = 10f;
        
        private Unit currentTarget;
        private bool isAggressive = true;

        protected override void Awake()
        {
            base.Awake();
            // Enemy-specific initialization
        }

        public override void Initialize(FactionType faction)
        {
            base.Initialize(faction);
            // Additional enemy initialization
        }

        protected override void Update()
        {
            base.Update();
            
            if (!isAggressive) return;

            // Look for targets
            if (currentTarget == null)
            {
                FindNewTarget();
            }
            else
            {
                // Attack if in range
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (distanceToTarget <= attackRange)
                {
                    Attack(currentTarget);
                }
                else if (distanceToTarget <= aggroRange)
                {
                    MoveTo(currentTarget.transform.position);
                }
                else
                {
                    currentTarget = null;
                }
            }
        }

        private void FindNewTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
            float closestDistance = float.MaxValue;
            Unit closestTarget = null;

            foreach (Collider col in colliders)
            {
                Unit potentialTarget = col.GetComponent<Unit>();
                if (potentialTarget != null && potentialTarget.GetFaction() != GetFaction())
                {
                    float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = potentialTarget;
                    }
                }
            }

            if (closestTarget != null && closestDistance <= aggroRange)
            {
                currentTarget = closestTarget;
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!Application.isPlaying) return;

            if (isSelected)
            {
                // Draw detection range
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, detectionRange);

                // Draw aggro range
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, aggroRange);

                // Draw line to current target if exists
                if (currentTarget != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, currentTarget.transform.position);
                }
            }
        }
    }
}
