using UnityEngine;
using RTS.Units.Combat;
using RTS.Factions;
using RTS.Resources;
using RTS.Buildings;

namespace RTS.Units
{
    public class DroneController : Unit
    {
        [Header("Drone Settings")]
        [SerializeField] private float gatherRadius = 5f;
        [SerializeField] private float resourceGatherRate = 1f;
        [SerializeField] private float harvestAmount = 5f;
        [SerializeField] private float depositRange = 2f;
        [SerializeField] protected float gatheringRate = 5f;
        [SerializeField] protected float maxCarryCapacity = 100f;
        protected float currentLoad = 0f;
        
        private ResourceNode currentResource;
        private Building depositTarget;
        private bool isHarvesting;
        
        public bool IsGathering { get; private set; }
        public float GatheringRate => resourceGatherRate;
        public float CurrentLoad => currentLoad;

        protected override void Awake()
        {
            base.Awake();
            // Drone-specific initialization
        }

        public override void Initialize(FactionType faction)
        {
            base.Initialize(faction);
            // Additional drone initialization
            
            // Register with TechnologicalFaction
            var techFaction = GameObject.FindObjectOfType<TechnologicalFaction>();
            if (techFaction != null)
            {
                techFaction.RegisterDrone(this);
            }
        }

        public void AssignResourceGathering(ResourceNode resource, Building dropOff)
        {
            StartGathering();
            currentResource = resource;
            depositTarget = dropOff;
        }

        public void StartGathering()
        {
            IsGathering = true;
            isHarvesting = true;
        }

        public void StopGathering()
        {
            IsGathering = false;
            isHarvesting = false;
        }

        protected override void Update()
        {
            base.Update();
            
            if (!IsGathering) return;

            if (isHarvesting && currentResource != null)
            {
                if (Vector3.Distance(transform.position, currentResource.transform.position) <= gatherRadius)
                {
                    float harvested = currentResource.Harvest(harvestAmount);
                    if (harvested > 0 && depositTarget != null)
                    {
                        MoveTo(depositTarget.transform.position);
                    }
                }
            }
        }

        public override void Stop()
        {
            base.Stop();
            isHarvesting = false;
            currentResource = null;
            depositTarget = null;
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!Application.isPlaying) return;

            if (isSelected)
            {
                // Draw deposit range
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, depositRange);

                // Draw gathering path if active
                if (currentResource != null && depositTarget != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, currentResource.transform.position);
                    Gizmos.DrawLine(currentResource.transform.position, depositTarget.transform.position);
                }
            }
        }

        private void OnDestroy()
        {
            // Unregister from TechnologicalFaction
            var techFaction = GameObject.FindObjectOfType<TechnologicalFaction>();
            if (techFaction != null)
            {
                techFaction.UnregisterDrone(this);
            }
        }
    }
}
