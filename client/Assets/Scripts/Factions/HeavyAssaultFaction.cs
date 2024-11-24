using UnityEngine;
using System.Collections.Generic;
using RTS.Units;
using RTS.Units.Combat;
using RTS.Core;

namespace RTS.Factions
{
    public class HeavyAssaultFaction : Faction
    {
        [Header("Heavy Assault Specifics")]
        public float armorBonus = 1.5f;         // Bonus to all unit armor
        public float damageBonus = 1.25f;       // Bonus to all unit damage
        public float productionTimeMultiplier = 1.5f;  // Units take longer to produce

        [Header("Rage Mechanics")]
        public float rageModeMultiplier = 2f;   // Damage multiplier during rage
        public float rageDuration = 15f;        // Duration of rage mode in seconds
        public float rageCooldown = 120f;       // Cooldown between rage activations
        public float rageHealthThreshold = 0.5f; // Health percentage to activate auto-rage

        [Header("Artillery Support")]
        public float artilleryDamage = 100f;
        public float artilleryCooldown = 60f;
        public float artilleryRadius = 10f;

        private bool isRageAvailable = true;
        private bool isRageActive = false;
        private float currentRageTime = 0f;
        private float lastArtilleryTime = 0f;
        private List<HeavyUnit> activeHeavyUnits = new List<HeavyUnit>();

        public override void Initialize()
        {
            base.Initialize();
            factionName = "Heavy Assault Division";
            type = (RTS.Core.FactionType)FactionType.HeavyAssaultFaction;

            // Apply faction-specific stat modifications
            baseArmor *= armorBonus;
        }

        public override void UpdateEconomy()
        {
            base.UpdateEconomy();

            // Update rage mode status
            if (isRageActive)
            {
                currentRageTime += Time.deltaTime;
                if (currentRageTime >= rageDuration)
                {
                    DeactivateRageMode();
                }
            }

            // Check for automatic rage activation on low health units
            foreach (var unit in activeHeavyUnits)
            {
                if (unit.GetHealthPercentage() <= rageHealthThreshold)
                {
                    ActivateRageMode();
                    break;
                }
            }
        }

        public bool ActivateRageMode()
        {
            if (!isRageAvailable || isRageActive) return false;

            isRageActive = true;
            currentRageTime = 0f;

            // Apply rage effects to all units
            foreach (var unit in activeHeavyUnits)
            {
                unit.ApplyRageBonus(rageModeMultiplier);
            }

            StartCoroutine(RageCooldownRoutine());
            return true;
        }

        private void DeactivateRageMode()
        {
            if (!isRageActive) return;

            isRageActive = false;
            
            // Remove rage effects from all units
            foreach (var unit in activeHeavyUnits)
            {
                unit.RemoveRageBonus();
            }
        }

        private System.Collections.IEnumerator RageCooldownRoutine()
        {
            isRageAvailable = false;
            yield return new WaitForSeconds(rageCooldown);
            isRageAvailable = true;
        }

        public bool CallArtilleryStrike(Vector3 targetPosition)
        {
            if (Time.time < lastArtilleryTime + artilleryCooldown) return false;

            // Find all units and buildings in the artillery radius
            Collider[] hits = Physics.OverlapSphere(targetPosition, artilleryRadius);
            foreach (var hit in hits)
            {
                // Apply artillery damage to units and buildings
                Unit unit = hit.GetComponent<Unit>();
                if (unit != null && unit.factionType != type)
                {
                    unit.TakeDamage(artilleryDamage);
                    continue;
                }

                Building building = hit.GetComponent<Building>();
                if (building != null && building.factionType != type)
                {
                    building.TakeDamage(artilleryDamage);
                }
            }

            lastArtilleryTime = Time.time;
            SpawnArtilleryEffect(targetPosition);
            return true;
        }

        private void SpawnArtilleryEffect(Vector3 position)
        {
            // Implement visual and sound effects for artillery strike
        }

        public override bool CanBuildUnit(UnitType unitType)
        {
            if (!base.CanBuildUnit(unitType)) return false;

            // Check faction-specific requirements
            switch (unitType)
            {
                case UnitType.HeavyTank:
                    return HasBuilding(BuildingType.WarFactory);
                case UnitType.Artillery:
                    return HasBuilding(BuildingType.Artillery_Platform);
                case UnitType.Helicopter:
                    return HasBuilding(BuildingType.WarFactory) && HasPowerLevel(150f);
                default:
                    return false;
            }
        }

        public override bool CanConstructBuilding(BuildingType buildingType)
        {
            if (!base.CanConstructBuilding(buildingType)) return false;

            // Check faction-specific requirements
            switch (buildingType)
            {
                case BuildingType.Artillery_Platform:
                    return HasBuilding(BuildingType.WarFactory);
                case BuildingType.HeavyDefenseBunker:
                    return HasBuilding(BuildingType.Barracks);
                default:
                    return true;
            }
        }

        // Unit management
        public void RegisterHeavyUnit(HeavyUnit unit)
        {
            if (unit != null && !activeHeavyUnits.Contains(unit))
            {
                activeHeavyUnits.Add(unit);
                if (isRageActive)
                {
                    unit.ApplyRageBonus(rageModeMultiplier);
                }
            }
        }

        public void UnregisterHeavyUnit(HeavyUnit unit)
        {
            if (unit != null)
            {
                activeHeavyUnits.Remove(unit);
            }
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

        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw territory boundaries
            if (territoryPoints.Count > 0)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                for (int i = 0; i < territoryPoints.Count; i++)
                {
                    Vector3 current = territoryPoints[i];
                    Vector3 next = territoryPoints[(i + 1) % territoryPoints.Count];
                    Gizmos.DrawLine(current, next);
                }
            }

            // Draw unit positions
            if (selectedUnits.Count > 0)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                foreach (var unit in selectedUnits)
                {
                    if (unit != null)
                    {
                        Gizmos.DrawWireSphere(unit.transform.position, 1f);
                    }
                }
            }

            // Draw strategic points
            Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
            foreach (var point in strategicPoints)
            {
                Gizmos.DrawWireSphere(point, 2f);
            }
        }
    }
}
