using UnityEngine;
using RTS.Core;

namespace RTS.Units
{
    public class InfantryUnit : Unit
    {
        [Header("Infantry Specific")]
        [SerializeField] protected float staminaMax = 100f;
        [SerializeField] protected float staminaCurrent;
        [SerializeField] protected float staminaRegenRate = 5f;
        [SerializeField] protected float sprintSpeedMultiplier = 1.5f;
        [SerializeField] protected float sprintStaminaCost = 10f;

        [Header("Cover System")]
        [SerializeField] protected float coverDamageReduction = 0.5f;
        [SerializeField] protected float coverDetectionRange = 5f;
        [SerializeField] protected LayerMask coverMask;

        protected bool isInCover;
        protected bool isSprinting;
        protected Transform currentCover;
        protected UnityEngine.AI.NavMeshAgent agent;

        protected override void Awake()
        {
            base.Awake();
            staminaCurrent = staminaMax;
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        protected virtual void Update()
        {
            RegenerateStamina();
            UpdateCoverStatus();
        }

        protected virtual void RegenerateStamina()
        {
            if (!isSprinting && staminaCurrent < staminaMax)
            {
                staminaCurrent = Mathf.Min(staminaMax, staminaCurrent + staminaRegenRate * Time.deltaTime);
            }
        }

        protected virtual void UpdateCoverStatus()
        {
            // Simple cover detection
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, coverDetectionRange, coverMask);
            if (nearbyObjects.Length > 0)
            {
                Transform nearestCover = null;
                float nearestDistance = float.MaxValue;

                foreach (var obj in nearbyObjects)
                {
                    float distance = Vector3.Distance(transform.position, obj.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestCover = obj.transform;
                        nearestDistance = distance;
                    }
                }

                currentCover = nearestCover;
                isInCover = currentCover != null;
            }
            else
            {
                currentCover = null;
                isInCover = false;
            }
        }

        public virtual void StartSprint()
        {
            if (staminaCurrent > 0)
            {
                isSprinting = true;
                if (agent != null)
                {
                    agent.speed = moveSpeed * sprintSpeedMultiplier;
                }
            }
        }

        public virtual void StopSprint()
        {
            isSprinting = false;
            if (agent != null)
            {
                agent.speed = moveSpeed;
            }
        }

        public override void TakeDamage(float amount)
        {
            // Apply cover damage reduction if in cover
            if (isInCover)
            {
                amount *= (1f - coverDamageReduction);
            }

            base.TakeDamage(amount);
        }

        public bool IsInCover() => isInCover;
        public float GetStaminaPercentage() => (staminaCurrent / staminaMax) * 100f;
        public bool IsSprinting() => isSprinting;
    }
}
