using UnityEngine;
using System.Collections;
using RTS.Core;

namespace RTS.Units
{
    public class Artillery : HeavyUnit
    {
        [Header("Artillery Settings")]
        public float minRange = 10f;
        public float maxRange = 40f;
        public float deployedRangeBonus = 15f;
        public float deployTime = 2f;
        public float undeployTime = 1.5f;
        public float accuracyRadius = 3f;        // Area where shells might land
        public float trajectoryHeight = 20f;     // Max height of the artillery shell
        public float shellTravelTime = 2f;       // Time for shell to reach target
        
        [Header("Deployed State Bonuses")]
        public float deployedAccuracyBonus = 0.5f;  // Reduces accuracy radius
        public float deployedDamageBonus = 1.25f;   // Increases damage
        public float deployedFireRateBonus = 0.8f;  // Reduces cooldown

        [Header("Visual Effects")]
        public LineRenderer trajectoryLine;
        public GameObject deployedModel;
        public GameObject undeployedModel;
        public ParticleSystem deployEffect;
        public ParticleSystem muzzleFlash;
        public GameObject shellPrefab;
        public GameObject explosionPrefab;

        [Header("Ammo System")]
        public float maxAmmo = 100f;
        public float ammoPerShot = 10f;
        public float lowAmmoThreshold = 30f;
        
        [Header("Shield Effect")]
        public GameObject shieldEffectPrefab;
        public ParticleSystem ammoResupplyEffect;

        private bool isDeployed = false;
        private bool isDeploying = false;
        private Vector3? targetPosition;
        private float baseAccuracyRadius;
        private float baseDamage;
        private float baseAttackSpeed;
        private float currentAmmo;
        private GameObject activeShieldEffect;
        private DamageHandler damageHandler;

        protected override void Awake()
        {
            base.Awake();
            type = UnitType.Artillery;

            // Set base stats
            maxHealth = 200f;
            armor = 15f;
            moveSpeed = 3f;
            attackRange = maxRange;
            splashRadius = 5f;
            splashDamageMultiplier = 0.75f;

            // Store base values for deployed state calculations
            baseAccuracyRadius = accuracyRadius;
            baseDamage = attackDamage;
            baseAttackSpeed = attackSpeed;

            // Initialize visual components
            if (deployedModel) deployedModel.SetActive(false);
            if (trajectoryLine) trajectoryLine.enabled = false;

            // Initialize ammo and damage handler
            currentAmmo = maxAmmo;
            damageHandler = gameObject.AddComponent<DamageHandler>();
        }

        public override void MoveTo(Vector3 destination)
        {
            if (isDeployed)
            {
                // Can't move while deployed
                return;
            }

            base.MoveTo(destination);
        }

        public override void Attack(Unit target)
        {
            if (target == null) return;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            // Check if target is within valid range
            if (distance < minRange)
            {
                // Too close to target
                return;
            }

            if (distance <= attackRange && Time.time >= lastAttackTime + attackSpeed)
            {
                targetPosition = target.transform.position;
                FireArtilleryShell();
            }
        }

        public void AttackPosition(Vector3 position)
        {
            float distance = Vector3.Distance(transform.position, position);
            
            if (distance < minRange || distance > attackRange)
            {
                return;
            }

            if (Time.time >= lastAttackTime + attackSpeed)
            {
                targetPosition = position;
                FireArtilleryShell();
            }
        }

        private void FireArtilleryShell()
        {
            if (!targetPosition.HasValue || currentAmmo < ammoPerShot) return;

            // Consume ammo
            currentAmmo -= ammoPerShot;

            // Calculate actual impact position with accuracy deviation
            Vector2 randomOffset = Random.insideUnitCircle * accuracyRadius;
            Vector3 impactPosition = targetPosition.Value + new Vector3(randomOffset.x, 0, randomOffset.y);

            // Spawn shell and set up trajectory
            StartCoroutine(SimulateArtilleryShell(impactPosition));

            // Play effects
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            lastAttackTime = Time.time;
        }

        public float GetAmmoPercentage()
        {
            return currentAmmo / maxAmmo * 100f;
        }

        public bool IsLowOnAmmo()
        {
            return currentAmmo <= lowAmmoThreshold;
        }

        public void SetShieldActive(bool active)
        {
            if (active && activeShieldEffect == null && shieldEffectPrefab != null)
            {
                activeShieldEffect = Instantiate(shieldEffectPrefab, transform);
            }
            else if (!active && activeShieldEffect != null)
            {
                Destroy(activeShieldEffect);
                activeShieldEffect = null;
            }
        }

        public void TriggerResupplyEffect()
        {
            if (ammoResupplyEffect != null && !ammoResupplyEffect.isPlaying)
            {
                ammoResupplyEffect.Play();
            }
        }

