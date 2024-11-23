using UnityEngine;
using System.Collections;

namespace RTS.Units.Combat
{
    [System.Serializable]
    public class TacticalRetreat
    {
        [Header("Retreat Settings")]
        public float healthThreshold = 0.3f;
        public float retreatSpeed = 1.5f;
        public float retreatDuration = 5f;
        public float retreatCooldown = 30f;
        
        [Header("Smoke Screen")]
        public bool hasSmoke = true;
        public float smokeDuration = 3f;
        public float smokeRadius = 5f;
        
        [Header("Effects")]
        public ParticleSystem smokeEffect;
        public AudioClip retreatSound;
        
        private bool isRetreating = false;
        private float lastRetreatTime = -999f;
        private Vector3 retreatDestination;
        private MonoBehaviour owner;
        
        public void Initialize(MonoBehaviour unit)
        {
            owner = unit;
        }
        
        public bool ShouldRetreat(float currentHealth, float maxHealth)
        {
            if (isRetreating) return true;
            
            return currentHealth / maxHealth <= healthThreshold && 
                   Time.time >= lastRetreatTime + retreatCooldown;
        }
        
        public IEnumerator PerformRetreat(Vector3 currentPosition, Vector3 fallbackPosition)
        {
            if (isRetreating) yield break;
            
            isRetreating = true;
            lastRetreatTime = Time.time;
            retreatDestination = fallbackPosition;
            
            // Deploy smoke screen
            if (hasSmoke && smokeEffect != null)
            {
                smokeEffect.Play();
                yield return new WaitForSeconds(smokeDuration);
                smokeEffect.Stop();
            }
            
            // Play retreat sound
            if (retreatSound != null)
            {
                AudioSource.PlayClipAtPoint(retreatSound, currentPosition);
            }
            
            // Perform retreat movement
            float startTime = Time.time;
            Vector3 startPosition = currentPosition;
            
            while (Time.time - startTime < retreatDuration)
            {
                float t = (Time.time - startTime) / retreatDuration;
                owner.transform.position = Vector3.Lerp(startPosition, retreatDestination, t);
                yield return null;
            }
            
            isRetreating = false;
        }
        
        public bool IsRetreating()
        {
            return isRetreating;
        }
        
        public Vector3 GetRetreatDestination()
        {
            return retreatDestination;
        }
    }
}
