using UnityEngine;
using RTS.Units.Combat;
using RTS.Factions;
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

        [Header("Tank Settings")]
        [SerializeField] private new float crushDamage = 50f;  // Explicitly hiding base member
        [SerializeField] private float crushRadius = 2f;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem attackEffect;
        [SerializeField] private ParticleSystem crushEffect;

        private float lastMainGunShot;
        private float lastSecondaryGunShot;

        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            base.InitializeComponents();
            
            // Initialize tank-specific components
            if (turret == null)
            {
                turret = transform.Find("Turret")?.gameObject;
            }
            
            if (mainGun == null)
            {
                mainGun = transform.Find("MainGun")?.gameObject;
            }
            
            // Set up effects
            if (mainGunEffect == null)
            {
                mainGunEffect = mainGun.GetComponentInChildren<ParticleSystem>();
            }
            
            if (secondaryGunEffect == null)
            {
                secondaryGunEffect = turret.GetComponentInChildren<ParticleSystem>();
            }
            
            // Initialize tank stats
            //currentArmor = maxArmor;
            //currentAmmo = maxAmmo;
            
            // Set up audio
            if (mainGunAudio == null)
            {
                mainGunAudio = mainGun.GetComponent<AudioSource>();
                if (mainGunAudio == null)
                {
                    mainGunAudio = mainGun.AddComponent<AudioSource>();
                    mainGunAudio.spatialBlend = 1f;
                    mainGunAudio.maxDistance = 30f;
                }
            }
            
            if (secondaryGunAudio == null)
            {
                secondaryGunAudio = turret.GetComponent<AudioSource>();
                if (secondaryGunAudio == null)
                {
                    secondaryGunAudio = turret.AddComponent<AudioSource>();
                    secondaryGunAudio.spatialBlend = 1f;
                    secondaryGunAudio.maxDistance = 30f;
                }
            }
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

        protected override bool ShouldApplyCrushDamage(Unit target)
        {
            if (target == null) return false;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            return distance <= crushRadius && target.GetType() != typeof(HeavyTank);
        }

        protected override void PlayAttackEffect()
        {
            if (attackEffect != null)
            {
                attackEffect.Play();
            }
        }

        protected override void PlayCrushEffect(Vector3 position)
        {
            if (crushEffect != null)
            {
                crushEffect.transform.position = position;
                crushEffect.Play();
            }
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

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!Application.isPlaying) return;

            if (isSelected)
            {
                // Draw crush damage radius
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, crushRadius);

                // Draw turret rotation range
                if (turret != null)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                    Vector3 forward = turret.transform.forward * attackRange;
                    Vector3 left = Quaternion.Euler(0, -maxTurretRotation, 0) * forward;
                    Vector3 right = Quaternion.Euler(0, maxTurretRotation, 0) * forward;
                    
                    Gizmos.DrawLine(turret.transform.position, turret.transform.position + left);
                    Gizmos.DrawLine(turret.transform.position, turret.transform.position + right);
                    Gizmos.DrawWireSphere(turret.transform.position, attackRange);
                }

                // Draw track marks preview
                //if (isMoving && trackMarks != null)
                //{
                //    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                //    Vector3 direction = (targetPosition - transform.position).normalized;
                //    Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                //    Vector3 leftTrack = transform.position - right * 1f;
                //    Vector3 rightTrack = transform.position + right * 1f;
                    
                //    Gizmos.DrawLine(leftTrack, leftTrack + direction * 3f);
                //    Gizmos.DrawLine(rightTrack, rightTrack + direction * 3f);
                //}
            }
        }
    }
}
