using UnityEngine;
using System.Collections;
using RTS.Effects;
using RTS.Units.Combat;
using RTS.Core;

namespace RTS.Units
{
    public class Artillery : HeavyUnit
    {
        [Header("Artillery Settings")]
        [SerializeField] private float minRange = 10f;
        [SerializeField] private float maxRange = 30f;
        [SerializeField] private float shellSpeed = 20f;
        [SerializeField] private float splashRadius = 5f;
        [SerializeField] private float reloadTime = 3f;
        [SerializeField] private int maxAmmo = 10;
        [SerializeField] private float ammoReplenishTime = 5f;

        [Header("Shell Effects")]
        [SerializeField] private GameObject shellPrefab;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip reloadSound;

        private ObjectPool<ArtilleryShell> shellPool;
        private ObjectPool<ParticleSystem> explosionPool;
        private int currentAmmo;
        private float nextFireTime;
        private float nextAmmoReplenishTime;
        private AudioSource audioSource;
        private bool isReloading;
        private new Vector3 targetPosition;  // Explicitly hiding base member
        private new float baseDamage;  // Explicitly hiding base member
        private new DamageHandler damageHandler;  // Explicitly hiding base member

        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = maxRange * 2f;
            
            currentAmmo = maxAmmo;
            baseDamage = attackDamage;
            
            // Initialize object pools
            if (shellPrefab != null)
            {
                var shellComponent = shellPrefab.GetComponent<ArtilleryShell>();
                if (shellComponent != null)
                {
                    shellPool = new ObjectPool<ArtilleryShell>(shellComponent, transform, 5);
                }
            }
            
            if (explosionPrefab != null)
            {
                var explosionComponent = explosionPrefab.GetComponent<ParticleSystem>();
                if (explosionComponent != null)
                {
                    explosionPool = new ObjectPool<ParticleSystem>(explosionComponent, transform, 5);
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            
            // Handle ammo replenishment
            if (currentAmmo < maxAmmo && Time.time >= nextAmmoReplenishTime)
            {
                ReplenishAmmo();
            }
        }

        private void ReplenishAmmo()
        {
            currentAmmo++;
            nextAmmoReplenishTime = Time.time + ammoReplenishTime;
            
            if (currentAmmo == maxAmmo)
            {
                isReloading = false;
            }
        }

        public override void Attack(Unit target)
        {
            if (target == null || currentAmmo <= 0 || Time.time < nextFireTime) return;

            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            if (distanceToTarget < minRange || distanceToTarget > maxRange) return;

            // Fire artillery shell
            FireShell(target.transform.position);
            
            currentAmmo--;
            nextFireTime = Time.time + reloadTime;
            
            if (currentAmmo <= 0)
            {
                isReloading = true;
                nextAmmoReplenishTime = Time.time + ammoReplenishTime;
            }
        }

        private IEnumerator FireShellRoutine(Vector3 targetPos)
        {
            if (shellPool == null) yield break;

            // Calculate firing arc
            Vector3 targetDir = targetPos - transform.position;
            float distance = targetDir.magnitude;
            float tof = distance / shellSpeed; // Time of flight
            
            // Account for gravity in the arc
            float height = distance * 0.5f;
            Vector3 midPoint = transform.position + targetDir * 0.5f + Vector3.up * height;
            
            // Spawn shell
            ArtilleryShell shell = shellPool.Get(transform.position);
            shell.Initialize(transform.position, midPoint, targetPos, tof, splashRadius, attackDamage);
            shell.OnExplode += HandleShellExplosion;

            // Play effects
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            if (audioSource != null && fireSound != null)
            {
                audioSource.PlayOneShot(fireSound);
            }

            yield return new WaitForSeconds(tof);
        }

        private IEnumerator ReloadRoutine()
        {
            if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }

            yield return new WaitForSeconds(reloadTime);
            
            currentAmmo = maxAmmo;
            isReloading = false;
        }

        private void HandleShellExplosion(Vector3 position)
        {
            // Spawn explosion effect
            if (explosionPool != null)
            {
                ParticleSystem explosion = explosionPool.Get(position);
                explosion.Play();
            }
            
            // Apply splash damage
            Collider[] colliders = Physics.OverlapSphere(position, splashRadius);
            foreach (var collider in colliders)
            {
                Unit unit = collider.GetComponent<Unit>();
                if (unit != null && unit.GetFaction() != GetFaction())
                {
                    float distance = Vector3.Distance(position, unit.transform.position);
                    float damageMultiplier = 1 - (distance / splashRadius);
                    float damage = attackDamage * damageMultiplier;
                    unit.TakeDamage(damage);
                }
            }
        }

        public bool IsReloading()
        {
            return isReloading;
        }

        public float GetAmmoPercentage()
        {
            return (float)currentAmmo / maxAmmo;
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!Application.isPlaying) return;

            if (isSelected)
            {
                // Min range
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, minRange);

                // Max range
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, maxRange);

                // Draw splash radius preview at mouse position if selected
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                {
                    float distanceToMouse = Vector3.Distance(transform.position, hit.point);
                    if (distanceToMouse >= minRange && distanceToMouse <= maxRange)
                    {
                        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                        Gizmos.DrawWireSphere(hit.point, splashRadius);
                    }
                }
            }
        }
    }
}
