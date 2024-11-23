using UnityEngine;
using RTS.Core;
using RTS.Factions;

namespace RTS.Units
{
    public class DroneController : Unit
    {
        [Header("Drone Settings")]
        public float gatheringRate = 2f;
        public float gatheringRange = 3f;
        public float returnRange = 5f;
        public float cargoCapacity = 50f;

        private float currentCargo = 0f;
        private bool isGathering = false;
        private ResourceNode currentResource;
        private Building dropOffPoint;

        public bool IsGathering => isGathering;
        public float GatheringRate => gatheringRate;

        protected override void Awake()
        {
            base.Awake();
            type = UnitType.Drone;
        }

        public override void Initialize(FactionType faction)
        {
            base.Initialize(faction);
            
            // Register with TechnologicalFaction
            var techFaction = GameObject.FindObjectOfType<TechnologicalFaction>();
            if (techFaction != null)
            {
                techFaction.RegisterDrone(this);
            }
        }

        public void AssignResourceGathering(ResourceNode resource, Building dropOff)
        {
            currentResource = resource;
            dropOffPoint = dropOff;
            StartGathering();
        }

        private void StartGathering()
        {
            if (currentResource == null || dropOffPoint == null) return;

            isGathering = true;
            MoveTo(currentResource.transform.position);
        }

        private void Update()
        {
            if (!isGathering) return;

            if (currentResource != null && currentCargo < cargoCapacity)
            {
                // Check if we're in range of the resource
                float distanceToResource = Vector3.Distance(transform.position, currentResource.transform.position);
                if (distanceToResource <= gatheringRange)
                {
                    GatherResources();
                }
            }
            else if (currentCargo >= cargoCapacity || currentResource == null)
            {
                ReturnResources();
            }
        }

        private void GatherResources()
        {
            if (currentResource.HasResources)
            {
                float gathered = currentResource.GatherResource(gatheringRate * Time.deltaTime);
                currentCargo += gathered;

                if (currentCargo >= cargoCapacity)
                {
                    ReturnResources();
                }
            }
            else
            {
                // Resource depleted, find new resource or return to base
                ReturnResources();
            }
        }

        private void ReturnResources()
        {
            if (dropOffPoint != null)
            {
                MoveTo(dropOffPoint.transform.position);
                
                // Check if we're in range of the drop-off point
                float distanceToDropOff = Vector3.Distance(transform.position, dropOffPoint.transform.position);
                if (distanceToDropOff <= returnRange)
                {
                    DepositResources();
                }
            }
        }

        private void DepositResources()
        {
            if (currentCargo > 0)
            {
                // Add resources to faction's stockpile
                var techFaction = GameObject.FindObjectOfType<TechnologicalFaction>();
                if (techFaction != null)
                {
                    techFaction.gold += currentCargo;
                }

                currentCargo = 0;
                
                // Return to gathering if resource still exists
                if (currentResource != null && currentResource.HasResources)
                {
                    StartGathering();
                }
                else
                {
                    isGathering = false;
                }
            }
        }

        public override void Stop()
        {
            base.Stop();
            isGathering = false;
            currentResource = null;
            dropOffPoint = null;
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

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // Draw gathering range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, gatheringRange);
            
            // Draw return range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, returnRange);
        }
    }
}
