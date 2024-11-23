using UnityEngine;
using System.Collections.Generic;

namespace RTS.Core
{
    public abstract class Faction : MonoBehaviour
    {
        [Header("Faction Properties")]
        public string factionName;
        public string description;
        public FactionType type;

        [Header("Resources")]
        public float gold = 1000f;
        public float power = 100f;

        [Header("Base Stats")]
        public float baseHealth = 1000f;
        public float baseArmor = 100f;

        protected List<Unit> units = new List<Unit>();
        protected List<Building> buildings = new List<Building>();

        public virtual void Initialize()
        {
            // Set up initial faction state
        }

        public virtual bool CanBuildUnit(UnitType unitType)
        {
            // Check resources and requirements
            return HasRequiredResources(unitType) && HasRequiredBuildings(unitType);
        }

        public virtual bool CanConstructBuilding(BuildingType buildingType)
        {
            // Check resources and requirements
            return HasRequiredResources(buildingType) && HasValidBuildLocation();
        }

        protected virtual bool HasRequiredResources(UnitType unitType)
        {
            // Implementation will vary by faction
            return true;
        }

        protected virtual bool HasRequiredResources(BuildingType buildingType)
        {
            // Implementation will vary by faction
            return true;
        }

        protected virtual bool HasRequiredBuildings(UnitType unitType)
        {
            // Check if necessary production buildings exist
            return true;
        }

        protected virtual bool HasValidBuildLocation()
        {
            // Check if there's a valid location to build
            return true;
        }

        public virtual void CollectResources()
        {
            // Base resource collection logic
        }

        public virtual void UpdateEconomy()
        {
            // Update resource generation and consumption
        }

        public virtual void AddUnit(Unit unit)
        {
            if (unit != null)
            {
                units.Add(unit);
            }
        }

        public virtual void RemoveUnit(Unit unit)
        {
            if (unit != null)
            {
                units.Remove(unit);
            }
        }

        public virtual void AddBuilding(Building building)
        {
            if (building != null)
            {
                buildings.Add(building);
            }
        }

        public virtual void RemoveBuilding(Building building)
        {
            if (building != null)
            {
                buildings.Remove(building);
            }
        }
    }

    public enum FactionType
    {
        TechnologicalFaction,
        HeavyAssaultFaction,
        GuerrillaFaction
    }
}
