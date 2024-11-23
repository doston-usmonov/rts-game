using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using RTS.UI;

namespace RTS.AI
{
    public class AITacticalAnalyzer : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private TerrainAnalyzer terrainAnalyzer;
        [SerializeField] private float tacticalUpdateInterval = 1f;
        [SerializeField] private float maxAnalysisRange = 100f;
        [SerializeField] private int maxConcurrentAnalysis = 10;

        [Header("Tactical Weights")]
        [SerializeField] private float heightAdvantageWeight = 1.5f;
        [SerializeField] private float coverWeight = 1.2f;
        [SerializeField] private float mobilityWeight = 1.0f;
        [SerializeField] private float threatAvoidanceWeight = 2.0f;

        private Dictionary<int, TacticalState> unitTacticalStates = new Dictionary<int, TacticalState>();
        private float lastUpdateTime;

        public class TacticalState
        {
            public Vector3 optimalPosition;
            public float tacticalScore;
            public bool needsRepositioning;
            public TacticalBehavior currentBehavior;
            public List<Vector3> potentialCoverPoints = new List<Vector3>();
            public float lastStateUpdateTime;
        }

        public enum TacticalBehavior
        {
            Aggressive,    // Seeks high ground, ignores cover
            Defensive,     // Prioritizes cover and defensive positions
            Mobile,        // Prioritizes movement and flanking opportunities
            Support       // Stays behind friendly units, seeks safe positions
        }

        private void Update()
        {
            if (Time.time - lastUpdateTime >= tacticalUpdateInterval)
            {
                lastUpdateTime = Time.time;
                _ = UpdateTacticalStates();
            }
        }

        private async Task UpdateTacticalStates()
        {
            var activeUnits = FindObjectsOfType<AIUnit>();
            var analysisGroups = activeUnits
                .Select((unit, index) => new { unit, index })
                .GroupBy(x => x.index / maxConcurrentAnalysis)
                .Select(g => g.Select(x => x.unit));

            foreach (var group in analysisGroups)
            {
                var tasks = group.Select(unit => AnalyzeUnitTactics(unit));
                await Task.WhenAll(tasks);
            }
        }

        private async Task AnalyzeUnitTactics(AIUnit unit)
        {
            if (unit == null) return;

            int unitId = unit.GetInstanceID();
            if (!unitTacticalStates.TryGetValue(unitId, out var state))
            {
                state = new TacticalState();
                unitTacticalStates[unitId] = state;
            }

            // Skip if recently updated
            if (Time.time - state.lastStateUpdateTime < tacticalUpdateInterval)
                return;

            // Analyze current position
            var currentTerrainInfo = await terrainAnalyzer.AnalyzeTerrainAtPosition(unit.transform.position);
            if (currentTerrainInfo == null) return;

            // Determine optimal behavior based on unit type and situation
            state.currentBehavior = DetermineTacticalBehavior(unit, currentTerrainInfo);

            // Find optimal position based on behavior
            var optimalPosition = await FindOptimalPosition(unit, state.currentBehavior);
            float newScore = await EvaluatePosition(optimalPosition, state.currentBehavior);

            // Update state if better position found
            if (newScore > state.tacticalScore)
            {
                state.optimalPosition = optimalPosition;
                state.tacticalScore = newScore;
                state.needsRepositioning = true;
            }

            // Update cover points
            state.potentialCoverPoints = await FindCoverPoints(unit.transform.position);
            state.lastStateUpdateTime = Time.time;

            // Apply tactical decisions
            ApplyTacticalDecisions(unit, state);
        }

        private TacticalBehavior DetermineTacticalBehavior(AIUnit unit, TerrainAnalyzer.TerrainCell currentTerrain)
        {
            // Consider unit type
            if (unit is Artillery)
                return currentTerrain.isHighGround ? TacticalBehavior.Aggressive : TacticalBehavior.Mobile;

            if (unit is HeavyDefenseBunker)
                return currentTerrain.providesCover ? TacticalBehavior.Defensive : TacticalBehavior.Mobile;

            // Consider unit health
            float healthPercentage = unit.GetHealthPercentage();
            if (healthPercentage < 0.3f)
                return TacticalBehavior.Defensive;
            if (healthPercentage < 0.6f)
                return TacticalBehavior.Support;

            // Consider enemy presence
            if (IsUnderHeavyAttack(unit))
                return TacticalBehavior.Defensive;

            return TacticalBehavior.Aggressive;
        }

        private async Task<Vector3> FindOptimalPosition(AIUnit unit, TacticalBehavior behavior)
        {
            Vector3 currentPos = unit.transform.position;
            List<Vector3> candidatePositions = GenerateCandidatePositions(currentPos);
            
            float bestScore = float.MinValue;
            Vector3 bestPosition = currentPos;

            foreach (var position in candidatePositions)
            {
                float score = await EvaluatePosition(position, behavior);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = position;
                }
            }

