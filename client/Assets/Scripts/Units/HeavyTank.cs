using UnityEngine;
using RTS.Core;
using RTS.Units.Environment;

namespace RTS.Units
{
    public class HeavyTank : HeavyUnit
    {
        [Header("Tank Specifics")]
        [SerializeField] private float mainGunDamage = 75f;
        [SerializeField] private float secondaryGunDamage = 25f;
        [SerializeField] private float mainGunCooldown = 3f;
        [SerializeField] private float secondaryGunCooldown = 1f;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Components")]
        [SerializeField] private Transform turret;
        [SerializeField] private Transform mainGun;
        [SerializeField] private ParticleSystem mainGunEffect;
        [SerializeField] private ParticleSystem secondaryGunEffect;
        [SerializeField] private AudioSource mainGunAudio;
        [SerializeField] private AudioSource secondaryGunAudio;

        [Header("Environment")]
        [SerializeField] private UnitEnvironmentState environmentState;

        private float lastMainGunShot;
        private float lastSecondaryGunShot;

        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (!turret) turret = transform.Find("Turret");
            if (!mainGun) mainGun = turret?.Find("MainGun");
            
            if (!mainGunEffect) mainGunEffect = mainGun?.GetComponentInChildren<ParticleSystem>();
            if (!secondaryGunEffect) secondaryGunEffect = turret?.GetComponentInChildren<ParticleSystem>();
            
            if (!mainGunAudio) mainGunAudio = mainGun?.GetComponent<AudioSource>();
            if (!secondaryGunAudio) secondaryGunAudio = turret?.GetComponent<AudioSource>();

            environmentState ??= new UnitEnvironmentState();
        }

        protected override void Update()
        {
            base.Update();
            UpdateEnvironmentalEffects();
        }

        private void UpdateEnvironmentalEffects()
        {
            if (environmentState == null) return;

            // Get environmental data from the terrain or environment manager
            float temperature = GetTemperature();
            float moisture = GetMoisture();

            environmentState.UpdateEnvironmentalEffects(Time.deltaTime, temperature, moisture);

            // Apply environmental effects to unit performance
            float effectMultiplier = environmentState.GetOverallEffectMultiplier();
            UpdateUnitPerformance(effectMultiplier);
        }

        private void UpdateUnitPerformance(float effectMultiplier)
        {
            // Modify unit stats based on environmental effects
            moveSpeed *= effectMultiplier;
            rotationSpeed *= effectMultiplier;
            mainGunDamage *= effectMultiplier;
            secondaryGunDamage *= effectMultiplier;
        }

        private float GetTemperature()
        {
            // Implementation depends on your terrain/environment system
            return 0f; // Placeholder
        }

        private float GetMoisture()
        {
            // Implementation depends on your terrain/environment system
            return 0f; // Placeholder
        }

        public override void Attack(Unit target)
        {
            if (target == null) return;

            // Update environmental effects
            UpdateEnvironmentalEffects();

            // Apply environmental effects to damage
            float environmentalDamageMultiplier = environmentState.GetOverallEffectMultiplier();
            float adjustedMainGunDamage = mainGunDamage * environmentalDamageMultiplier;
            float adjustedSecondaryGunDamage = secondaryGunDamage * environmentalDamageMultiplier;

            // Rotate turret towards target
            if (turret != null)
            {
                Vector3 targetDirection = (target.transform.position - turret.position).normalized;
                targetDirection.y = 0f;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                turret.rotation = Quaternion.Slerp(turret.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance <= attackRange)
            {
                // Fire main gun if ready
                if (Time.time >= lastMainGunShot + mainGunCooldown)
                {
                    FireMainGun(target, adjustedMainGunDamage);
                }
                
                // Fire secondary gun if ready
                if (Time.time >= lastSecondaryGunShot + secondaryGunCooldown)
                {
                    FireSecondaryGun(target, adjustedSecondaryGunDamage);
                }
            }
        }

        private void FireMainGun(Unit target, float damage)
        {
            target.TakeDamage(damage);
            lastMainGunShot = Time.time;

            // Apply splash damage
            ApplySplashDamage(target.transform.position);

            // Play effects
            var effect = effectPool.GetObject();
            effect.transform.position = mainGun.position;
            effect.transform.rotation = mainGun.rotation;
            effect.Play();

            var audio = audioPool.GetObject();
            audio.transform.position = mainGun.position;
            audio.Play();
        }

        private void FireSecondaryGun(Unit target, float damage)
        {
            target.TakeDamage(damage);
            lastSecondaryGunShot = Time.time;

            // Play effects
            var effect = effectPool.GetObject();
            effect.transform.position = transform.position;
            effect.transform.rotation = transform.rotation;
            effect.Play();

            var audio = audioPool.GetObject();
            audio.transform.position = transform.position;
            audio.Play();
        }

        protected override bool ShouldApplyCrushDamage(Unit otherUnit)
        {
            // Heavy tanks can crush infantry and light vehicles
            return otherUnit.type == UnitType.Insurgent || 
                   otherUnit.type == UnitType.Drone ||
                   otherUnit.type == UnitType.TechnicalVehicle;
        }

        protected override void PlayAttackEffect()
        {
            // Additional tank-specific effects
            // e.g., track marks, dust clouds, etc.
        }

        protected override void PlayCrushEffect(Vector3 position)
        {
            // Tank-specific crush effects
            // e.g., metal crushing sounds, debris particles
        }

        public override void TakeDamage(float damage)
        {
            // Tanks take reduced damage from small arms
            if (damage < 20f)
            {
                damage *= 0.5f;
            }
            
            base.TakeDamage(damage);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw main gun range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw turret rotation
            if (turret != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(turret.position, turret.position + turret.forward * 5f);
            }
        }
    }
}
