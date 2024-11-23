using UnityEngine;
using System.Collections.Generic;

namespace RTS.Core
{
    public abstract class Building : MonoBehaviour
    {
        [Header("Building Properties")]
        public string buildingName;
        public BuildingType type;
        public FactionType factionType;

        [Header("Stats")]
        public float maxHealth = 500f;
        public float currentHealth;
        public float armor = 20f;

        [Header("Cost")]
        public float goldCost;
        public float powerCost;
        public float constructionTime;

        [Header("Production")]
        public bool canProduceUnits;
        public List<UnitType> producibleUnits;
        public float productionSpeedMultiplier = 1f;

        protected bool isConstructed;
        protected float constructionProgress;
        protected Queue<UnitType> productionQueue;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            productionQueue = new Queue<UnitType>();
        }

        public virtual void Initialize(FactionType faction)
        {
            factionType = faction;
            isConstructed = false;
            constructionProgress = 0f;
        }

        public virtual void UpdateConstruction(float deltaTime)
        {
            if (!isConstructed)
            {
                constructionProgress += deltaTime;
                if (constructionProgress >= constructionTime)
                {
                    CompleteConstruction();
                }
            }
        }

        protected virtual void CompleteConstruction()
        {
            isConstructed = true;
            // Enable building functionality
        }

        public virtual bool CanProduceUnit(UnitType unitType)
        {
            return isConstructed && canProduceUnits && producibleUnits.Contains(unitType);
        }

        public virtual void QueueUnit(UnitType unitType)
        {
            if (CanProduceUnit(unitType))
            {
                productionQueue.Enqueue(unitType);
            }
        }

        public virtual void CancelProduction(UnitType unitType)
        {
            // Implementation for canceling unit production
        }

        public virtual void TakeDamage(float damage)
        {
            float actualDamage = Mathf.Max(0, damage - armor);
            currentHealth -= actualDamage;

            if (currentHealth <= 0)
            {
                Destroy();
            }
        }

        protected virtual void Destroy()
        {
            // Handle building destruction (e.g., play animation, spawn debris)
            Destroy(gameObject);
        }

        public virtual void Repair(float amount)
        {
            if (currentHealth < maxHealth)
            {
                currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            // Draw building footprint in editor
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider>().bounds.size);
        }
    }

    public enum BuildingType
    {
        // Common Buildings
        CommandCenter,
        PowerPlant,
        Barracks,

        // Tech Faction Buildings
        ResearchLab,
        DroneFactory,
        LaserDefenseTower,

        // Heavy Assault Buildings
        WarFactory,
        Artillery_Platform,
        HeavyDefenseBunker,

        // Guerrilla Buildings
        UndergroundBase,
        TunnelNetwork,
        TrapFactory
    }
}
