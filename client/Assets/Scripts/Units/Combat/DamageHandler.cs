using UnityEngine;

namespace RTS.Units.Combat
{
    public class DamageHandler : MonoBehaviour
    {
        [Header("Damage Settings")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float armorPenetration = 0f;
        [SerializeField] private bool ignoreArmor = false;
        
        [Header("Critical Hit")]
        [SerializeField] private float criticalHitChance = 0.1f;
        [SerializeField] private float criticalHitMultiplier = 2f;

        public float CalculateDamage(float baseDamage, float targetArmor)
        {
            float finalDamage = baseDamage * damageMultiplier;

            // Check for critical hit
            if (Random.value <= criticalHitChance)
            {
                finalDamage *= criticalHitMultiplier;
            }

            // Apply armor calculations
            if (!ignoreArmor)
            {
                float effectiveArmor = Mathf.Max(0, targetArmor - armorPenetration);
                finalDamage = finalDamage * (100 / (100 + effectiveArmor));
            }

            return Mathf.Max(0, finalDamage);
        }

        public void SetDamageMultiplier(float multiplier)
        {
            damageMultiplier = multiplier;
        }

        public void SetArmorPenetration(float penetration)
        {
            armorPenetration = penetration;
        }

        public void SetCriticalHitChance(float chance)
        {
            criticalHitChance = Mathf.Clamp01(chance);
        }

        public void SetCriticalHitMultiplier(float multiplier)
        {
            criticalHitMultiplier = Mathf.Max(1, multiplier);
        }

        public void EnableArmorPiercing(bool enable)
        {
            ignoreArmor = enable;
        }
    }
}
