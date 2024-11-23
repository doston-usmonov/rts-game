using UnityEngine;
using RTS.Core;
using RTS.Factions;
using RTS.Units.Combat;
using System.Collections;

namespace RTS.Units
{
    public abstract class HeavyUnit : Unit
    {
        [Header("Heavy Unit Properties")]
        [SerializeField] protected float crushDamage = 25f;
        [SerializeField] protected float splashRadius = 2f;
        [SerializeField] protected float splashDamageMultiplier = 0.5f;

        [Header("Combat Systems")]
        [SerializeField] protected TerrainAdaptation terrainAdaptation;
        [SerializeField] protected TacticalRetreat tacticalRetreat;
        [SerializeField] protected FormationBonus formationBonus;

        protected float baseDamage;
        protected float baseArmor;
        protected bool isEnraged = false;
        protected ParticleSystem rageEffect;
        protected TerrainType currentTerrain = TerrainType.Normal;
        protected Vector3 fallbackPosition;

        protected override void Awake()
        {
            base.Awake();
            InitializeComponents();
        }

        protected virtual void InitializeComponents()
        {
            baseDamage = attackDamage;
            baseArmor = armor;
            
            // Initialize combat systems
            tacticalRetreat.Initialize(this);
            
            // Get effects
            rageEffect = GetComponentInChildren<ParticleSystem>();
            
            // Set initial fallback position
            fallbackPosition = transform.position - transform.forward * 20f;
        }

        public override void Initialize(FactionType faction)
        {
            base.Initialize(faction);
            
            var heavyFaction = GameObject.FindObjectOfType<HeavyAssaultFaction>();
            if (heavyFaction != null)
            {
                heavyFaction.RegisterHeavyUnit(this);
            }
        }

        protected override void Update()
        {
            base.Update();
            
            UpdateTerrainEffects();
            CheckRetreatConditions();
            UpdateFormationEffects();
        }

        protected virtual void UpdateTerrainEffects()
        {
            // Update terrain type based on current position
            currentTerrain = GetCurrentTerrainType();
            
            // Update adaptation
            terrainAdaptation.UpdateAdaptation(currentTerrain, Time.deltaTime);
            
            // Apply movement modifications
            float terrainMultiplier = terrainAdaptation.GetMovementMultiplier(currentTerrain);
            float adaptationBonus = terrainAdaptation.GetAdaptationBonus();
            
            // Update movement speed
            moveSpeed = baseMoveSpeed * terrainMultiplier * adaptationBonus;
        }

        protected virtual void CheckRetreatConditions()
        {
            if (tacticalRetreat.ShouldRetreat(currentHealth, maxHealth))
            {
                StartCoroutine(tacticalRetreat.PerformRetreat(transform.position, fallbackPosition));
            }
        }

        protected virtual void UpdateFormationEffects()
        {
            // Update nearby allies
            var allUnits = GameObject.FindObjectsOfType<HeavyUnit>();
            formationBonus.UpdateNearbyAllies(transform.position, allUnits);
            
            // Apply formation bonuses
            float damageMultiplier = formationBonus.GetDamageMultiplier();
            float armorMultiplier = formationBonus.GetArmorMultiplier();
            float speedMultiplier = formationBonus.GetSpeedMultiplier();
            
            // Update stats
            attackDamage = baseDamage * damageMultiplier;
            armor = baseArmor * armorMultiplier;
            moveSpeed *= speedMultiplier;
        }

        public override void Attack(Unit target)
        {
            if (target == null || tacticalRetreat.IsRetreating()) return;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance <= attackRange && Time.time >= lastAttackTime + attackSpeed)
            {
                // Get combat multipliers
                float terrainMultiplier = terrainAdaptation.GetCombatMultiplier(
                    HasHeightAdvantage(target),
                    IsInCover(),
                    false // Not implemented fortification yet
                );
                
                // Calculate final damage
                float finalDamage = attackDamage * terrainMultiplier;
                
                // Perform main attack
                target.TakeDamage(finalDamage);
                
                // Apply splash damage
                if (splashRadius > 0)
                {
                    ApplySplashDamage(target.transform.position, finalDamage * splashDamageMultiplier);
                }
                
                lastAttackTime = Time.time;
                
                // Trigger attack effects
                OnAttackEffects(target);
            }
        }

        protected virtual bool HasHeightAdvantage(Unit target)
        {
            return transform.position.y > target.transform.position.y + 1f;
        }

        protected virtual bool IsInCover()
        {
            // Implement cover detection logic
            return false;
        }

        protected virtual TerrainType GetCurrentTerrainType()
        {
            // Implement terrain detection logic
            return TerrainType.Normal;
        }

        protected virtual void ApplySplashDamage(Vector3 center, float damage)
        {
            var colliders = Physics.OverlapSphere(center, splashRadius);
            foreach (var col in colliders)
            {
                var unit = col.GetComponent<Unit>();
                if (unit != null && unit != this)
                {
                    float distance = Vector3.Distance(center, col.transform.position);
                    float damageMultiplier = 1f - (distance / splashRadius);
                    unit.TakeDamage(damage * damageMultiplier);
                }
            }
        }

        protected virtual void OnAttackEffects(Unit target)
        {
            // Implement attack effects
        }

        public void SetFormationType(FormationType formation)
        {
            formationBonus.UpdateFormation(formation);
        }

        public void SetFallbackPosition(Vector3 position)
        {
            fallbackPosition = position;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw splash radius
            if (splashRadius > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, splashRadius);
            }
            
            // Draw retreat path if retreating
            if (tacticalRetreat.IsRetreating())
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, tacticalRetreat.GetRetreatDestination());
            }
        }
    }
}
