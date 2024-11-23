using UnityEngine;
using System.Collections.Generic;
using RTS.Core;

namespace RTS.Factions
{
    public class TechnologicalFaction : Faction
    {
        [Header("Tech Faction Specifics")]
        public float energyEfficiency = 1.5f;  // Bonus to power generation
        public float automationBonus = 0.25f;  // Reduction in unit production time
        public float techResearchRate = 1.0f;  // Rate at which new technologies are researched

        [Header("Special Abilities")]
        public float droneSatelliteCooldown = 180f;  // Cooldown for satellite scan ability
        public float empBlastCooldown = 120f;        // Cooldown for EMP blast
        public float shieldRegenerationRate = 5f;    // Shield regeneration per second

        private List<DroneController> activeDrones = new List<DroneController>();
        private bool satelliteScanAvailable = true;
        private bool empBlastAvailable = true;
        private float currentShieldStrength;
        private float maxShieldStrength = 200f;

        public override void Initialize()
        {
            base.Initialize();
            factionName = "Technological Alliance";
            type = FactionType.TechnologicalFaction;
            currentShieldStrength = maxShieldStrength;

            // Apply faction-specific bonuses
            power *= energyEfficiency;
        }

        public override void UpdateEconomy()
        {
            base.UpdateEconomy();
            
            // Automated resource collection through drones
            foreach (DroneController drone in activeDrones)
            {
                if (drone.IsGathering)
                {
                    gold += drone.GatheringRate * Time.deltaTime;
                }
            }

            // Shield regeneration
            if (currentShieldStrength < maxShieldStrength)
            {
                currentShieldStrength += shieldRegenerationRate * Time.deltaTime;
                currentShieldStrength = Mathf.Min(currentShieldStrength, maxShieldStrength);
            }
        }

        public override bool CanBuildUnit(UnitType unitType)
        {
            if (!base.CanBuildUnit(unitType)) return false;

            // Check tech-specific requirements
            switch (unitType)
            {
                case UnitType.Drone:
                    return HasBuilding(BuildingType.DroneFactory);
                case UnitType.LaserTank:
                    return HasBuilding(BuildingType.ResearchLab) && HasBuilding(BuildingType.WarFactory);
                case UnitType.StealthJet:
                    return HasBuilding(BuildingType.ResearchLab) && HasPowerLevel(100f);
                default:
                    return false;
            }
        }

        public override bool CanConstructBuilding(BuildingType buildingType)
        {
            if (!base.CanConstructBuilding(buildingType)) return false;

            // Check tech-specific requirements
            switch (buildingType)
            {
                case BuildingType.ResearchLab:
                    return HasBuilding(BuildingType.PowerPlant);
                case BuildingType.LaserDefenseTower:
                    return HasBuilding(BuildingType.ResearchLab) && HasPowerLevel(150f);
                default:
                    return true;
            }
        }

        // Special Abilities
        public bool ActivateSatelliteScan(Vector3 targetPosition)
        {
            if (!satelliteScanAvailable) return false;

            // Implement satellite scan logic here
            // Reveals an area of the map and detects stealth units
            satelliteScanAvailable = false;
            StartCoroutine(ResetSatelliteScanCooldown());
            return true;
        }

        public bool ActivateEMPBlast(Vector3 targetPosition, float radius)
        {
            if (!empBlastAvailable) return false;

            // Implement EMP blast logic here
            // Temporarily disables enemy units and buildings
            empBlastAvailable = false;
            StartCoroutine(ResetEMPBlastCooldown());
            return true;
        }

        // Helper methods
        private bool HasBuilding(BuildingType type)
        {
            return buildings.Exists(b => b.type == type && b.isConstructed);
        }

        private bool HasPowerLevel(float requiredPower)
        {
            return power >= requiredPower;
        }

        private System.Collections.IEnumerator ResetSatelliteScanCooldown()
        {
            yield return new WaitForSeconds(droneSatelliteCooldown);
            satelliteScanAvailable = true;
        }

        private System.Collections.IEnumerator ResetEMPBlastCooldown()
        {
            yield return new WaitForSeconds(empBlastCooldown);
            empBlastAvailable = true;
        }

        // Shield system
        public override void TakeDamage(float damage)
        {
            // Absorb damage with shields first
            if (currentShieldStrength > 0)
            {
                float shieldDamage = Mathf.Min(currentShieldStrength, damage);
                currentShieldStrength -= shieldDamage;
                damage -= shieldDamage;
            }

            // Apply remaining damage to base health
            if (damage > 0)
            {
                base.TakeDamage(damage);
            }
        }

        // Drone management
        public void RegisterDrone(DroneController drone)
        {
            if (drone != null && !activeDrones.Contains(drone))
            {
                activeDrones.Add(drone);
            }
        }

        public void UnregisterDrone(DroneController drone)
        {
            if (drone != null)
            {
                activeDrones.Remove(drone);
            }
        }
    }
}
