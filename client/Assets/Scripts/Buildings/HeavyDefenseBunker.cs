using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RTS.Core;
using RTS.Units;

namespace RTS.Buildings
{
    public class HeavyDefenseBunker : Building
    {
        [Header("Bunker Stats")]
        public int maxGarrisonCapacity = 4;
        public float baseArmor = 50f;
        public float baseHealth = 1000f;
        public float repairRadius = 15f;
        public float repairRate = 10f; // HP per second
        public float repairTickInterval = 1f;

        [Header("Weapon Systems")]
        public Transform antiVehicleTurret;
        public Transform antiAirTurret;
        public float turretRotationSpeed = 120f;
        public float groundAttackRange = 30f;
        public float airAttackRange = 40f;
        public float groundDamage = 40f;
        public float airDamage = 25f;
        public float attackSpeed = 1f;

        [Header("Fortify Mode")]
        public float fortifyArmorBonus = 25f;
        public float fortifyDamageBonus = 1.5f;
        public float fortifyDuration = 20f;
        public float fortifyCooldown = 45f;

        [Header("Visual Effects")]
        public ParticleSystem fortifyEffect;
        public ParticleSystem repairEffect;
        public GameObject shieldVFX;
        public GameObject[] weaponUpgradeModels;

        // Garrison management
        private List<Unit> garrisonedUnits = new List<Unit>();
        private Dictionary<Unit, Vector3> exitPositions = new Dictionary<Unit, Vector3>();

        // Weapon system state
        private bool isAntiVehicleTurretActive = true;
        private bool isAntiAirTurretActive = true;
        private Unit currentGroundTarget;
        private Unit currentAirTarget;
        private float lastGroundAttackTime;
        private float lastAirAttackTime;

        // Fortify mode state
        private bool isFortified;
        private float fortifyEndTime;
        private float fortifyCooldownEndTime;

        // Upgrade tracking
        private WeaponUpgradeType currentWeaponUpgrade = WeaponUpgradeType.Standard;
        private bool isUpgrading;
        private float upgradeProgress;

        protected override void Awake()
        {
            base.Awake();
            maxHealth = baseHealth;
            armor = baseArmor;
            
            // Initialize weapon systems
            if (antiVehicleTurret) antiVehicleTurret.gameObject.SetActive(true);
            if (antiAirTurret) antiAirTurret.gameObject.SetActive(true);

            // Start repair pulse
            InvokeRepeating(nameof(RepairPulse), 0f, repairTickInterval);
        }

        private void Update()
        {
            UpdateWeaponSystems();
            UpdateFortifyStatus();
            UpdateUpgradeProgress();
        }

        #region Garrison Management

        public bool GarrisonUnit(Unit unit)
        {
            if (garrisonedUnits.Count >= maxGarrisonCapacity || 
                !CanBeGarrisoned(unit) || 
                garrisonedUnits.Contains(unit))
                return false;

            // Store exit position before disabling the unit
            exitPositions[unit] = FindGarrisonExitPosition();
            
            // Disable unit's GameObject but keep the reference
            unit.gameObject.SetActive(false);
            garrisonedUnits.Add(unit);
            
            // Apply garrison buffs to the bunker
            UpdateGarrisonBonuses();
            
            return true;
        }

        public bool UngarrisonUnit(Unit unit)
        {
            if (!garrisonedUnits.Contains(unit))
                return false;

            if (exitPositions.TryGetValue(unit, out Vector3 exitPosition))
            {
                // Reactivate unit at exit position
                unit.gameObject.SetActive(true);
                unit.transform.position = exitPosition;
                
                garrisonedUnits.Remove(unit);
                exitPositions.Remove(unit);
                
                // Update buffs after unit leaves
                UpdateGarrisonBonuses();
                return true;
            }
            
            return false;
        }

        private bool CanBeGarrisoned(Unit unit)
        {
            // Check if unit type is allowed to garrison
            return unit is InfantryUnit || 
                   (unit is LightVehicleUnit && garrisonedUnits.Count < maxGarrisonCapacity/2);
        }

        private Vector3 FindGarrisonExitPosition()
        {
            // Find a safe position around the bunker for units to exit
            float radius = 5f;
            float angle = Random.Range(0f, 360f);
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            return transform.position + offset;
        }

        private void UpdateGarrisonBonuses()
        {
            // Calculate bonus based on garrisoned units
            float armorBonus = garrisonedUnits.Count * 5f;
            armor = baseArmor + armorBonus;
        }

        #endregion

        #region Weapon Systems

        private void UpdateWeaponSystems()
        {
            if (!isUpgrading)
            {
                // Handle ground targets
                if (currentGroundTarget != null && isAntiVehicleTurretActive)
                {
                    UpdateTurretAim(antiVehicleTurret, currentGroundTarget.transform.position);
                    TryAttackGroundTarget();
                }

                // Handle air targets
                if (currentAirTarget != null && isAntiAirTurretActive)
                {
                    UpdateTurretAim(antiAirTurret, currentAirTarget.transform.position);
                    TryAttackAirTarget();
                }
            }
        }

        private void UpdateTurretAim(Transform turret, Vector3 targetPosition)
        {
            if (turret == null) return;

            Vector3 targetDirection = targetPosition - turret.position;
            targetDirection.y = 0f;

            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            turret.rotation = Quaternion.RotateTowards(
                turret.rotation,
                targetRotation,
                turretRotationSpeed * Time.deltaTime
            );
        }

