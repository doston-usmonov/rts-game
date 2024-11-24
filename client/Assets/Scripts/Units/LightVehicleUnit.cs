using UnityEngine;
using RTS.Core;

namespace RTS.Units
{
    public class LightVehicleUnit : Unit
    {
        [Header("Light Vehicle Specific")]
        [SerializeField] protected float fuelMax = 100f;
        [SerializeField] protected float fuelCurrent;
        [SerializeField] protected float fuelConsumptionRate = 2f;
        [SerializeField] protected float engineDamageThreshold = 0.3f;

        [Header("Mobility")]
        [SerializeField] protected float accelerationRate = 5f;
        [SerializeField] protected float decelerationRate = 3f;
        [SerializeField] protected float turnSpeed = 120f;
        [SerializeField] protected float currentSpeed;
        [SerializeField] protected float maxReverseSpeed;

        protected bool isEngineDisabled;
        protected float currentTurnAngle;
        protected UnityEngine.AI.NavMeshAgent agent;

        protected override void Awake()
        {
            base.Awake();
            fuelCurrent = fuelMax;
            maxReverseSpeed = moveSpeed * 0.5f;
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        protected virtual void Update()
        {
            ConsumeFuel();
            CheckEngineStatus();
        }

        protected virtual void ConsumeFuel()
        {
            if (agent != null && agent.velocity.magnitude > 0.1f)
            {
                float consumption = fuelConsumptionRate * Time.deltaTime;
                fuelCurrent = Mathf.Max(0f, fuelCurrent - consumption);

                if (fuelCurrent <= 0f)
                {
                    DisableEngine();
                }
            }
        }

        protected virtual void CheckEngineStatus()
        {
            if (currentHealth <= (maxHealth * engineDamageThreshold) && !isEngineDisabled)
            {
                DisableEngine();
            }
        }

        protected virtual void DisableEngine()
        {
            isEngineDisabled = true;
            if (agent != null)
            {
                agent.speed = 0f;
                agent.enabled = false;
            }
        }

        public override void TakeDamage(float amount)
        {
            base.TakeDamage(amount);
            CheckEngineStatus();
        }

        public virtual void Accelerate()
        {
            if (!isEngineDisabled && fuelCurrent > 0)
            {
                currentSpeed = Mathf.Min(moveSpeed, currentSpeed + accelerationRate * Time.deltaTime);
                if (agent != null)
                {
                    agent.speed = currentSpeed;
                }
            }
        }

        public virtual void Decelerate()
        {
            currentSpeed = Mathf.Max(0f, currentSpeed - decelerationRate * Time.deltaTime);
            if (agent != null)
            {
                agent.speed = currentSpeed;
            }
        }

        public virtual void Reverse()
        {
            if (!isEngineDisabled && fuelCurrent > 0)
            {
                currentSpeed = Mathf.Max(-maxReverseSpeed, currentSpeed - accelerationRate * Time.deltaTime);
                if (agent != null)
                {
                    agent.speed = Mathf.Abs(currentSpeed);
                }
            }
        }

        public virtual void Turn(float direction)
        {
            if (!isEngineDisabled)
            {
                currentTurnAngle = direction * turnSpeed * Time.deltaTime;
                transform.Rotate(0f, currentTurnAngle, 0f);
            }
        }

        public float GetFuelPercentage() => (fuelCurrent / fuelMax) * 100f;
        public bool IsEngineDisabled() => isEngineDisabled;
        public float GetCurrentSpeed() => currentSpeed;
    }
}
