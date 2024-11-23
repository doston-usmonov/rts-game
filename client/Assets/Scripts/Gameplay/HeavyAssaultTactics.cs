using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RTS.Units;
using RTS.Buildings;

namespace RTS.Gameplay
{
    public class HeavyAssaultTactics : MonoBehaviour
    {
        [Header("Artillery Support")]
        public float artilleryShieldRadius = 20f;
        public float ammoResupplyRadius = 15f;
        public float ammoResupplyRate = 10f;     // Ammo points per second
        public float shieldDamageReduction = 0.3f; // 30% damage reduction
        
        [Header("AI Behavior")]
        public float threatDetectionRadius = 35f;
        public float minEnemyStrengthForFortify = 3f; // Number of enemy units to trigger fortify
        public float aiDecisionInterval = 0.5f;
        public LayerMask enemyLayerMask;

        private Dictionary<Artillery, float> artilleryAmmoLevels = new Dictionary<Artillery, float>();
        private Dictionary<HeavyDefenseBunker, List<Artillery>> bunkerArtilleryPairs = new Dictionary<HeavyDefenseBunker, List<Artillery>>();
        private float nextAIUpdateTime;

        private void Start()
        {
            // Initialize tracking for all existing bunkers and artillery
            var bunkers = FindObjectsOfType<HeavyDefenseBunker>();
            var artillery = FindObjectsOfType<Artillery>();

            foreach (var bunker in bunkers)
            {
                bunkerArtilleryPairs[bunker] = new List<Artillery>();
            }

            foreach (var art in artillery)
            {
                artilleryAmmoLevels[art] = 100f; // Start with full ammo
            }

            // Subscribe to unit creation events if you have them
            // GameEvents.OnUnitCreated += HandleNewUnit;
        }

        private void Update()
        {
            if (Time.time >= nextAIUpdateTime)
            {
                UpdateTacticalBehavior();
                nextAIUpdateTime = Time.time + aiDecisionInterval;
            }
        }

        private void UpdateTacticalBehavior()
        {
            foreach (var bunkerPair in bunkerArtilleryPairs)
            {
                var bunker = bunkerPair.Key;
                if (bunker == null) continue;

                // Update artillery assignments
                UpdateArtilleryAssignments(bunker);

                // Check for threats and manage bunker behavior
                ManageBunkerBehavior(bunker);

                // Handle artillery support
                foreach (var artillery in bunkerPair.Value)
                {
                    if (artillery == null) continue;
                    
                    ManageArtillerySupport(bunker, artillery);
                }
            }
        }

        private void UpdateArtilleryAssignments(HeavyDefenseBunker bunker)
        {
            // Find nearby artillery units that aren't assigned to other bunkers
            var nearbyArtillery = Physics.OverlapSphere(bunker.transform.position, artilleryShieldRadius)
                .Select(col => col.GetComponent<Artillery>())
                .Where(art => art != null && 
                            art.factionType == bunker.factionType && 
                            !bunkerArtilleryPairs.Values.Any(list => list != bunkerArtilleryPairs[bunker] && list.Contains(art)))
                .ToList();

            // Update assignments
            bunkerArtilleryPairs[bunker] = nearbyArtillery;
        }

        private void ManageBunkerBehavior(HeavyDefenseBunker bunker)
        {
            // Detect threats
            var threats = Physics.OverlapSphere(bunker.transform.position, threatDetectionRadius, enemyLayerMask);
            float threatLevel = CalculateThreatLevel(threats);

            // Manage fortify mode
            if (threatLevel >= minEnemyStrengthForFortify && !bunker.IsFortified)
            {
                bunker.ActivateFortifyMode();
            }

            // Prioritize targets
            Unit highestThreatUnit = FindHighestThreatUnit(threats);
            if (highestThreatUnit != null)
            {
                if (IsAirUnit(highestThreatUnit))
                {
                    bunker.SetAirTarget(highestThreatUnit);
                }
                else
                {
                    bunker.SetGroundTarget(highestThreatUnit);
                }
            }
        }

