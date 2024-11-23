using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RTS.Units;

namespace RTS.UI
{
    public class CombatPredictor : MonoBehaviour
    {
        [Header("Prediction Settings")]
        public float predictionUpdateInterval = 0.5f;
        public float maxPredictionTime = 5f;
        public int predictionSteps = 10;
        
        [Header("Visual Settings")]
        public Material trajectoryMaterial;
        public float trajectoryWidth = 0.2f;
        public Color highProbabilityColor = Color.green;
        public Color mediumProbabilityColor = Color.yellow;
        public Color lowProbabilityColor = Color.red;
        
        [Header("Combat Parameters")]
        public float optimalEngagementRange = 20f;
        public float maxEngagementRange = 50f;
        public float terrainAdvantageMultiplier = 1.5f;
        public float moraleAdvantageMultiplier = 1.2f;

        private Dictionary<int, List<PredictionData>> groupPredictions = new Dictionary<int, List<PredictionData>>();
        private Dictionary<int, List<LineRenderer>> predictionLines = new Dictionary<int, List<LineRenderer>>();
        private ObjectPool<LineRenderer> linePool;
        private float lastUpdateTime;

        private class PredictionData
        {
            public Vector3 startPosition;
            public Vector3 predictedPosition;
            public float hitProbability;
            public float potentialDamage;
            public bool hasTerrainAdvantage;
            public MonoBehaviour target;
        }

        private void Awake()
        {
            InitializeObjectPool();
        }

        private void InitializeObjectPool()
        {
            GameObject poolContainer = new GameObject("LineRendererPool");
            poolContainer.transform.SetParent(transform);
            linePool = poolContainer.AddComponent<ObjectPool<LineRenderer>>();
            
            linePool.Initialize(CreateLineRenderer, 50);
        }

        private LineRenderer CreateLineRenderer()
        {
            GameObject lineObj = new GameObject("PredictionLine");
            lineObj.transform.SetParent(transform);
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = trajectoryMaterial;
            line.startWidth = trajectoryWidth;
            line.endWidth = trajectoryWidth;
            line.positionCount = predictionSteps;
            
            return line;
        }

        public void UpdateCombatPredictions(int groupIndex, HashSet<MonoBehaviour> units, HashSet<MonoBehaviour> enemies)
        {
            if (Time.time - lastUpdateTime < predictionUpdateInterval) return;
            
            ClearPredictions(groupIndex);
            List<PredictionData> predictions = new List<PredictionData>();

            foreach (var unit in units)
            {
                var unitPredictions = PredictUnitCombat(unit, enemies);
                predictions.AddRange(unitPredictions);
            }

            groupPredictions[groupIndex] = predictions;
            VisualizePredictions(groupIndex, predictions);
            lastUpdateTime = Time.time;
        }

        private List<PredictionData> PredictUnitCombat(MonoBehaviour unit, HashSet<MonoBehaviour> enemies)
        {
            List<PredictionData> predictions = new List<PredictionData>();
            Vector3 unitPosition = unit.transform.position;
            float unitElevation = GetTerrainHeight(unitPosition);

            foreach (var enemy in enemies)
            {
                Vector3 enemyPosition = enemy.transform.position;
                float enemyElevation = GetTerrainHeight(enemyPosition);
                bool hasTerrainAdvantage = unitElevation > enemyElevation + 2f; // 2 meters height advantage

                float distance = Vector3.Distance(unitPosition, enemyPosition);
                if (distance > maxEngagementRange) continue;

                float baseProbability = CalculateHitProbability(unit, enemy, distance);
                float potentialDamage = CalculatePotentialDamage(unit, enemy, distance);

                // Apply terrain and morale modifiers
                if (hasTerrainAdvantage)
                {
                    baseProbability *= terrainAdvantageMultiplier;
                    potentialDamage *= terrainAdvantageMultiplier;
                }

                // Consider unit morale if available
                if (unit is Artillery artillery)
                {
                    float moraleModifier = Mathf.Lerp(0.5f, moraleAdvantageMultiplier, artillery.CurrentMorale);
                    baseProbability *= moraleModifier;
                    potentialDamage *= moraleModifier;
                }

                Vector3 predictedPosition = PredictTargetPosition(enemy, maxPredictionTime);

                predictions.Add(new PredictionData
                {
                    startPosition = unitPosition,
                    predictedPosition = predictedPosition,
                    hitProbability = baseProbability,
                    potentialDamage = potentialDamage,
                    hasTerrainAdvantage = hasTerrainAdvantage,
                    target = enemy
                });
            }

            return predictions;
        }

