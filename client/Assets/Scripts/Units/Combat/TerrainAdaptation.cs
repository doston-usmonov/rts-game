using UnityEngine;

namespace RTS.Units.Combat
{
    [System.Serializable]
    public class TerrainAdaptation
    {
        [Header("Terrain Movement")]
        public float mudSlowdown = 0.7f;
        public float snowSlowdown = 0.8f;
        public float sandSlowdown = 0.75f;
        public float waterSlowdown = 0.5f;
        
        [Header("Terrain Bonuses")]
        public float heightAdvantage = 1.2f;
        public float coverBonus = 1.15f;
        public float fortifiedBonus = 1.3f;
        
        [Header("Adaptation Speed")]
        public float adaptationRate = 0.5f;
        public float maxAdaptationBonus = 1.5f;
        
        private float currentAdaptation = 1f;
        private TerrainType lastTerrainType;
        
        public float GetMovementMultiplier(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.Mud:
                    return mudSlowdown;
                case TerrainType.Snow:
                    return snowSlowdown;
                case TerrainType.Sand:
                    return sandSlowdown;
                case TerrainType.Water:
                    return waterSlowdown;
                default:
                    return 1f;
            }
        }
        
        public float GetCombatMultiplier(bool hasHeightAdvantage, bool inCover, bool isFortified)
        {
            float multiplier = 1f;
            
            if (hasHeightAdvantage)
                multiplier *= heightAdvantage;
            
            if (inCover)
                multiplier *= coverBonus;
            
            if (isFortified)
                multiplier *= fortifiedBonus;
                
            return multiplier;
        }
        
        public void UpdateAdaptation(TerrainType currentTerrain, float deltaTime)
        {
            if (currentTerrain == lastTerrainType)
            {
                currentAdaptation = Mathf.Min(currentAdaptation + adaptationRate * deltaTime, maxAdaptationBonus);
            }
            else
            {
                currentAdaptation = 1f;
                lastTerrainType = currentTerrain;
            }
        }
        
        public float GetAdaptationBonus()
        {
            return currentAdaptation;
        }
    }
    
    public enum TerrainType
    {
        Normal,
        Mud,
        Snow,
        Sand,
        Water,
        Forest,
        Urban
    }
}
