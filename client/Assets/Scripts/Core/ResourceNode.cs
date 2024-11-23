using UnityEngine;

namespace RTS.Core
{
    public class ResourceNode : MonoBehaviour
    {
        [Header("Resource Properties")]
        public ResourceType type;
        public float maxAmount = 1000f;
        public float currentAmount;
        public float regenerationRate = 0f;  // Amount regenerated per second, 0 for non-regenerating resources

        [Header("Visualization")]
        public GameObject resourceModel;
        public ParticleSystem harvestEffect;
        public float visualScaleMultiplier = 0.001f;  // How much to scale the model per resource unit

        private void Start()
        {
            currentAmount = maxAmount;
            UpdateVisuals();
        }

        private void Update()
        {
            if (regenerationRate > 0 && currentAmount < maxAmount)
            {
                currentAmount = Mathf.Min(currentAmount + (regenerationRate * Time.deltaTime), maxAmount);
                UpdateVisuals();
            }
        }

        public float GatherResource(float amount)
        {
            if (currentAmount <= 0) return 0;

            float gathered = Mathf.Min(amount, currentAmount);
            currentAmount -= gathered;

            UpdateVisuals();
            PlayHarvestEffect();

            if (currentAmount <= 0)
            {
                OnDepletion();
            }

            return gathered;
        }

        private void UpdateVisuals()
        {
            if (resourceModel != null)
            {
                float scale = (currentAmount / maxAmount) * visualScaleMultiplier;
                resourceModel.transform.localScale = new Vector3(scale, scale, scale);
            }
        }

        private void PlayHarvestEffect()
        {
            if (harvestEffect != null && !harvestEffect.isPlaying)
            {
                harvestEffect.Play();
            }
        }

        private void OnDepletion()
        {
            // Handle resource depletion (e.g., play effect, disable gathering)
            if (regenerationRate <= 0)
            {
                // If resource doesn't regenerate, destroy it
                Destroy(gameObject);
            }
            else
            {
                // If it regenerates, just disable gathering temporarily
                if (resourceModel != null)
                {
                    resourceModel.SetActive(false);
                }
            }
        }

        public bool HasResources => currentAmount > 0;

        private void OnDrawGizmosSelected()
        {
            // Draw resource node range
            Gizmos.color = GetResourceColor();
            Gizmos.DrawWireSphere(transform.position, 1f);
        }

        private Color GetResourceColor()
        {
            switch (type)
            {
                case ResourceType.Gold:
                    return Color.yellow;
                case ResourceType.Power:
                    return Color.blue;
                default:
                    return Color.white;
            }
        }
    }

    public enum ResourceType
    {
        Gold,
        Power
    }
}