            return bestPosition;
        }

        private List<Vector3> GenerateCandidatePositions(Vector3 center)
        {
            List<Vector3> positions = new List<Vector3>();
            float step = 5f; // Distance between candidate positions
            int rings = 5;   // Number of concentric rings to check

            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = ring * step;
                int points = Mathf.Max(8 * ring, 8); // More points in outer rings
                
                for (int i = 0; i < points; i++)
                {
                    float angle = (i * 360f / points) * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius
                    );
                    positions.Add(center + offset);
                }
            }

            return positions;
        }

        private async Task<float> EvaluatePosition(Vector3 position, TacticalBehavior behavior)
        {
            var terrainInfo = await terrainAnalyzer.AnalyzeTerrainAtPosition(position);
            if (terrainInfo == null) return float.MinValue;

            float score = 0f;

            // Base terrain score
            score += terrainInfo.movementModifier * mobilityWeight;
            if (terrainInfo.isHighGround) score += heightAdvantageWeight;
            if (terrainInfo.providesCover) score += coverWeight;

            // Behavior-specific scoring
            switch (behavior)
            {
                case TacticalBehavior.Aggressive:
                    score += terrainInfo.isHighGround ? heightAdvantageWeight * 2 : 0;
                    score += terrainInfo.movementModifier * mobilityWeight * 1.5f;
                    break;

                case TacticalBehavior.Defensive:
                    score += terrainInfo.providesCover ? coverWeight * 2 : 0;
                    score += terrainInfo.isHighGround ? heightAdvantageWeight * 1.5f : 0;
                    break;

                case TacticalBehavior.Mobile:
                    score += terrainInfo.movementModifier * mobilityWeight * 2;
                    score -= terrainInfo.isImpassable ? 1000 : 0;
                    break;

                case TacticalBehavior.Support:
                    score += terrainInfo.providesCover ? coverWeight * 1.5f : 0;
                    score += GetSupportPositionScore(position);
                    break;
            }

            // Threat avoidance
            score -= GetThreatLevel(position) * threatAvoidanceWeight;

            return score;
        }

        private float GetSupportPositionScore(Vector3 position)
        {
            float score = 0f;
            var friendlyUnits = FindObjectsOfType<AIUnit>()
                .Where(u => u != null && !IsEnemy(u));

            foreach (var friendly in friendlyUnits)
            {
                float distance = Vector3.Distance(position, friendly.transform.position);
                if (distance < 10f) // Optimal support range
                    score += 1f - (distance / 10f);
            }

            return score;
        }

        private float GetThreatLevel(Vector3 position)
        {
            float threat = 0f;
            var enemies = FindObjectsOfType<AIUnit>()
                .Where(u => u != null && IsEnemy(u));

            foreach (var enemy in enemies)
            {
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < enemy.GetAttackRange())
                    threat += 1f - (distance / enemy.GetAttackRange());
            }

            return threat;
        }

        private async Task<List<Vector3>> FindCoverPoints(Vector3 position)
        {
            var terrainInfo = await terrainAnalyzer.AnalyzeTerrainAtPosition(position);
            if (terrainInfo == null || !terrainInfo.providesCover)
                return new List<Vector3>();

            return terrainInfo.coverPoints
                .OrderBy(p => Vector3.Distance(position, p))
                .Take(5)
                .ToList();
        }

        private void ApplyTacticalDecisions(AIUnit unit, TacticalState state)
        {
            if (unit == null || !state.needsRepositioning)
                return;

            // Apply movement
            if (Vector3.Distance(unit.transform.position, state.optimalPosition) > 1f)
            {
                unit.SetDestination(state.optimalPosition);
            }

            // Apply behavior-specific actions
            switch (state.currentBehavior)
            {
                case TacticalBehavior.Aggressive:
                    unit.SetAggressive(true);
                    break;

                case TacticalBehavior.Defensive:
                    unit.SetAggressive(false);
                    if (state.potentialCoverPoints.Count > 0)
                        unit.UseCover(state.potentialCoverPoints[0]);
                    break;

                case TacticalBehavior.Mobile:
                    unit.SetAggressive(false);
                    unit.EnableFlanking(true);
                    break;

                case TacticalBehavior.Support:
                    unit.SetAggressive(false);
                    unit.ProvideSupportFire(true);
                    break;
            }

            state.needsRepositioning = false;
        }

        private bool IsUnderHeavyAttack(AIUnit unit)
        {
            int nearbyEnemies = Physics.OverlapSphere(
                unit.transform.position,
                unit.GetAttackRange(),
                LayerMask.GetMask("Enemy")
            ).Length;

            return nearbyEnemies >= 3;
        }

        private bool IsEnemy(AIUnit unit)
        {
            return unit.gameObject.layer == LayerMask.NameToLayer("Enemy");
        }
    }
}