        private float CalculateHitProbability(MonoBehaviour attacker, MonoBehaviour target, float distance)
        {
            float baseAccuracy = 0.8f;
            float rangeModifier = 1f - (distance / maxEngagementRange);
            
            // Consider unit-specific factors
            if (attacker is Artillery artillery)
            {
                baseAccuracy *= Mathf.Lerp(0.5f, 1f, artillery.CurrentAmmo / artillery.MaxAmmo);
            }
            else if (attacker is HeavyDefenseBunker bunker)
            {
                baseAccuracy *= 0.9f; // Bunkers are more accurate
            }

            // Apply movement penalties
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null && targetRb.velocity.magnitude > 0.1f)
            {
                baseAccuracy *= 0.8f;
            }

            return Mathf.Clamp01(baseAccuracy * rangeModifier);
        }

        private float CalculatePotentialDamage(MonoBehaviour attacker, MonoBehaviour target, float distance)
        {
            float baseDamage = 10f;
            float rangeModifier = 1f - (distance / maxEngagementRange);

            if (attacker is Artillery artillery)
            {
                baseDamage = artillery.DamageMultiplier * artillery.CurrentAmmo;
            }
            else if (attacker is HeavyDefenseBunker bunker)
            {
                baseDamage = bunker.DamageMultiplier * bunker.CurrentAmmo;
            }

            return baseDamage * rangeModifier;
        }

        private Vector3 PredictTargetPosition(MonoBehaviour target, float predictionTime)
        {
            Vector3 currentPosition = target.transform.position;
            Rigidbody rb = target.GetComponent<Rigidbody>();
            
            if (rb != null && rb.velocity.magnitude > 0.1f)
            {
                return currentPosition + (rb.velocity * predictionTime);
            }
            
            return currentPosition;
        }

        private float GetTerrainHeight(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, LayerMask.GetMask("Terrain")))
            {
                return hit.point.y;
            }
            return position.y;
        }

        private void VisualizePredictions(int groupIndex, List<PredictionData> predictions)
        {
            if (!predictionLines.ContainsKey(groupIndex))
            {
                predictionLines[groupIndex] = new List<LineRenderer>();
            }

            // Return existing lines to pool
            foreach (var line in predictionLines[groupIndex])
            {
                linePool.ReturnToPool(line);
            }
            predictionLines[groupIndex].Clear();

            foreach (var prediction in predictions)
            {
                LineRenderer line = linePool.Get();
                predictionLines[groupIndex].Add(line);

                Vector3 start = prediction.startPosition;
                Vector3 end = prediction.predictedPosition;
                
                // Generate trajectory points
                Vector3[] points = new Vector3[predictionSteps];
                for (int i = 0; i < predictionSteps; i++)
                {
                    float t = i / (float)(predictionSteps - 1);
                    points[i] = Vector3.Lerp(start, end, t);
                    
                    // Add slight arc for artillery
                    if (prediction.target is Artillery)
                    {
                        float arc = Mathf.Sin(t * Mathf.PI) * 5f;
                        points[i].y += arc;
                    }
                }
                
                line.positionCount = predictionSteps;
                line.SetPositions(points);

                // Set color based on hit probability
                Color trajectoryColor = Color.Lerp(
                    Color.Lerp(lowProbabilityColor, mediumProbabilityColor, prediction.hitProbability * 2),
                    highProbabilityColor,
                    prediction.hitProbability
                );
                
                line.startColor = trajectoryColor;
                line.endColor = new Color(trajectoryColor.r, trajectoryColor.g, trajectoryColor.b, 0.2f);
            }
        }

        private void ClearPredictions(int groupIndex)
        {
            if (groupPredictions.ContainsKey(groupIndex))
            {
                groupPredictions.Remove(groupIndex);
            }

            if (predictionLines.ContainsKey(groupIndex))
            {
                foreach (var line in predictionLines[groupIndex])
                {
                    linePool.ReturnToPool(line);
                }
                predictionLines[groupIndex].Clear();
            }
        }

        public void OnGroupDestroyed(int groupIndex)
        {
            ClearPredictions(groupIndex);
        }

        private void OnDestroy()
        {
            foreach (var groupLines in predictionLines.Values)
            {
                foreach (var line in groupLines)
                {
                    Destroy(line.gameObject);
                }
            }
            predictionLines.Clear();
            groupPredictions.Clear();
        }
    }
}