        private void ManageArtillerySupport(HeavyDefenseBunker bunker, Artillery artillery)
        {
            // Check if artillery needs ammo
            if (artilleryAmmoLevels.TryGetValue(artillery, out float ammoLevel) && ammoLevel < 100f)
            {
                float distance = Vector3.Distance(bunker.transform.position, artillery.transform.position);
                if (distance <= ammoResupplyRadius)
                {
                    // Resupply ammo
                    artilleryAmmoLevels[artillery] = Mathf.Min(100f, ammoLevel + ammoResupplyRate * Time.deltaTime);
                    
                    // Trigger resupply effects if needed
                    // artillery.TriggerResupplyEffect();
                }
            }

            // Apply bunker shield protection
            if (Vector3.Distance(bunker.transform.position, artillery.transform.position) <= artilleryShieldRadius)
            {
                ApplyBunkerShield(artillery);
            }
        }

        private float CalculateThreatLevel(Collider[] threats)
        {
            float totalThreat = 0f;
            foreach (var threat in threats)
            {
                Unit unit = threat.GetComponent<Unit>();
                if (unit != null)
                {
                    // Calculate threat based on unit type and stats
                    float unitThreat = unit.attackDamage * (unit.Health / unit.MaxHealth);
                    
                    // Multiply threat for certain unit types
                    if (unit is Artillery) unitThreat *= 1.5f;
                    if (IsAirUnit(unit)) unitThreat *= 1.2f;
                    
                    totalThreat += unitThreat;
                }
            }
            return totalThreat;
        }

        private Unit FindHighestThreatUnit(Collider[] threats)
        {
            Unit highestThreatUnit = null;
            float highestThreat = 0f;

            foreach (var threat in threats)
            {
                Unit unit = threat.GetComponent<Unit>();
                if (unit != null)
                {
                    float unitThreat = CalculateIndividualThreat(unit);
                    if (unitThreat > highestThreat)
                    {
                        highestThreat = unitThreat;
                        highestThreatUnit = unit;
                    }
                }
            }

            return highestThreatUnit;
        }

        private float CalculateIndividualThreat(Unit unit)
        {
            float threat = unit.attackDamage * (unit.Health / unit.MaxHealth);
            
            // Prioritize certain unit types
            if (unit is Artillery) threat *= 1.5f;
            if (IsAirUnit(unit)) threat *= 1.2f;
            
            // Consider distance as a factor
            float distance = Vector3.Distance(transform.position, unit.transform.position);
            threat *= 1f / Mathf.Max(1f, distance * 0.1f);
            
            return threat;
        }

        private void ApplyBunkerShield(Artillery artillery)
        {
            // Modify artillery's damage reduction
            if (artillery.TryGetComponent<DamageHandler>(out var damageHandler))
            {
                damageHandler.AddDamageReduction("BunkerShield", shieldDamageReduction);
            }
        }

        private bool IsAirUnit(Unit unit)
        {
            // Implement based on your unit type system
            return unit.GetComponent<AirUnit>() != null;
        }

        public void RegisterNewBunker(HeavyDefenseBunker bunker)
        {
            if (!bunkerArtilleryPairs.ContainsKey(bunker))
            {
                bunkerArtilleryPairs[bunker] = new List<Artillery>();
            }
        }

        public void RegisterNewArtillery(Artillery artillery)
        {
            if (!artilleryAmmoLevels.ContainsKey(artillery))
            {
                artilleryAmmoLevels[artillery] = 100f;
            }
        }

        public void UnregisterBunker(HeavyDefenseBunker bunker)
        {
            bunkerArtilleryPairs.Remove(bunker);
        }

        public void UnregisterArtillery(Artillery artillery)
        {
            artilleryAmmoLevels.Remove(artillery);
            foreach (var pair in bunkerArtilleryPairs)
            {
                pair.Value.Remove(artillery);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            foreach (var bunkerPair in bunkerArtilleryPairs)
            {
                var bunker = bunkerPair.Key;
                if (bunker == null) continue;

                // Draw threat detection radius
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(bunker.transform.position, threatDetectionRadius);

                // Draw artillery shield radius
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(bunker.transform.position, artilleryShieldRadius);

                // Draw ammo resupply radius
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(bunker.transform.position, ammoResupplyRadius);

                // Draw lines to assigned artillery units
                Gizmos.color = Color.white;
                foreach (var artillery in bunkerPair.Value)
                {
                    if (artillery != null)
                    {
                        Gizmos.DrawLine(bunker.transform.position, artillery.transform.position);
                    }
                }
            }
        }
    }
}