        private void TryAttackGroundTarget()
        {
            if (Time.time >= lastGroundAttackTime + attackSpeed)
            {
                float damage = groundDamage * (isFortified ? fortifyDamageBonus : 1f);
                currentGroundTarget.TakeDamage(damage);
                lastGroundAttackTime = Time.time;

                // Trigger attack visual effects here
            }
        }

        private void TryAttackAirTarget()
        {
            if (Time.time >= lastAirAttackTime + attackSpeed)
            {
                float damage = airDamage * (isFortified ? fortifyDamageBonus : 1f);
                currentAirTarget.TakeDamage(damage);
                lastAirAttackTime = Time.time;

                // Trigger attack visual effects here
            }
        }

        public void SetGroundTarget(Unit target)
        {
            if (target == null || Vector3.Distance(transform.position, target.transform.position) > groundAttackRange)
            {
                currentGroundTarget = null;
                return;
            }

            currentGroundTarget = target;
        }

        public void SetAirTarget(Unit target)
        {
            if (target == null || Vector3.Distance(transform.position, target.transform.position) > airAttackRange)
            {
                currentAirTarget = null;
                return;
            }

            currentAirTarget = target;
        }

        #endregion

        #region Fortify Mode

        public bool ActivateFortifyMode()
        {
            if (isFortified || Time.time < fortifyCooldownEndTime)
                return false;

            isFortified = true;
            fortifyEndTime = Time.time + fortifyDuration;
            
            // Apply fortify bonuses
            armor += fortifyArmorBonus;
            
            // Activate visual effects
            if (fortifyEffect != null)
                fortifyEffect.Play();
            if (shieldVFX != null)
                shieldVFX.SetActive(true);

            return true;
        }

        private void UpdateFortifyStatus()
        {
            if (isFortified && Time.time >= fortifyEndTime)
            {
                DeactivateFortifyMode();
            }
        }

        private void DeactivateFortifyMode()
        {
            isFortified = false;
            fortifyCooldownEndTime = Time.time + fortifyCooldown;
            
            // Remove fortify bonuses
            armor -= fortifyArmorBonus;
            
            // Deactivate visual effects
            if (fortifyEffect != null)
                fortifyEffect.Stop();
            if (shieldVFX != null)
                shieldVFX.SetActive(false);
        }

        #endregion

        #region Repair System

        private void RepairPulse()
        {
            // Find nearby damaged allied units
            Collider[] colliders = Physics.OverlapSphere(transform.position, repairRadius);
            foreach (Collider col in colliders)
            {
                Unit unit = col.GetComponent<Unit>();
                if (unit != null && unit.factionType == factionType && unit.Health < unit.MaxHealth)
                {
                    RepairUnit(unit);
                }
            }
        }

        private void RepairUnit(Unit unit)
        {
            float healAmount = repairRate * repairTickInterval;
            unit.Heal(healAmount);

            // Show repair effect
            if (repairEffect != null && !repairEffect.isPlaying)
            {
                repairEffect.Play();
            }
        }

        #endregion

        #region Weapon Upgrades

        public enum WeaponUpgradeType
        {
            Standard,
            LaserCannon,
            MissileLauncher
        }

        public bool StartWeaponUpgrade(WeaponUpgradeType upgradeType)
        {
            if (isUpgrading || upgradeType == currentWeaponUpgrade)
                return false;

            isUpgrading = true;
            upgradeProgress = 0f;
            
            // Disable weapons during upgrade
            isAntiVehicleTurretActive = false;
            isAntiAirTurretActive = false;

            return true;
        }

        private void UpdateUpgradeProgress()
        {
            if (!isUpgrading) return;

            upgradeProgress += Time.deltaTime / 10f; // 10 seconds to upgrade
            
            if (upgradeProgress >= 1f)
            {
                CompleteUpgrade();
            }
        }

        private void CompleteUpgrade()
        {
            isUpgrading = false;
            isAntiVehicleTurretActive = true;
            isAntiAirTurretActive = true;

            // Update weapon models and stats based on upgrade type
            UpdateWeaponModels();
            UpdateWeaponStats();
        }

        private void UpdateWeaponModels()
        {
            // Disable all upgrade models
            foreach (var model in weaponUpgradeModels)
            {
                if (model != null) model.SetActive(false);
            }

            // Enable the current upgrade model
            int upgradeIndex = (int)currentWeaponUpgrade - 1;
            if (upgradeIndex >= 0 && upgradeIndex < weaponUpgradeModels.Length)
            {
                weaponUpgradeModels[upgradeIndex].SetActive(true);
            }
        }

        private void UpdateWeaponStats()
        {
            switch (currentWeaponUpgrade)
            {
                case WeaponUpgradeType.LaserCannon:
                    groundDamage *= 1.5f;
                    attackSpeed *= 0.8f;
                    break;
                case WeaponUpgradeType.MissileLauncher:
                    airDamage *= 2f;
                    groundAttackRange *= 1.25f;
                    airAttackRange *= 1.25f;
                    break;
            }
        }

        #endregion

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw repair radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, repairRadius);

            // Draw attack ranges
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, groundAttackRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, airAttackRange);
        }
    }
}
