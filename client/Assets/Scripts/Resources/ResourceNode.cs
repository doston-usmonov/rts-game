using UnityEngine;

namespace RTS.Resources
{
    public class ResourceNode : MonoBehaviour
    {
        [Header("Resource Settings")]
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private float maxResources = 1000f;
        [SerializeField] private float currentResources;
        [SerializeField] private float harvestRate = 10f;
        [SerializeField] private float respawnTime = 300f;
        
        private float lastHarvestTime;
        private bool isDepleted;

        private void Awake()
        {
            currentResources = maxResources;
        }

        private void Update()
        {
            if (isDepleted)
            {
                if (Time.time - lastHarvestTime >= respawnTime)
                {
                    Respawn();
                }
            }
        }

        public bool CanHarvest()
        {
            return !isDepleted && currentResources > 0;
        }

        public float Harvest(float amount)
        {
            if (!CanHarvest()) return 0f;

            float harvestedAmount = Mathf.Min(amount * harvestRate * Time.deltaTime, currentResources);
            currentResources -= harvestedAmount;

            if (currentResources <= 0)
            {
                isDepleted = true;
                lastHarvestTime = Time.time;
            }

            return harvestedAmount;
        }

        private void Respawn()
        {
            currentResources = maxResources;
            isDepleted = false;
        }

        public ResourceType GetResourceType()
        {
            return resourceType;
        }

        public float GetCurrentResources()
        {
            return currentResources;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isDepleted ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    public enum ResourceType
    {
        Metal,
        Energy,
        Crystal
    }
}