        private IEnumerator SimulateArtilleryShell(Vector3 impactPosition)
        {
            if (shellPrefab != null)
            {
                GameObject shell = Instantiate(shellPrefab, transform.position, Quaternion.identity);
                Vector3 startPos = transform.position;
                float elapsed = 0f;

                while (elapsed < shellTravelTime)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / shellTravelTime;

                    // Calculate parabolic trajectory
                    Vector3 currentPos = Vector3.Lerp(startPos, impactPosition, progress);
                    float height = Mathf.Sin(progress * Mathf.PI) * trajectoryHeight;
                    currentPos.y += height;

                    shell.transform.position = currentPos;
                    shell.transform.LookAt(impactPosition);

                    yield return null;
                }

                // Shell impact
                Destroy(shell);
                CreateExplosion(impactPosition);
            }
        }

        private void CreateExplosion(Vector3 position)
        {
            // Spawn explosion effect
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, position, Quaternion.identity);
            }

            // Apply damage to units in splash radius
            Collider[] hits = Physics.OverlapSphere(position, splashRadius);
            foreach (var hit in hits)
            {
                Unit unit = hit.GetComponent<Unit>();
                if (unit != null && unit.factionType != factionType)
                {
                    float distance = Vector3.Distance(position, hit.transform.position);
                    float damageMultiplier = 1 - (distance / splashRadius);
                    float damage = attackDamage * damageMultiplier;
                    unit.TakeDamage(damage);
                }

                Building building = hit.GetComponent<Building>();
                if (building != null && building.factionType != factionType)
                {
                    float distance = Vector3.Distance(position, hit.transform.position);
                    float damageMultiplier = 1 - (distance / splashRadius);
                    float damage = attackDamage * damageMultiplier * 1.5f; // Extra damage to buildings
                    building.TakeDamage(damage);
                }
            }
        }

        public void ToggleDeployment()
        {
            if (!isDeploying)
            {
                StartCoroutine(DeploymentSequence());
            }
        }

        private IEnumerator DeploymentSequence()
        {
            isDeploying = true;
            float deploymentTime = isDeployed ? undeployTime : deployTime;
            
            // Play deployment effect
            if (deployEffect != null)
            {
                deployEffect.Play();
            }

            // Wait for deployment animation
            yield return new WaitForSeconds(deploymentTime);

            // Toggle deployed state
            isDeployed = !isDeployed;

            // Update visual models
            if (deployedModel) deployedModel.SetActive(isDeployed);
            if (undeployedModel) undeployedModel.SetActive(!isDeployed);

            // Apply deployed state bonuses
            if (isDeployed)
            {
                attackRange = maxRange + deployedRangeBonus;
                accuracyRadius = baseAccuracyRadius * deployedAccuracyBonus;
                attackDamage = baseDamage * deployedDamageBonus;
                attackSpeed = baseAttackSpeed * deployedFireRateBonus;
                agent.enabled = false;
            }
            else
            {
                attackRange = maxRange;
                accuracyRadius = baseAccuracyRadius;
                attackDamage = baseDamage;
                attackSpeed = baseAttackSpeed;
                agent.enabled = true;
            }

            isDeploying = false;
        }

        public void ShowTrajectory(Vector3 targetPos)
        {
            if (!trajectoryLine || !isDeployed) return;

            trajectoryLine.enabled = true;
            Vector3[] points = CalculateTrajectoryPoints(targetPos);
            trajectoryLine.positionCount = points.Length;
            trajectoryLine.SetPositions(points);
        }

        public void HideTrajectory()
        {
            if (trajectoryLine)
            {
                trajectoryLine.enabled = false;
            }
        }

        private Vector3[] CalculateTrajectoryPoints(Vector3 targetPos)
        {
            int pointCount = 20;
            Vector3[] points = new Vector3[pointCount];
            Vector3 startPos = transform.position;

            for (int i = 0; i < pointCount; i++)
            {
                float progress = i / (float)(pointCount - 1);
                Vector3 pos = Vector3.Lerp(startPos, targetPos, progress);
                float height = Mathf.Sin(progress * Mathf.PI) * trajectoryHeight;
                pos.y += height;
                points[i] = pos;
            }

            return points;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw min range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minRange);

            // Draw max range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw accuracy radius if deployed
            if (isDeployed && targetPosition.HasValue)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(targetPosition.Value, accuracyRadius);
            }

            // Draw ammo status
            if (Application.isPlaying)
            {
                Vector3 position = transform.position + Vector3.up * 3f;
                UnityEditor.Handles.Label(position, $"Ammo: {currentAmmo:F0}/{maxAmmo}");
            }
        }
    }
}
