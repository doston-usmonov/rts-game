using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTS.AI
{
    public class AISquadManager : MonoBehaviour
    {
        [Header("Squad Settings")]
        [SerializeField] private int maxSquadSize = 8;
        [SerializeField] private float squadCohesionRadius = 15f;
        [SerializeField] private float squadUpdateInterval = 0.5f;

        [Header("Tactical Settings")]
        [SerializeField] private float assaultRange = 25f;
        [SerializeField] private float flankerSpreadDistance = 12f;
        [SerializeField] private float supportDistance = 8f;

        private Dictionary<int, AISquad> squads = new Dictionary<int, AISquad>();
        private AITacticalAnalyzer tacticalAnalyzer;
        private float lastUpdateTime;

        public class AISquad
        {
            public int squadId;
            public SquadRole role;
            public List<AIUnit> units = new List<AIUnit>();
            public Vector3 squadCenter;
            public Transform primaryTarget;
            public AISquad supportTarget;
            public TacticalState state;
            public float cohesionStrength = 1f;
            public float lastActionTime;

            public enum SquadRole
            {
                Assault,    // Main attack force
                Flanker,    // Maneuver around enemy
                Support,    // Provide backup and healing
                Scout,      // Reconnaissance
                Artillery   // Long-range support
            }

            public enum TacticalState
            {
                Advancing,
                Attacking,
                Flanking,
                Supporting,
                Retreating,
                Scouting
            }
        }

        private void Awake()
        {
            tacticalAnalyzer = GetComponent<AITacticalAnalyzer>();
        }

        private void Update()
        {
            if (Time.time - lastUpdateTime >= squadUpdateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateSquads();
            }
        }

        public int CreateSquad(AISquad.SquadRole role)
        {
            int squadId = GenerateSquadId();
            var squad = new AISquad
            {
                squadId = squadId,
                role = role,
                state = AISquad.TacticalState.Advancing
            };

            squads.Add(squadId, squad);
            return squadId;
        }

        public void AssignUnitToSquad(AIUnit unit, int squadId)
        {
            if (squads.TryGetValue(squadId, out var squad))
            {
                if (squad.units.Count < maxSquadSize)
                {
                    squad.units.Add(unit);
                    ConfigureUnitForSquadRole(unit, squad.role);
                }
            }
        }

        private void ConfigureUnitForSquadRole(AIUnit unit, AISquad.SquadRole role)
        {
            switch (role)
            {
                case AISquad.SquadRole.Assault:
                    unit.SetAggressive(true);
                    unit.EnableFlanking(false);
                    break;

                case AISquad.SquadRole.Flanker:
                    unit.SetAggressive(true);
                    unit.EnableFlanking(true);
                    break;

                case AISquad.SquadRole.Support:
                    unit.SetAggressive(false);
                    unit.ProvideSupportFire(true);
                    break;

                case AISquad.SquadRole.Scout:
                    unit.SetAggressive(false);
                    unit.EnableFlanking(true);
                    break;

                case AISquad.SquadRole.Artillery:
                    unit.SetAggressive(true);
                    unit.ProvideSupportFire(true);
                    break;
            }
        }

        private void UpdateSquads()
        {
            foreach (var squad in squads.Values)
            {
                UpdateSquadCenter(squad);
                UpdateSquadTactics(squad);
                EnforceSquadCohesion(squad);
            }

            CoordinateSquadActions();
        }

        private void UpdateSquadCenter(AISquad squad)
        {
            if (squad.units.Count == 0) return;

            Vector3 center = Vector3.zero;
            int activeUnits = 0;

            foreach (var unit in squad.units.Where(u => u != null))
            {
                center += unit.transform.position;
                activeUnits++;
            }

            if (activeUnits > 0)
            {
                squad.squadCenter = center / activeUnits;
            }
        }

        private void UpdateSquadTactics(AISquad squad)
        {
            switch (squad.role)
            {
                case AISquad.SquadRole.Assault:
                    UpdateAssaultTactics(squad);
                    break;

                case AISquad.SquadRole.Flanker:
                    UpdateFlankerTactics(squad);
                    break;

                case AISquad.SquadRole.Support:
                    UpdateSupportTactics(squad);
                    break;

                case AISquad.SquadRole.Scout:
                    UpdateScoutTactics(squad);
                    break;

                case AISquad.SquadRole.Artillery:
                    UpdateArtilleryTactics(squad);
                    break;
            }
        }

        private void UpdateAssaultTactics(AISquad squad)
        {
            if (squad.primaryTarget == null)
            {
                squad.primaryTarget = FindPriorityTarget(squad);
            }

            if (squad.primaryTarget != null)
            {
                float distanceToTarget = Vector3.Distance(squad.squadCenter, squad.primaryTarget.position);

                if (distanceToTarget <= assaultRange)
                {
                    squad.state = AISquad.TacticalState.Attacking;
                    foreach (var unit in squad.units)
                    {
                        if (unit != null)
                        {
                            unit.SetAggressive(true);
                            unit.SetDestination(squad.primaryTarget.position);
                        }
                    }
                }
                else
                {
                    squad.state = AISquad.TacticalState.Advancing;
                    Vector3 assaultPosition = tacticalAnalyzer.FindOptimalPosition(
                        squad.primaryTarget.position,
                        AITacticalAnalyzer.TacticalBehavior.Aggressive
                    ).Result;

                    foreach (var unit in squad.units)
                    {
                        if (unit != null)
                        {
                            unit.SetDestination(assaultPosition);
                        }
                    }
                }
            }
        }

        private void UpdateFlankerTactics(AISquad squad)
        {
            if (squad.primaryTarget == null)
            {
                squad.primaryTarget = FindPriorityTarget(squad);
                return;
            }

            squad.state = AISquad.TacticalState.Flanking;
            Vector3 flankPosition = CalculateFlankPosition(squad);

            // Spread units along the flank
            for (int i = 0; i < squad.units.Count; i++)
            {
                if (squad.units[i] == null) continue;

                float offset = (i - squad.units.Count / 2f) * flankerSpreadDistance;
                Vector3 spreadOffset = Vector3.Cross(
                    (squad.primaryTarget.position - flankPosition).normalized,
                    Vector3.up
                ) * offset;

                squad.units[i].SetDestination(flankPosition + spreadOffset);
            }
        }

        private void UpdateSupportTactics(AISquad squad)
        {
            if (squad.supportTarget == null)
            {
                squad.supportTarget = FindSquadNeedingSupport();
                if (squad.supportTarget == null) return;
            }

            squad.state = AISquad.TacticalState.Supporting;
            Vector3 supportPosition = squad.supportTarget.squadCenter - 
                (squad.supportTarget.squadCenter - squad.squadCenter).normalized * supportDistance;

            foreach (var unit in squad.units)
            {
                if (unit != null)
                {
                    unit.SetDestination(supportPosition);
                    unit.ProvideSupportFire(true);
                }
            }
        }

        private void UpdateScoutTactics(AISquad squad)
        {
            // Implement scouting behavior
            squad.state = AISquad.TacticalState.Scouting;
            // TODO: Implement patrol routes and enemy detection
        }

        private void UpdateArtilleryTactics(AISquad squad)
        {
            if (squad.primaryTarget == null)
            {
                squad.primaryTarget = FindPriorityTarget(squad);
                return;
            }

            Vector3 artilleryPosition = tacticalAnalyzer.FindOptimalPosition(
                squad.primaryTarget.position,
                AITacticalAnalyzer.TacticalBehavior.Support
            ).Result;

            foreach (var unit in squad.units)
            {
                if (unit != null)
                {
                    unit.SetDestination(artilleryPosition);
                    unit.ProvideSupportFire(true);
                }
            }
        }

        private void EnforceSquadCohesion(AISquad squad)
        {
            if (squad.units.Count <= 1) return;

            foreach (var unit in squad.units)
            {
                if (unit == null) continue;

                float distanceToCenter = Vector3.Distance(unit.transform.position, squad.squadCenter);
                if (distanceToCenter > squadCohesionRadius * squad.cohesionStrength)
                {
                    Vector3 cohesionForce = (squad.squadCenter - unit.transform.position).normalized;
                    Vector3 currentDestination = unit.transform.position + cohesionForce * squadCohesionRadius;
                    unit.SetDestination(currentDestination);
                }
            }
        }

        private void CoordinateSquadActions()
        {
            // Find squads that can support each other
            var assaultSquads = squads.Values.Where(s => s.role == AISquad.SquadRole.Assault).ToList();
            var supportSquads = squads.Values.Where(s => s.role == AISquad.SquadRole.Support).ToList();

            foreach (var assault in assaultSquads)
            {
                if (assault.state == AISquad.TacticalState.Attacking)
                {
                    // Assign support squads to assist
                    var nearbySupport = supportSquads
                        .OrderBy(s => Vector3.Distance(s.squadCenter, assault.squadCenter))
                        .FirstOrDefault();

                    if (nearbySupport != null)
                    {
                        nearbySupport.supportTarget = assault;
                    }
                }
            }
        }

        private Transform FindPriorityTarget(AISquad squad)
        {
            // Implement target priority system
            return null; // TODO: Implement target selection
        }

        private AISquad FindSquadNeedingSupport()
        {
            return squads.Values
                .Where(s => s.role == AISquad.SquadRole.Assault && s.state == AISquad.TacticalState.Attacking)
                .OrderBy(s => s.units.Average(u => u != null ? u.GetHealthPercentage() : 1f))
                .FirstOrDefault();
        }

        private Vector3 CalculateFlankPosition(AISquad squad)
        {
            if (squad.primaryTarget == null) return squad.squadCenter;

            Vector3 targetPos = squad.primaryTarget.position;
            Vector3 directionToTarget = (targetPos - squad.squadCenter).normalized;
            Vector3 flankDir = new Vector3(-directionToTarget.z, 0, directionToTarget.x);

            // Choose the flank direction with fewer enemies
            Vector3 leftFlank = targetPos + flankDir * flankerSpreadDistance;
            Vector3 rightFlank = targetPos - flankDir * flankerSpreadDistance;

            int leftEnemies = CountNearbyEnemies(leftFlank);
            int rightEnemies = CountNearbyEnemies(rightFlank);

            return leftEnemies < rightEnemies ? leftFlank : rightFlank;
        }

        private int CountNearbyEnemies(Vector3 position)
        {
            return Physics.OverlapSphere(position, assaultRange, LayerMask.GetMask("Enemy")).Length;
        }

        private int GenerateSquadId()
        {
            return squads.Count > 0 ? squads.Keys.Max() + 1 : 1;
        }
    }
}
