using UnityEngine;
using System.Collections.Generic;

namespace RTS.Units.Combat
{
    [System.Serializable]
    public class FormationBonus
    {
        [Header("Formation Bonuses")]
        public float lineFormationDamageBonus = 1.2f;
        public float columnFormationArmorBonus = 1.15f;
        public float wedgeFormationSpeedBonus = 1.25f;
        public float boxFormationDefenseBonus = 1.3f;
        
        [Header("Proximity Bonuses")]
        public float proximityRadius = 5f;
        public float maxProximityBonus = 1.5f;
        public float bonusPerAlly = 0.1f;
        
        [Header("Morale Effects")]
        public float moraleRadius = 10f;
        public float moraleBonusMultiplier = 1.2f;
        public float moralePenaltyMultiplier = 0.8f;
        
        private FormationType currentFormation;
        private List<HeavyUnit> nearbyAllies = new List<HeavyUnit>();
        
        public void UpdateFormation(FormationType formation)
        {
            currentFormation = formation;
        }
        
        public void UpdateNearbyAllies(Vector3 position, HeavyUnit[] allUnits)
        {
            nearbyAllies.Clear();
            
            foreach (var unit in allUnits)
            {
                if (unit != null && Vector3.Distance(position, unit.transform.position) <= proximityRadius)
                {
                    nearbyAllies.Add(unit);
                }
            }
        }
        
        public float GetDamageMultiplier()
        {
            float multiplier = 1f;
            
            // Formation specific bonus
            switch (currentFormation)
            {
                case FormationType.Line:
                    multiplier *= lineFormationDamageBonus;
                    break;
                case FormationType.Wedge:
                    // Wedge gets partial damage bonus
                    multiplier *= 1f + (lineFormationDamageBonus - 1f) * 0.5f;
                    break;
            }
            
            // Proximity bonus
            float proximityBonus = Mathf.Min(nearbyAllies.Count * bonusPerAlly, maxProximityBonus - 1f);
            multiplier *= (1f + proximityBonus);
            
            return multiplier;
        }
        
        public float GetArmorMultiplier()
        {
            float multiplier = 1f;
            
            // Formation specific bonus
            switch (currentFormation)
            {
                case FormationType.Column:
                    multiplier *= columnFormationArmorBonus;
                    break;
                case FormationType.Box:
                    multiplier *= boxFormationDefenseBonus;
                    break;
            }
            
            return multiplier;
        }
        
        public float GetSpeedMultiplier()
        {
            float multiplier = 1f;
            
            // Formation specific bonus
            switch (currentFormation)
            {
                case FormationType.Wedge:
                    multiplier *= wedgeFormationSpeedBonus;
                    break;
                case FormationType.Box:
                    // Box formation is slower
                    multiplier *= 0.9f;
                    break;
            }
            
            return multiplier;
        }
        
        public float GetMoraleMultiplier(float averageAlliedHealth, float averageEnemyHealth)
        {
            float moraleMultiplier = 1f;
            
            // High allied health boosts morale
            if (averageAlliedHealth > 0.7f)
            {
                moraleMultiplier *= moraleBonusMultiplier;
            }
            // Low allied health reduces morale
            else if (averageAlliedHealth < 0.3f)
            {
                moraleMultiplier *= moralePenaltyMultiplier;
            }
            
            // Winning/losing affects morale
            if (averageAlliedHealth > averageEnemyHealth * 1.5f)
            {
                moraleMultiplier *= moraleBonusMultiplier;
            }
            else if (averageAlliedHealth * 1.5f < averageEnemyHealth)
            {
                moraleMultiplier *= moralePenaltyMultiplier;
            }
            
            return moraleMultiplier;
        }
    }
}
