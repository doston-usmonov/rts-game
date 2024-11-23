using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RTS.Units;

namespace RTS.UI
{
    public class SpecializedFormation : MonoBehaviour
    {
        [Header("Formation Settings")]
        public float minUnitSpacing = 2f;
        public float maxUnitSpacing = 5f;
        public float formationDepth = 10f;
        public float terrainCheckHeight = 100f;

        [Header("Terrain Analysis")]
        public LayerMask terrainLayer;
        public LayerMask coverLayer;
        public float maxSlopeDegrees = 30f;
        public float coverDetectionRadius = 5f;
        public float highGroundThreshold = 2f;
        public float lowGroundPenalty = 0.8f;
        public float highGroundBonus = 1.2f;
        public float heightAdvantageMultiplier = 1.5f;
        public float coverValueMultiplier = 2f;

        // Squad role-specific formation parameters
        [SerializeField] private float assaultSpacing = 2f;
        [SerializeField] private float flankerSpacing = 4f;
        [SerializeField] private float supportSpacing = 3f;
        [SerializeField] private float artillerySpacing = 5f;
        [SerializeField] private float scoutSpacing = 6f;

        // Morale and spacing parameters
        [SerializeField] private float baseMorale = 100f;
        [SerializeField] private float moraleDecayRate = 5f;
        [SerializeField] private float moraleLowThreshold = 30f;
        [SerializeField] private float moraleRecoveryRate = 2f;
        [SerializeField] private float enemyProximityMoraleImpact = -10f;
        [SerializeField] private float leaderProximityMoraleBonus = 15f;

        // Dynamic spacing modifiers
        [SerializeField] private float combatSpacingMultiplier = 1.5f;
        [SerializeField] private float retreatSpacingMultiplier = 2f;
        [SerializeField] private float urbanSpacingMultiplier = 0.8f;
        [SerializeField] private float forestSpacingMultiplier = 0.6f;

        // Combat behavior parameters
        [SerializeField] private float chargeTriggerDistance = 10f;
        [SerializeField] private float retreatHealthThreshold = 0.3f;
        [SerializeField] private float flankingAngleThreshold = 45f;
        [SerializeField] private float artilleryMinRange = 20f;
        [SerializeField] private float artilleryMaxRange = 100f;

        // Leader ability parameters
        [SerializeField] private float leaderBuffRange = 15f;
        [SerializeField] private float moraleBoostAmount = 25f;
        [SerializeField] private float moraleBoostDuration = 10f;
        [SerializeField] private float damageBuffMultiplier = 1.3f;
        [SerializeField] private float speedBuffMultiplier = 1.2f;
        [SerializeField] private float healingPerSecond = 10f;

        // Formation transition parameters
        [SerializeField] private float formationTransitionSpeed = 5f;
        [SerializeField] private float formationTighteningMultiplier = 0.6f;
        [SerializeField] private float formationLooseningMultiplier = 1.5f;
        [SerializeField] private float chargeFormationSpacing = 1.5f;
        [SerializeField] private float defensiveFormationSpacing = 3f;

        // Squad coordination parameters
        [SerializeField] private float squadCommunicationRange = 30f;
        [SerializeField] private float supportDistance = 15f;

        // Tactical maneuver parameters
        [SerializeField] private float flankingDistance = 25f;
        [SerializeField] private float ambushPreparationTime = 3f;
        [SerializeField] private float pincerAngle = 120f;

        // Pathfinding parameters
        [SerializeField] private float pathNodeSpacing = 5f;
        [SerializeField] private float maxPathDeviation = 30f;
        [SerializeField] private int maxPathNodes = 20;
        [SerializeField] private float pathSmoothingFactor = 0.5f;

        private Dictionary<UnitType, FormationPreset> specializedFormations;
        private Dictionary<int, float> unitMorale = new Dictionary<int, float>();
        private Dictionary<int, Vector3> lastKnownPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, SquadRole> currentRoles = new Dictionary<int, SquadRole>();
        private Dictionary<int, CombatState> unitCombatStates = new Dictionary<int, CombatState>();
        private Dictionary<int, LeaderAbility> activeLeaderAbilities = new Dictionary<int, LeaderAbility>();
        private Dictionary<int, float> abilityTimers = new Dictionary<int, float>();
        private Dictionary<int, List<LeaderBuff>> activeBuffs = new Dictionary<int, List<LeaderBuff>>();
        private Dictionary<int, Vector3> targetPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, FormationTransitionState> transitionStates = new Dictionary<int, FormationTransitionState>();
        private Dictionary<int, SquadData> squadDataMap = new Dictionary<int, SquadData>();

        // Performance optimization - cache paths and scores
        private Dictionary<(Vector3, Vector3), List<Vector3>> pathCache = new Dictionary<(Vector3, Vector3), List<Vector3>>();
        private const int MAX_CACHED_PATHS = 100;

        [System.Serializable]
        private class ThreatWeightModifiers
        {
            public float TerrainDensityEffect = 0.7f;    // How much terrain density reduces artillery threat
            public float RangeEffect = 1.2f;             // Multiplier for units at optimal range
            public float HeightAdvantageEffect = 1.3f;   // Multiplier for units with height advantage
            public float SquadSizeEffect = 1.1f;         // Additional threat per squad member
        }

        [SerializeField] private ThreatWeightModifiers threatModifiers;
        private Dictionary<int, float> cachedThreatScores = new Dictionary<int, float>();
        private float threatScoreCacheTime = 0.5f;       // How long to cache threat scores
        private Dictionary<int, float> threatScoreTimestamps = new Dictionary<int, float>();

        #if UNITY_EDITOR
        [System.Serializable]
        private class DebugSettings
        {
            public bool ShowPaths = true;
            public bool ShowCover = true;
            public bool ShowTacticalPositions = true;
            public bool ShowPerformanceStats = true;
            public float VisualizationHeight = 1f;
        }

        [SerializeField] private DebugSettings debugSettings = new DebugSettings();
        
        private Dictionary<CoordinationType, Color> tacticalColors = new Dictionary<CoordinationType, Color>()
        {
            { CoordinationType.Flanking, new Color(1f, 0.5f, 0f, 0.7f) },  // Orange
            { CoordinationType.Support, new Color(0f, 1f, 0f, 0.7f) },     // Green
            { CoordinationType.Pincer, new Color(1f, 0f, 0f, 0.7f) },      // Red
            { CoordinationType.Ambush, new Color(0.5f, 0f, 1f, 0.7f) }     // Purple
        };

        [System.Serializable]
        private class TacticalAnalysis
        {
            public float CoverScore;
            public float ThreatLevel;
            public string CurrentDecision;
            public List<string> Feedback = new List<string>();
            public bool IsPositionOptimal;
            public Vector3 SuggestedPosition;
        }

        private Dictionary<UnitType, float> unitThreatValues = new Dictionary<UnitType, float>()
        {
            { UnitType.Artillery, 1.5f },
            { UnitType.HeavyDefense, 1.2f },
            { UnitType.Support, 0.7f },
            { UnitType.Scout, 0.5f },
            { UnitType.Infantry, 1.0f }
        };

        private Dictionary<int, TacticalAnalysis> squadAnalysis = new Dictionary<int, TacticalAnalysis>();

        // Performance monitoring
        private class PerformanceMetrics
        {
            public float LastPathfindingTime;
            public int CacheHits;
            public int CacheMisses;
            public float AveragePathLength;
            public int ActiveSquads;
            public Dictionary<CoordinationType, int> TacticalTypeCount = new Dictionary<CoordinationType, int>();
            
            // New detailed metrics
            public float AverageThreatCalculationTime;
            public float MaxThreatCalculationTime;
            public float AverageTerrainAnalysisTime;
            public float MaxTerrainAnalysisTime;
            public int TotalCacheEntries;
            public float CacheHitRate;
            public Dictionary<UnitType, float> AverageUnitEffectiveness = new Dictionary<UnitType, float>();
        }
        
        private PerformanceMetrics metrics = new PerformanceMetrics();
        private Queue<float> pathfindingTimes = new Queue<float>();
        private const int MAX_TIME_SAMPLES = 50;

        [System.Serializable]
        private class StressTestSettings
        {
            public bool EnableStressTest = false;
            public int ExtraSquadsToSpawn = 10;
            public float UpdateInterval = 0.1f;
            public bool LogPerformanceMetrics = true;
        }

        [SerializeField] private StressTestSettings stressTestSettings;
        private List<float> updateTimes = new List<float>();
        private float lastStressUpdate;

        [System.Serializable]
        private class StressTestVisualization
        {
            public bool ShowPerformanceGraph = true;
            public bool ShowHeatmap = true;
            public bool ShowUnitEffectiveness = true;
            public float HeatmapUpdateInterval = 0.5f;
            public Color LowLoadColor = Color.green;
            public Color MediumLoadColor = Color.yellow;
            public Color HighLoadColor = Color.red;
            
            // Heatmap filter options
            public bool ShowTacticalLoad = true;
            public bool ShowTerrainLoad = true;
            public bool ShowUnitDensityLoad = true;
            public bool ShowPathfindingHeatmap = true;
            
            // Pathfinding visualization
            public bool ShowPathfindingIssues = true;
            public float PathCongestionThreshold = 0.7f;
            public float ChokePointDetectionRadius = 10f;
            
            // Advanced visualization options
            public bool ShowDebugPaths = true;
            public bool ShowEfficiencyDashboard = true;
            public bool ShowDetailedMetrics = true;
            public float PathDeviationThreshold = 0.3f;
            
            // Visual style settings
            public Color OptimalPathColor = new Color(0.2f, 0.8f, 0.2f, 0.6f);
            public Color DeviationPathColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
            public float PathLineWidth = 0.2f;
            public float DashLength = 1f;
            
            // Layer visibility options
            public bool ShowCongestionLayer = true;
            public bool ShowTerrainLayer = true;
            public bool ShowPredictivePaths = true;
            
            // Alert system settings
            public float CriticalDeviationThreshold = 0.6f;
            public float CriticalCongestionThreshold = 0.8f;
            public float AlertDisplayDuration = 5f;
            
            // Customization settings
            public float PathOpacity = 0.8f;
            public float DashSize = 1f;
            public bool EnableAIOptimization = true;
        }

        [SerializeField] private StressTestVisualization stressTestViz = new StressTestVisualization();
        private List<Vector2> performanceGraph = new List<Vector2>();
        private Dictionary<Vector2Int, float> computationalHeatmap = new Dictionary<Vector2Int, float>();
        private Dictionary<Vector2Int, PathfindingMetrics> pathfindingHeatmap = new Dictionary<Vector2Int, PathfindingMetrics>();
        private float lastHeatmapUpdate;

        private class PerformanceThresholds
        {
            public const float HIGH_LOAD = 0.8f;
            public const float MEDIUM_LOAD = 0.5f;
            public const float OPTIMAL_SUPPORT_RANGE = 15f;
            public const float MAX_HEIGHT_ADVANTAGE = 10f;
            public const float CRITICAL_UNIT_DENSITY = 0.7f;
        }

        private struct PathfindingMetrics
        {
            public int PathRequestCount;
            public int FailedPathRequests;
            public float AveragePathLength;
            public float CongestionLevel;
            public bool IsChokePoint;
            public Dictionary<UnitType, UnitTypePathMetrics> UnitTypeMetrics;
        }

        private struct UnitTypePathMetrics
        {
            public int SuccessfulPaths;
            public int FailedPaths;
            public float AveragePathLength;
            public float AverageDeviationFromOptimal;
            public float EfficiencyScore;
        }

        private Color GetLoadColor(float load)
        {
            if (load >= PerformanceThresholds.HIGH_LOAD)
                return Color.Lerp(stressTestViz.HighLoadColor, Color.red, (load - PerformanceThresholds.HIGH_LOAD) * 5f);
            if (load >= PerformanceThresholds.MEDIUM_LOAD)
                return Color.Lerp(stressTestViz.MediumLoadColor, stressTestViz.HighLoadColor, 
                    (load - PerformanceThresholds.MEDIUM_LOAD) / (PerformanceThresholds.HIGH_LOAD - PerformanceThresholds.MEDIUM_LOAD));
            return Color.Lerp(stressTestViz.LowLoadColor, stressTestViz.MediumLoadColor, 
                load / PerformanceThresholds.MEDIUM_LOAD);
        }

        private void UpdateStressTestVisualization()
        {
            if (!stressTestSettings.EnableStressTest || !stressTestViz.ShowPerformanceGraph) return;

            // Update performance graph
            if (updateTimes.Count > 0)
            {
                float currentLoad = updateTimes.Average() * 1000f;
                performanceGraph.Add(new Vector2(Time.time, currentLoad));
                
                // Keep last 100 samples
                if (performanceGraph.Count > 100)
                    performanceGraph.RemoveAt(0);
            }

            // Update heatmap
            if (stressTestViz.ShowHeatmap && Time.time - lastHeatmapUpdate > stressTestViz.HeatmapUpdateInterval)
            {
                lastHeatmapUpdate = Time.time;
                computationalHeatmap.Clear();

                foreach (var squad in squadDataMap.Values)
                {
                    Vector3 pos = GetSquadPosition(squad.TargetSquad);
                    Vector2Int gridPos = new Vector2Int(
                        Mathf.RoundToInt(pos.x / 10f),
                        Mathf.RoundToInt(pos.z / 10f)
                    );

                    float computationalLoad = CalculateLocalComputationalLoad(pos);
                    computationalHeatmap[gridPos] = computationalLoad;
                }
            }
        }

        private float CalculateLocalComputationalLoad(Vector3 position)
        {
            float load = 0f;
            var nearbySquads = GetNearbySquads(position, 30f);
            
            // Factor in number of nearby units
            load += nearbySquads.Count * 0.2f;
            
            // Factor in terrain complexity
            load += CalculateTerrainDensity(position, 20f) * 0.3f;
            
            // Factor in tactical complexity
            int tacticalOperations = 0;
            foreach (var squadId in nearbySquads)
            {
                var squad = squadDataMap[squadId];
                if (squad.CoordinationType != CoordinationType.None)
                    tacticalOperations++;
            }
            load += tacticalOperations * 0.15f;

            return Mathf.Clamp01(load);
        }

        private void DrawStressTestVisuals()
        {
            if (!stressTestSettings.EnableStressTest) return;

            // Draw performance graph
            if (stressTestViz.ShowPerformanceGraph && performanceGraph.Count > 1)
            {
                Handles.BeginGUI();
                var graphRect = new Rect(Screen.width - 220, 10, 200, 100);
                Handles.DrawSolidRectangleWithOutline(graphRect, 
                    new Color(0, 0, 0, 0.8f), Color.white);

                // Draw graph lines
                for (int i = 0; i < performanceGraph.Count - 1; i++)
                {
                    Vector2 start = new Vector2(
                        graphRect.x + (i * graphRect.width / 100f),
                        graphRect.y + graphRect.height - (performanceGraph[i].y * graphRect.height / 16.7f)
                    );
                    Vector2 end = new Vector2(
                        graphRect.x + ((i + 1) * graphRect.width / 100f),
                        graphRect.y + graphRect.height - (performanceGraph[i + 1].y * graphRect.height / 16.7f)
                    );
                    
                    Color lineColor = Color.Lerp(
                        stressTestViz.LowLoadColor,
                        stressTestViz.HighLoadColor,
                        performanceGraph[i + 1].y / 16.7f
                    );
                    Handles.color = lineColor;
                    Handles.DrawLine(start, end);
                }

                // Draw labels
                GUI.color = Color.white;
                GUI.Label(new Rect(graphRect.x + 5, graphRect.y + 5, 190, 20),
                    $"Update Time (ms): {performanceGraph[performanceGraph.Count - 1].y:F2}");
                GUI.Label(new Rect(graphRect.x + 5, graphRect.y + 25, 190, 20),
                    $"Active Squads: {squadDataMap.Count}");

                Handles.EndGUI();
            }

            // Draw computational heatmap
            if (stressTestViz.ShowHeatmap)
            {
                foreach (var kvp in computationalHeatmap)
                {
                    Vector3 worldPos = new Vector3(
                        kvp.Key.x * 10f,
                        0f,
                        kvp.Key.y * 10f
                    );

                    float tacticalLoad = stressTestViz.ShowTacticalLoad ? CalculateTacticalLoad(worldPos) : 0f;
                    float terrainLoad = stressTestViz.ShowTerrainLoad ? CalculateTerrainLoad(worldPos) : 0f;
                    float unitDensityLoad = stressTestViz.ShowUnitDensityLoad ? CalculateUnitDensityLoad(worldPos) : 0f;
                    
                    // Calculate total load based on enabled filters
                    int activeFilters = (stressTestViz.ShowTacticalLoad ? 1 : 0) +
                                      (stressTestViz.ShowTerrainLoad ? 1 : 0) +
                                      (stressTestViz.ShowUnitDensityLoad ? 1 : 0);
                    
                    float totalLoad = activeFilters > 0 ? 
                        (tacticalLoad + terrainLoad + unitDensityLoad) / activeFilters : 0f;

                    // Draw pathfinding heatmap overlay
                    if (stressTestViz.ShowPathfindingHeatmap && pathfindingHeatmap.ContainsKey(kvp.Key))
                    {
                        var pathMetrics = pathfindingHeatmap[kvp.Key];
                        if (pathMetrics.CongestionLevel >= stressTestViz.PathCongestionThreshold)
                        {
                            // Draw congestion warning
                            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
                            Color congestionColor = Color.Lerp(Color.yellow, Color.red, pulse);
                            congestionColor.a = 0.4f;
                            Gizmos.color = congestionColor;
                            Gizmos.DrawWireCube(worldPos + Vector3.up * 0.2f, new Vector3(9f, 0.1f, 9f));
                        }

                        if (pathMetrics.IsChokePoint)
                        {
                            // Draw chokepoint indicator
                            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f); // Orange
                            for (int i = 0; i < 8; i++)
                            {
                                float angle = i * 45f * Mathf.Deg2Rad;
                                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 4f;
                                Gizmos.DrawLine(worldPos + Vector3.up * 0.3f,
                                             worldPos + Vector3.up * 0.3f + direction);
                            }
                        }
                    }

                    Color heatmapColor = GetLoadColor(totalLoad);
                    heatmapColor.a = 0.3f;

                    Gizmos.color = heatmapColor;
                    Gizmos.DrawCube(worldPos, new Vector3(9f, 0.1f, 9f));

                    // Draw warning indicators for high load areas
                    if (totalLoad >= PerformanceThresholds.HIGH_LOAD)
                    {
                        float warningPulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                        Color warningColor = Color.Lerp(Color.yellow, Color.red, warningPulse);
                        warningColor.a = 0.4f;
                        Gizmos.color = warningColor;
                        Gizmos.DrawWireCube(worldPos + Vector3.up * 0.1f, new Vector3(9.5f, 0.15f, 9.5f));
                    }

                    // Mouse hover feedback
                    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    if (Vector3.Distance(new Vector3(mouseWorldPos.x, worldPos.y, mouseWorldPos.z), worldPos) < 5f)
                    {
                        Vector3 labelPos = worldPos + Vector3.up * 2f;
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(labelPos);
                        if (screenPos.z > 0)
                        {
                            Handles.BeginGUI();
                            GUI.color = Color.white;
                            
                            // Enhanced load breakdown display with filtering info
                            Rect labelRect = new Rect(screenPos.x - 80, Screen.height - screenPos.y, 160, 140);
                            GUI.Box(labelRect, "");
                            
                            string loadStatus = totalLoad >= PerformanceThresholds.HIGH_LOAD ? "HIGH LOAD!" :
                                              totalLoad >= PerformanceThresholds.MEDIUM_LOAD ? "Medium Load" : "Normal";
                            
                            int yOffset = 5;
                            GUI.Label(new Rect(labelRect.x + 5, labelRect.y + yOffset, 150, 20),
                                $"Status: {loadStatus}");
                            
                            if (stressTestViz.ShowTacticalLoad)
                            {
                                yOffset += 20;
                                GUI.Label(new Rect(labelRect.x + 5, labelRect.y + yOffset, 150, 20),
                                    $"Tactical: {tacticalLoad:P0}");
                            }
                            
                            if (stressTestViz.ShowTerrainLoad)
                            {
                                yOffset += 20;
                                GUI.Label(new Rect(labelRect.x + 5, labelRect.y + yOffset, 150, 20),
                                    $"Terrain: {terrainLoad:P0}");
                            }
                            
                            if (stressTestViz.ShowUnitDensityLoad)
                            {
                                yOffset += 20;
                                GUI.Label(new Rect(labelRect.x + 5, labelRect.y + yOffset, 150, 20),
                                    $"Units: {unitDensityLoad:P0}");
                            }

                            if (stressTestViz.ShowPathfindingHeatmap && pathfindingHeatmap.ContainsKey(kvp.Key))
                            {
                                var pathMetrics = pathfindingHeatmap[kvp.Key];
                                yOffset += 20;
                                GUI.Label(new Rect(labelRect.x + 5, labelRect.y + yOffset, 150, 20),
                                    $"Path Success: {1f - (float)pathMetrics.FailedPathRequests / pathMetrics.PathRequestCount:P0}");
                                yOffset += 20;
                                GUI.Label(new Rect(labelRect.x + 5, labelRect.y + yOffset, 150, 20),
                                    $"Congestion: {pathMetrics.CongestionLevel:P0}");
                                
                                // Unit-specific metrics
                                if (pathMetrics.UnitTypeMetrics != null && pathMetrics.UnitTypeMetrics.Count > 0)
                                {
                                    yOffset += 25;
                                    GUI.Label(new Rect(labelRect.x + 5, labelRect.y + yOffset, 150, 20),
                                        "Unit Performance:");
                                        
                                    foreach (var unitMetrics in pathMetrics.UnitTypeMetrics)
                                    {
                                        yOffset += 20;
                                        string efficiency = unitMetrics.Value.EfficiencyScore >= 0.8f ? "High" :
                                                          unitMetrics.Value.EfficiencyScore >= 0.5f ? "Med" : "Low";
                                        GUI.Label(new Rect(labelRect.x + 5, labelRect.y + yOffset, 150, 20),
                                            $"{unitMetrics.Key}: {efficiency} ({unitMetrics.Value.EfficiencyScore:P0})");
                                    }
                                }
                            }
                            
                            // Show optimization hints
                            if (totalLoad >= PerformanceThresholds.HIGH_LOAD)
                            {
                                string hint = GetOptimizationHint(tacticalLoad, terrainLoad, unitDensityLoad);
                                GUI.Label(new Rect(labelRect.x + 5, labelRect.y + 160, 150, 20),
                                    $"Hint: {hint}");
                            }
                            
                            Handles.EndGUI();
                        }
                    }
                }
            }

            // Draw unit effectiveness
            if (stressTestViz.ShowUnitEffectiveness)
            {
                foreach (var squad in squadDataMap.Values)
                {
                    var position = GetSquadPosition(squad.TargetSquad);
                    var unitType = GetSquadUnitType(squad.TargetSquad);
                    float effectiveness = CalculateUnitEffectiveness(squad.TargetSquad);

                    // Draw effectiveness indicator with gradient ring
                    DrawEffectivenessRing(position, effectiveness, unitType);

                    // Draw detailed effectiveness breakdown
                    var screenPos = Camera.main.WorldToScreenPoint(position + Vector3.up * 4f);
                    if (screenPos.z > 0)
                    {
                        Handles.BeginGUI();
                        GUI.color = Color.white;
                        
                        float rangeEff = GetRangeEffectiveness(squad.TargetSquad);
                        float terrainEff = GetTerrainEffectiveness(squad.TargetSquad);
                        float heightEff = CalculateHeightAdvantage(position, GetSquadPosition(squad.TargetSquad));
                        float supportEff = CalculateSupportBonus(squad.TargetSquad);

                        Rect bgRect = new Rect(screenPos.x - 60, Screen.height - screenPos.y, 120, 90);
                        GUI.Box(bgRect, "");
                        
                        GUI.Label(new Rect(bgRect.x + 5, bgRect.y + 5, 110, 20),
                            $"{unitType}: {effectiveness:P0}");
                        GUI.Label(new Rect(bgRect.x + 5, bgRect.y + 25, 110, 20),
                            $"Range: {rangeEff:P0}");
                        GUI.Label(new Rect(bgRect.x + 5, bgRect.y + 45, 110, 20),
                            $"Terrain: {terrainEff:P0}");
                        GUI.Label(new Rect(bgRect.x + 5, bgRect.y + 65, 110, 20),
                            $"Height+Sup: {(heightEff + supportEff):P0}");
                        
                        Handles.EndGUI();
                    }
                }
            }
            
            // Draw debug paths
            if (stressTestViz.ShowDebugPaths)
            {
                DrawPathDebugVisuals();
            }
            
            // Draw efficiency dashboard
            if (stressTestViz.ShowEfficiencyDashboard)
            {
                DrawEfficiencyDashboard();
            }
        }

        private float CalculateUnitEffectiveness(int squadId)
        {
            var squad = squadDataMap[squadId];
            var unitType = GetSquadUnitType(squadId);
            float effectiveness = 1f;

            // Position effectiveness
            Vector3 pos = GetSquadPosition(squadId);
            float distanceToTarget = Vector3.Distance(pos, GetSquadPosition(squad.TargetSquad));
            float optimalRange = GetOptimalRange(unitType);
            float rangeEffectiveness = 1f - Mathf.Clamp01(Mathf.Abs(distanceToTarget - optimalRange) / optimalRange);
            effectiveness *= rangeEffectiveness;

            // Terrain effectiveness
            float terrainDensity = CalculateTerrainDensity(pos, 20f);
            float terrainEffectiveness = 1f;
            switch (unitType)
            {
                case UnitType.Artillery:
                    terrainEffectiveness = 1f - terrainDensity * 0.8f;
                    break;
                case UnitType.Scout:
                    terrainEffectiveness = 1f - terrainDensity * 0.4f;
                    break;
                case UnitType.Infantry:
                    terrainEffectiveness = terrainDensity * 0.6f + 0.4f; // Infantry benefits from cover
                    break;
                case UnitType.HeavyDefense:
                    terrainEffectiveness = Mathf.Lerp(0.7f, 1f, terrainDensity); // Heavy units slightly benefit from cover
                    break;
                case UnitType.Support:
                    terrainEffectiveness = 1f - terrainDensity * 0.3f;
                    break;
            }
            effectiveness *= terrainEffectiveness;

            // Height advantage
            float heightAdvantage = CalculateHeightAdvantage(pos, GetSquadPosition(squad.TargetSquad));
            float heightEffectiveness = 1f + (heightAdvantage * 0.3f); // Up to 30% bonus for height
            effectiveness *= heightEffectiveness;

            // Support bonus
            float supportBonus = CalculateSupportBonus(squadId);
            effectiveness *= (1f + supportBonus);

            return Mathf.Clamp01(effectiveness);
        }

        private float CalculateHeightAdvantage(Vector3 pos, Vector3 targetPos)
        {
            float heightDiff = pos.y - targetPos.y;
            return Mathf.Clamp01(heightDiff / 10f); // Normalize height difference
        }

        private float CalculateSupportBonus(int squadId)
        {
            float bonus = 0f;
            var nearbySquads = GetNearbySquads(GetSquadPosition(squadId), 15f);
            
            foreach (var nearbySquad in nearbySquads)
            {
                if (nearbySquad == squadId) continue;
                
                var nearbyType = GetSquadUnitType(nearbySquad);
                if (nearbyType == UnitType.Support)
                {
                    bonus += 0.15f; // 15% bonus per support unit
                }
            }
            
            return Mathf.Clamp(bonus, 0f, 0.45f); // Cap at 45% bonus
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Original visualization code...
            {{ ... }}

            // Draw stress test visualizations
            DrawStressTestVisuals();
            
            // Update stress test visualization
            UpdateStressTestVisualization();
        }

        private void Update()
        {
            UpdateTacticalAnalysis();
            RunStressTest();
        }

        private void UpdateTacticalAnalysis()
        {
            foreach (var kvp in squadDataMap)
            {
                var squadId = kvp.Key;
                var data = kvp.Value;
                
                if (!squadAnalysis.ContainsKey(squadId))
                    squadAnalysis[squadId] = new TacticalAnalysis();
                
                var analysis = squadAnalysis[squadId];

                // Calculate cover and evaluate position
                if (data.CurrentPath != null && data.CurrentPath.Count > 1)
                {
                    float totalCover = 0f;
                    float bestCoverScore = 0f;
                    Vector3 bestPosition = data.TargetPosition;

                    for (int i = 0; i < data.CurrentPath.Count - 1; i++)
                    {
                        var visibility = VisibilityInfo.Calculate(
                            data.CurrentPath[i],
                            data.CurrentPath[i + 1],
                            terrainLayer,
                            coverLayer,
                            pathNodeSpacing
                        );
                        totalCover += visibility.CoverScore;

                        // Check for better positions
                        var surroundingPositions = GetSurroundingPositions(data.CurrentPath[i], 5f);
                        foreach (var pos in surroundingPositions)
                        {
                            var coverScore = EvaluatePositionCover(pos, GetSquadPosition(data.TargetSquad));
                            if (coverScore > bestCoverScore)
                            {
                                bestCoverScore = coverScore;
                                bestPosition = pos;
                            }
                        }
                    }
                    
                    analysis.CoverScore = totalCover / (data.CurrentPath.Count - 1);
                    
                    // Provide feedback on cover utilization
                    if (analysis.CoverScore < 0.3f)
                    {
                        analysis.Feedback.Add("Low cover! Consider repositioning");
                        analysis.IsPositionOptimal = false;
                        analysis.SuggestedPosition = bestPosition;
                    }
                    else if (analysis.CoverScore > 0.7f)
                    {
                        analysis.Feedback.Add("Good cover position");
                        analysis.IsPositionOptimal = true;
                    }
                }

                // Calculate weighted threat based on unit types
                float threat = 0f;
                var myPos = GetSquadPosition(squadId);
                var nearbyThreats = new List<(int squadId, float weight)>();

                foreach (var otherSquad in squadDataMap)
                {
                    if (otherSquad.Key == squadId) continue;
                    
                    var otherPos = GetSquadPosition(otherSquad.Key);
                    var distance = Vector3.Distance(myPos, otherPos);
                    
                    if (distance < 50f)
                    {
                        var visibility = VisibilityInfo.Calculate(
                            myPos, otherPos, terrainLayer, coverLayer, distance);
                        
                        if (visibility.HasLineOfSight)
                        {
                            var unitType = GetSquadUnitType(otherSquad.Key);
                            var threatWeight = unitThreatValues[unitType];
                            var squadThreat = threatWeight * (1f - visibility.CoverScore) * (1f - distance/50f);
                            
                            threat += squadThreat;
                            nearbyThreats.Add((otherSquad.Key, squadThreat));
                        }
                    }
                }
                
                analysis.ThreatLevel = Mathf.Clamp01(threat);

                // Add threat-specific feedback
                if (nearbyThreats.Count > 0)
                {
                    var highestThreat = nearbyThreats.OrderByDescending(t => 
                        t.weight).First();
                    var threatType = GetSquadUnitType(highestThreat.squadId);
                    analysis.Feedback.Add($"Threat: {threatType} ({highestThreat.weight:F2})");
                }

                // Tactical position analysis
                switch (data.CoordinationType)
                {
                    case CoordinationType.Flanking:
                        var enemyPos = GetSquadPosition(data.TargetSquad);
                        var allyPos = GetSquadPosition(data.CoordinatingWith);
                        var flankAngle = Vector3.Angle(
                            enemyPos - allyPos,
                            data.TargetPosition - enemyPos
                        );
                        
                        analysis.CurrentDecision = $"Flanking ({flankAngle:F0}°)";
                        
                        if (flankAngle < 45f)
                        {
                            analysis.Feedback.Add("Suboptimal flank angle");
                            analysis.IsPositionOptimal = false;
                        }
                        else if (flankAngle > 75f)
                        {
                            analysis.Feedback.Add("Optimal flank position");
                            analysis.IsPositionOptimal = true;
                        }
                        break;

                    case CoordinationType.Support:
                        var supportDist = Vector3.Distance(data.TargetPosition, allyPos);
                        analysis.CurrentDecision = "Supporting";
                        
                        if (supportDist > 30f)
                        {
                            analysis.Feedback.Add("Too far from ally");
                            analysis.IsPositionOptimal = false;
                        }
                        else if (supportDist < 10f)
                        {
                            analysis.Feedback.Add("Too close to ally");
                            analysis.IsPositionOptimal = false;
                        }
                        else
                        {
                            analysis.Feedback.Add("Good support distance");
                            analysis.IsPositionOptimal = true;
                        }
                        break;
                }
            }
        }

        private Vector3[] GetSurroundingPositions(Vector3 center, float radius)
        {
            var positions = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                positions[i] = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
            }
            return positions;
        }

        private float EvaluatePositionCover(Vector3 position, Vector3 target)
        {
            var visibility = VisibilityInfo.Calculate(
                position, target, terrainLayer, coverLayer, 50f);
            return visibility.CoverScore;
        }

        private void RunStressTest()
        {
            if (!stressTestSettings.EnableStressTest) return;

            if (Time.time - lastStressUpdate < stressTestSettings.UpdateInterval) return;
            lastStressUpdate = Time.time;

            float startTime = Time.realtimeSinceStartup;

            // Force tactical analysis update for all squads
            foreach (var squad in squadDataMap.Values)
            {
                var analysis = squadAnalysis[squad.TargetSquad];
                analysis.Feedback.Clear();

                // Calculate dynamic threat for each nearby squad
                var nearbySquads = GetNearbySquads(GetSquadPosition(squad.TargetSquad), 50f);
                foreach (var nearbySquad in nearbySquads)
                {
                    float threatWeight = CalculateDynamicThreatWeight(nearbySquad, GetSquadPosition(nearbySquad));
                    analysis.Feedback.Add($"Squad {nearbySquad} Threat: {threatWeight:F2}");
                }
            }

            float updateTime = Time.realtimeSinceStartup - startTime;
            updateTimes.Add(updateTime);

            if (stressTestSettings.LogPerformanceMetrics && updateTimes.Count >= 100)
            {
                float averageTime = updateTimes.Average() * 1000f;
                float maxTime = updateTimes.Max() * 1000f;
                Debug.Log($"Stress Test Metrics:\n" +
                         $"Average Update Time: {averageTime:F2}ms\n" +
                         $"Max Update Time: {maxTime:F2}ms\n" +
                         $"Active Squads: {squadDataMap.Count}");
                updateTimes.Clear();
            }
        }

        private List<int> GetNearbySquads(Vector3 position, float radius)
        {
            return squadDataMap.Keys
                .Where(id => Vector3.Distance(GetSquadPosition(id), position) <= radius)
                .ToList();
        }

        private class AmbushCoordination
        {
            public bool IsCoordinating;           // Whether squad is in coordinated ambush
            public HashSet<int> AmbushSquads;     // Squads participating in ambush
            public Vector3 AmbushCenter;          // Center point of ambush
            public float TriggerRadius;           // Distance to trigger ambush
            public bool HoldFire;                 // Whether to hold fire until triggered
            public float LastUpdateTime;          // Time of last coordination update
        }

        private class ActiveAbility
        {
            public string Name;                   // Ability name
            public float Duration;                // How long ability lasts
            public float Cooldown;                // Time between uses
            public float LastUseTime;             // When ability was last used
            public bool IsActive;                 // Whether ability is currently active
            public Dictionary<string, float> Modifiers; // Effect modifiers
        }

        private Dictionary<int, AmbushCoordination> ambushCoordination = new Dictionary<int, AmbushCoordination>();
        private Dictionary<int, Dictionary<string, ActiveAbility>> unitAbilities = new Dictionary<int, Dictionary<string, ActiveAbility>>();

        private void UpdateAmbushCoordination()
        {
            foreach (var squad in squadDataMap.Values)
            {
                if (!ambushCoordination.ContainsKey(squad.SquadId))
                    continue;

                var coord = ambushCoordination[squad.SquadId];
                if (!coord.IsCoordinating) continue;

                // Check if enemies are within trigger radius
                bool shouldTrigger = CheckAmbushTrigger(coord);
                if (shouldTrigger)
                {
                    TriggerCoordinatedAmbush(squad.SquadId);
                }
                else
                {
                    UpdateAmbushPositioning(squad.SquadId);
                }
            }
        }

        private bool CheckAmbushTrigger(AmbushCoordination coord)
        {
            foreach (var enemySquad in GetEnemySquads())
            {
                var enemyPos = GetSquadCenter(enemySquad.SquadId);
                float distance = Vector3.Distance(enemyPos, coord.AmbushCenter);

                if (distance <= coord.TriggerRadius)
                    return true;
            }
            return false;
        }

        private void TriggerCoordinatedAmbush(int initiatorSquadId)
        {
            var coord = ambushCoordination[initiatorSquadId];
            
            // Signal all participating squads
            foreach (int squadId in coord.AmbushSquads)
            {
                // Switch to assault formation
                SetSquadFormation(squadId, FormationType.Assault);
                
                // Disable hold fire
                var squad = squadDataMap[squadId];
                foreach (var unit in GetSquadUnits(squad.SquadId))
                {
                    SetUnitHoldFire(unit.Id, false);
                }

                // Use any available combat abilities
                TriggerAmbushAbilities(squadId);
            }

            // Clear coordination state
            coord.IsCoordinating = false;
            coord.AmbushSquads.Clear();
        }

        private void TriggerAmbushAbilities(int squadId)
        {
            var squad = squadDataMap[squadId];
            foreach (var unit in GetSquadUnits(squad.SquadId))
            {
                if (!unitAbilities.ContainsKey(unit.Id))
                    continue;

                foreach (var ability in unitAbilities[unit.Id].Values)
                {
                    if (CanUseAbility(ability))
                    {
                        ActivateAbility(unit.Id, ability.Name);
                    }
                }
            }
        }

        private void InitiateCoordinatedAmbush(int squadId, Vector3 ambushPoint)
        {
            // Create new coordination
            var coord = new AmbushCoordination
            {
                IsCoordinating = true,
                AmbushSquads = new HashSet<int> { squadId },
                AmbushCenter = ambushPoint,
                TriggerRadius = 20f,
                HoldFire = true,
                LastUpdateTime = Time.time
            };

            ambushCoordination[squadId] = coord;

            // Find nearby friendly squads to join
            float recruitRadius = 50f;
            foreach (var otherSquad in squadDataMap.Values)
            {
                if (otherSquad.SquadId == squadId)
                    continue;

                var distance = Vector3.Distance(
                    GetSquadCenter(otherSquad.SquadId),
                    ambushPoint
                );

                if (distance <= recruitRadius)
                {
                    coord.AmbushSquads.Add(otherSquad.SquadId);
                }
            }

            // Set initial positions
            UpdateAmbushPositioning(squadId);
        }

        private void UpdateAmbushPositioning(int squadId)
        {
            var coord = ambushCoordination[squadId];
            int squadCount = coord.AmbushSquads.Count;
            float radius = 15f;

            // Distribute squads in a circle around ambush point
            int index = 0;
            foreach (int participantId in coord.AmbushSquads)
            {
                float angle = (index / (float)squadCount) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                Vector3 position = coord.AmbushCenter + offset;
                SetSquadDestination(participantId, position);
                SetSquadFormation(participantId, FormationType.Ambush);

                // Enable hold fire
                var squad = squadDataMap[participantId];
                foreach (var unit in GetSquadUnits(squad.SquadId))
                {
                    SetUnitHoldFire(unit.Id, coord.HoldFire);
                }

                index++;
            }
        }

        private void InitializeUnitAbilities(UnitData unit)
        {
            if (unitAbilities.ContainsKey(unit.Id))
                return;

            unitAbilities[unit.Id] = new Dictionary<string, ActiveAbility>();

            // Add stealth-related abilities based on unit type
            switch (unit.Type)
            {
                case UnitType.Scout:
                    AddAbility(unit.Id, "Cloak", 10f, 30f);
                    AddAbility(unit.Id, "SpeedBoost", 5f, 20f);
                    break;

                case UnitType.Infiltrator:
                    AddAbility(unit.Id, "Camouflage", 15f, 45f);
                    AddAbility(unit.Id, "SilentMove", 8f, 25f);
                    break;

                case UnitType.Special:
                    AddAbility(unit.Id, "StealthField", 12f, 40f);
                    AddAbility(unit.Id, "Decoy", 6f, 30f);
                    break;
            }
        }

        private void AddAbility(int unitId, string name, float duration, float cooldown)
        {
            unitAbilities[unitId][name] = new ActiveAbility
            {
                Name = name,
                Duration = duration,
                Cooldown = cooldown,
                LastUseTime = -cooldown,
                IsActive = false,
                Modifiers = GetAbilityModifiers(name)
            };
        }

        private Dictionary<string, float> GetAbilityModifiers(string abilityName)
        {
            var modifiers = new Dictionary<string, float>();
            
            switch (abilityName)
            {
                case "Cloak":
                    modifiers["StealthBonus"] = 0.8f;
                    modifiers["SpeedPenalty"] = 0.3f;
                    break;

                case "SpeedBoost":
                    modifiers["SpeedBonus"] = 0.5f;
                    modifiers["StealthPenalty"] = 0.2f;
                    break;

                case "Camouflage":
                    modifiers["StealthBonus"] = 0.6f;
                    modifiers["CoverBonus"] = 0.4f;
                    break;

                case "SilentMove":
                    modifiers["MovementPenalty"] = 0f;
                    modifiers["SpeedPenalty"] = 0.4f;
                    break;

                case "StealthField":
                    modifiers["StealthBonus"] = 0.5f;
                    modifiers["RadiusBonus"] = 10f;
                    break;

                case "Decoy":
                    modifiers["DetectionThreshold"] = 0.3f;
                    break;
            }

            return modifiers;
        }

        private bool CanUseAbility(ActiveAbility ability)
        {
            return !ability.IsActive && 
                   Time.time >= ability.LastUseTime + ability.Cooldown;
        }

        private void ActivateAbility(int unitId, string abilityName)
        {
            if (!unitAbilities.ContainsKey(unitId) ||
                !unitAbilities[unitId].ContainsKey(abilityName))
                return;

            var ability = unitAbilities[unitId][abilityName];
            if (!CanUseAbility(ability))
                return;

            ability.IsActive = true;
            ability.LastUseTime = Time.time;

            // Apply ability effects
            ApplyAbilityEffects(unitId, ability, true);

            // Schedule deactivation
            StartCoroutine(DeactivateAbilityAfterDuration(unitId, abilityName));
        }

        private void ApplyAbilityEffects(int unitId, ActiveAbility ability, bool enable)
        {
            if (!unitStealthCache.ContainsKey(unitId))
                return;

            var stealth = unitStealthCache[unitId];
            float multiplier = enable ? 1f : -1f;

            foreach (var modifier in ability.Modifiers)
            {
                switch (modifier.Key)
                {
                    case "StealthBonus":
                        stealth.BaseStealthValue += modifier.Value * multiplier;
                        break;

                    case "SpeedPenalty":
                        // Apply to unit movement system
                        ModifyUnitSpeed(unitId, -modifier.Value * multiplier);
                        break;

                    case "SpeedBonus":
                        ModifyUnitSpeed(unitId, modifier.Value * multiplier);
                        break;

                    case "CoverBonus":
                        // Enhance cover effectiveness
                        ModifyUnitCoverBonus(unitId, modifier.Value * multiplier);
                        break;

                    case "MovementPenalty":
                        stealth.MovementPenalty = modifier.Value;
                        break;

                    case "DetectionThreshold":
                        stealth.DetectionThreshold = modifier.Value;
                        break;
                }
            }
        }

        private IEnumerator DeactivateAbilityAfterDuration(int unitId, string abilityName)
        {
            if (!unitAbilities.ContainsKey(unitId) ||
                !unitAbilities[unitId].ContainsKey(abilityName))
                yield break;

            var ability = unitAbilities[unitId][abilityName];
            yield return new WaitForSeconds(ability.Duration);

            // Deactivate ability
            ability.IsActive = false;
            ApplyAbilityEffects(unitId, ability, false);
        }

        [System.Serializable]
        private class TacticalVisualization
        {
            public bool ShowSoundDetection = true;
            public bool ShowThreatMemory = true;
            public bool ShowSquadRoles = true;
            public bool ShowAlertness = true;
            
            public Color ScoutColor = new Color(0.2f, 0.8f, 0.2f, 0.6f);
            public Color AssaultColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);
            public Color SupportColor = new Color(0.2f, 0.2f, 0.8f, 0.6f);
            public Color ArtilleryColor = new Color(0.8f, 0.8f, 0.2f, 0.6f);
            
            public float RoleIconSize = 2f;
            public float AlertnessIconSize = 3f;
            public float SoundWaveSpacing = 2f;
        }

        [SerializeField] private TacticalVisualization tacticalViz;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            if (tacticalViz.ShowSquadRoles)
                DrawSquadRoles();
            
            if (tacticalViz.ShowSoundDetection)
                DrawSoundDetection();
            
            if (tacticalViz.ShowThreatMemory)
                DrawThreatMemory();
            
            if (tacticalViz.ShowAlertness)
                DrawSquadAlertness();
        }

        private void DrawSquadRoles()
        {
            foreach (var roleEntry in squadRoles)
            {
                if (!squadDataMap.ContainsKey(roleEntry.Key)) continue;

                var squad = squadDataMap[roleEntry.Key].TargetSquad;
                Vector3 position = GetSquadPosition(squad);
                Color roleColor = GetRoleColor(roleEntry.Value.Type);
                
                // Draw role indicator
                Gizmos.color = roleColor;
                float size = tacticalViz.RoleIconSize;
                DrawRoleIcon(position, roleEntry.Value.Type, size);
                
                // Draw specialization bonus indicator
                if (roleEntry.Value.SpecializationBonus > 1f)
                {
                    DrawSpecializationRing(position, roleEntry.Value.SpecializationBonus, roleColor);
                }
            }
        }

        private void DrawRoleIcon(Vector3 position, SquadRole.RoleType role, float size)
        {
            switch (role)
            {
                case SquadRole.RoleType.Scout:
                    // Eye symbol
                    DrawEyeSymbol(position, size);
                    break;
                case SquadRole.RoleType.Assault:
                    // Sword symbol
                    DrawSwordSymbol(position, size);
                    break;
                case SquadRole.RoleType.Support:
                    // Cross symbol
                    DrawCrossSymbol(position, size);
                    break;
                case SquadRole.RoleType.Artillery:
                    // Target symbol
                    DrawTargetSymbol(position, size);
                    break;
            }
        }

        private void DrawEyeSymbol(Vector3 position, float size)
        {
            Vector3 up = Vector3.up * size * 0.5f;
            Vector3 right = Vector3.right * size;
            
            // Draw eye outline
            Gizmos.DrawWireSphere(position, size * 0.3f);
            // Draw eyebrow
            Gizmos.DrawLine(position + up - right * 0.5f, position + up + right * 0.5f);
        }

        private void DrawSwordSymbol(Vector3 position, float size)
        {
            Vector3 up = Vector3.up * size;
            Vector3 right = Vector3.right * size * 0.3f;
            
            // Draw blade
            Gizmos.DrawLine(position, position + up);
            // Draw crossguard
            Gizmos.DrawLine(position + up * 0.7f - right, position + up * 0.7f + right);
            // Draw pommel
            Gizmos.DrawWireSphere(position, size * 0.1f);
        }

        private void DrawCrossSymbol(Vector3 position, float size)
        {
            Vector3 up = Vector3.up * size;
            Vector3 right = Vector3.right * size;
            
            Gizmos.DrawLine(position - up * 0.5f, position + up * 0.5f);
            Gizmos.DrawLine(position - right * 0.5f, position + right * 0.5f);
        }

        private void DrawTargetSymbol(Vector3 position, float size)
        {
            // Draw concentric circles
            Gizmos.DrawWireSphere(position, size * 0.3f);
            Gizmos.DrawWireSphere(position, size * 0.6f);
            
            // Draw crosshairs
            Vector3 up = Vector3.up * size;
            Vector3 right = Vector3.right * size;
            Gizmos.DrawLine(position - up * 0.5f, position + up * 0.5f);
            Gizmos.DrawLine(position - right * 0.5f, position + right * 0.5f);
        }

        private void DrawSpecializationRing(Vector3 position, float bonus, Color baseColor)
        {
            float size = tacticalViz.RoleIconSize * 1.5f;
            Color ringColor = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.3f);
            Gizmos.color = ringColor;
            
            // Draw specialization ring
            Gizmos.DrawWireSphere(position, size);
            
            // Draw bonus indicator segments
            int segments = Mathf.RoundToInt((bonus - 1f) * 10f);
            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Gizmos.DrawLine(position + dir * size, position + dir * (size * 1.2f));
            }
        }

        private void DrawSoundDetection()
        {
            foreach (var entry in squadDataMap)
            {
                if (!squadAwareness.ContainsKey(entry.Key)) continue;

                Vector3 position = GetSquadPosition(entry.Value.TargetSquad);
                float range = CalculateHearingRange(entry.Key);
                
                // Draw hearing range
                Gizmos.color = new Color(0.2f, 0.8f, 0.8f, 0.2f);
                DrawHearingRange(position, range);
                
                // Draw sound waves for active sounds
                if (IsInCombat(entry.Value.TargetSquad) || GetSquadSpeed(entry.Value.TargetSquad) > 5f)
                {
                    DrawSoundWaves(position, CalculateSoundIntensity(entry.Value.TargetSquad));
                }
            }
        }

        private void DrawHearingRange(Vector3 position, float range)
        {
            // Draw main hearing circle
            Gizmos.DrawWireSphere(position, range);
            
            // Draw directional indicators
            int directions = 8;
            float angleStep = 360f / directions;
            float innerRadius = range * 0.8f;
            
            for (int i = 0; i < directions; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Gizmos.DrawLine(position + dir * innerRadius, position + dir * range);
            }
        }

        private void DrawSoundWaves(Vector3 position, float intensity)
        {
            Color waveColor = new Color(1f, 0.6f, 0f, 0.3f);
            Gizmos.color = waveColor;
            
            int waves = 3;
            float spacing = tacticalViz.SoundWaveSpacing;
            
            for (int i = 0; i < waves; i++)
            {
                float radius = (i + 1) * spacing * intensity;
                Gizmos.DrawWireSphere(position, radius);
            }
        }

        private void DrawThreatMemory()
        {
            foreach (var entry in squadAwareness)
            {
                if (!squadDataMap.ContainsKey(entry.Key)) continue;

                Vector3 squadPos = GetSquadPosition(squadDataMap[entry.Key].TargetSquad);
                
                foreach (var threat in entry.Value.ThreatMemory)
                {
                    // Draw threat indicator
                    Color threatColor = new Color(1f, 0f, 0f, threat.Value);
                    Gizmos.color = threatColor;
                    
                    DrawThreatMarker(threat.Key, threat.Value);
                    
                    // Draw connection line
                    Gizmos.DrawLine(squadPos, threat.Key);
                }
            }
        }

        private void DrawThreatMarker(Vector3 position, float intensity)
        {
            float size = tacticalViz.RoleIconSize;
            
            // Draw warning triangle
            Vector3 top = position + Vector3.up * size;
            Vector3 bottomLeft = position + Vector3.left * size * 0.5f;
            Vector3 bottomRight = position + Vector3.right * size * 0.5f;
            
            Gizmos.DrawLine(top, bottomLeft);
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, top);
        }

        private void DrawSquadAlertness()
        {
            foreach (var entry in squadAwareness)
            {
                if (!squadDataMap.ContainsKey(entry.Key)) continue;

                Vector3 position = GetSquadPosition(squadDataMap[entry.Key].TargetSquad);
                float alertness = entry.Value.AlertnessLevel;
                
                // Draw alertness indicator
                Color alertColor = Color.Lerp(Color.green, Color.red, alertness);
                alertColor.a = 0.6f;
                Gizmos.color = alertColor;
                
                DrawAlertnessIndicator(position, alertness);
            }
        }

        private void DrawAlertnessIndicator(Vector3 position, float alertness)
        {
            float size = tacticalViz.AlertnessIconSize * (0.5f + alertness * 0.5f);
            Vector3 offset = Vector3.up * 3f;
            position += offset;
            
            if (alertness > 0.7f)
            {
                // High alertness - exclamation mark
                DrawExclamationMark(position, size);
            }
            else if (alertness > 0.3f)
            {
                // Medium alertness - dot
                DrawAlertnessDot(position, size);
            }
            else
            {
                // Low alertness - horizontal line
                DrawAlertnessLine(position, size);
            }
        }

        private void DrawExclamationMark(Vector3 position, float size)
        {
            Vector3 top = position + Vector3.up * size * 0.5f;
            Vector3 bottom = position - Vector3.up * size * 0.5f;
            
            Gizmos.DrawLine(top, bottom + Vector3.up * size * 0.2f);
            Gizmos.DrawWireSphere(bottom, size * 0.1f);
        }

        private void DrawAlertnessDot(Vector3 position, float size)
        {
            Gizmos.DrawWireSphere(position, size * 0.2f);
        }

        private void DrawAlertnessLine(Vector3 position, float size)
        {
            Vector3 left = position - Vector3.right * size * 0.3f;
            Vector3 right = position + Vector3.right * size * 0.3f;
            Gizmos.DrawLine(left, right);
        }

        // ... rest of the code remains the same ...

        [System.Serializable]
        public class UnitStatusVisualization
        {
            public bool ShowHealthBars = true;
            public bool ShowAmmoStatus = true;
            public bool ShowAbilityStatus = true;
            public bool ShowMovementPaths = true;
            
            public float HealthBarWidth = 2f;
            public float HealthBarHeight = 0.2f;
            public float StatusIconSize = 0.5f;
            public float PathLineWidth = 2f;
            
            public Color HealthyColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            public Color DamagedColor = new Color(0.8f, 0.8f, 0.2f, 0.8f);
            public Color CriticalColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            public Color AmmoLowColor = new Color(1f, 0.6f, 0f, 0.8f);
            public Color PathColor = new Color(0.2f, 0.6f, 1f, 0.4f);
        }

        [SerializeField] private UnitStatusVisualization statusViz;

        private void DrawUnitStatus(GameObject unit, Vector3 position)
        {
            if (!Application.isPlaying || unit == null) return;

            float yOffset = 2f;  // Base height above unit
            
            // Draw health bar
            if (statusViz.ShowHealthBars)
            {
                DrawHealthBar(unit, position + Vector3.up * yOffset);
                yOffset += statusViz.HealthBarHeight * 2f;
            }
            
            // Draw ammo status
            if (statusViz.ShowAmmoStatus)
            {
                DrawAmmoStatus(unit, position + Vector3.up * yOffset);
                yOffset += statusViz.StatusIconSize * 1.5f;
            }
            
            // Draw active abilities
            if (statusViz.ShowAbilityStatus)
            {
                DrawAbilityStatus(unit, position + Vector3.up * yOffset);
            }
            
            // Draw movement path
            if (statusViz.ShowMovementPaths)
            {
                DrawMovementPath(unit);
            }
        }

        private void DrawHealthBar(GameObject unit, Vector3 position)
        {
            float health = GetUnitHealth(unit);
            float maxHealth = GetUnitMaxHealth(unit);
            float healthPercent = health / maxHealth;
            
            // Background bar
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            DrawBar(position, statusViz.HealthBarWidth, statusViz.HealthBarHeight);
            
            // Health bar with color gradient based on health percentage
            Color healthColor = Color.Lerp(
                Color.Lerp(statusViz.CriticalColor, statusViz.DamagedColor, healthPercent * 2f),
                Color.Lerp(statusViz.DamagedColor, statusViz.HealthyColor, (healthPercent - 0.5f) * 2f),
                healthPercent
            );
            
            Gizmos.color = healthColor;
            DrawBar(position, statusViz.HealthBarWidth * healthPercent, statusViz.HealthBarHeight);
            
            // Damage effect when health is low
            if (healthPercent < 0.3f)
            {
                float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
                Color warningColor = new Color(1f, 0f, 0f, pulse * 0.5f);
                Gizmos.color = warningColor;
                DrawBar(position, statusViz.HealthBarWidth, statusViz.HealthBarHeight * 1.2f);
            }
        }

        private void DrawBar(Vector3 position, float width, float height)
        {
            Vector3 size = new Vector3(width, height, 0.1f);
            Vector3 center = position + Vector3.right * (width * 0.5f - statusViz.HealthBarWidth * 0.5f);
            Gizmos.DrawCube(center, size);
        }

        private void DrawAmmoStatus(GameObject unit, Vector3 position)
        {
            float ammo = GetUnitAmmo(unit);
            float maxAmmo = GetUnitMaxAmmo(unit);
            float ammoPercent = ammo / maxAmmo;
            
            // Draw ammo icon
            Gizmos.color = ammoPercent < 0.3f ? statusViz.AmmoLowColor : Color.white;
            DrawAmmoIcon(position, statusViz.StatusIconSize);
            
            // Draw ammo count
            if (ammoPercent < 0.3f)
            {
                float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                Gizmos.color = new Color(statusViz.AmmoLowColor.r, statusViz.AmmoLowColor.g, statusViz.AmmoLowColor.b, pulse);
                DrawAmmoWarning(position, statusViz.StatusIconSize * 1.2f);
            }
        }

        private void DrawAmmoIcon(Vector3 position, float size)
        {
            Vector3 right = Vector3.right * size * 0.3f;
            Vector3 up = Vector3.up * size * 0.5f;
            
            // Draw bullet shape
            Gizmos.DrawLine(position - right, position + right);
            Gizmos.DrawLine(position + right, position + right + up);
            Gizmos.DrawLine(position + right + up, position - right + up);
            Gizmos.DrawLine(position - right + up, position - right);
        }

        private void DrawAmmoWarning(Vector3 position, float size)
        {
            DrawWarningTriangle(position, size);
        }

        private void DrawAbilityStatus(GameObject unit, Vector3 position)
        {
            if (!unitAbilities.ContainsKey(GetUnitId(unit))) return;
            
            float spacing = statusViz.StatusIconSize * 1.2f;
            float xOffset = -((unitAbilities[GetUnitId(unit)].Count - 1) * spacing * 0.5f);
            
            foreach (var ability in unitAbilities[GetUnitId(unit)].Values)
            {
                Vector3 abilityPos = position + Vector3.right * xOffset;
                
                // Draw ability icon
                if (ability.IsActive)
                {
                    // Active ability effect
                    float remainingTime = ability.Duration - (Time.time - ability.LastUseTime);
                    float progress = remainingTime / ability.Duration;
                    DrawAbilityIcon(abilityPos, statusViz.StatusIconSize, ability.Type, progress);
                }
                else if (CanUseAbility(ability))
                {
                    // Ready ability
                    DrawAbilityIcon(abilityPos, statusViz.StatusIconSize, ability.Type, 1f);
                }
                else
                {
                    // Cooldown ability
                    float cooldownProgress = (Time.time - ability.LastUseTime) / ability.Cooldown;
                    DrawAbilityCooldown(abilityPos, statusViz.StatusIconSize, cooldownProgress);
                }
                
                xOffset += spacing;
            }
        }

        private void DrawAbilityIcon(Vector3 position, float size, AbilityType type, float progress)
        {
            // Base icon
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Gizmos.DrawWireSphere(position, size * 0.5f);
            
            // Progress indicator
            if (progress < 1f)
            {
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
                DrawProgressArc(position, size * 0.6f, progress);
            }
            
            // Ability-specific icon
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f);
            DrawAbilityTypeIcon(position, size * 0.3f, type);
        }

        private void DrawProgressArc(Vector3 position, float radius, float progress)
        {
            int segments = 32;
            float angleStep = 360f / segments;
            float startAngle = -90f;
            float endAngle = startAngle + 360f * progress;
            
            for (float angle = startAngle; angle < endAngle; angle += angleStep)
            {
                float rad1 = angle * Mathf.Deg2Rad;
                float rad2 = (angle + angleStep) * Mathf.Deg2Rad;
                
                Vector3 p1 = position + new Vector3(Mathf.Cos(rad1), Mathf.Sin(rad1)) * radius;
                Vector3 p2 = position + new Vector3(Mathf.Cos(rad2), Mathf.Sin(rad2)) * radius;
                
                Gizmos.DrawLine(p1, p2);
            }
        }

        private void DrawAbilityTypeIcon(Vector3 position, float size, AbilityType type)
        {
            switch (type)
            {
                case AbilityType.Stealth:
                    DrawStealthIcon(position, size);
                    break;
                case AbilityType.Scan:
                    DrawScanIcon(position, size);
                    break;
                case AbilityType.Ambush:
                    DrawAmbushIcon(position, size);
                    break;
                // Add more ability type icons as needed
            }
        }

        private void DrawMovementPath(GameObject unit)
        {
            if (!squadDataMap.ContainsKey(GetSquadId(unit))) return;
            
            var squad = squadDataMap[GetSquadId(unit)].TargetSquad;
            Vector3 currentPos = GetSquadPosition(squad);
            Vector3 targetPos = GetSquadDestination(squad);
            
            if (Vector3.Distance(currentPos, targetPos) > 1f)
            {
                // Draw path line
                Gizmos.color = statusViz.PathColor;
                DrawDashedLine(currentPos, targetPos, statusViz.PathLineWidth);
                
                // Draw direction arrow
                Vector3 direction = (targetPos - currentPos).normalized;
                Vector3 arrowPos = Vector3.Lerp(currentPos, targetPos, 0.8f);
                DrawDirectionArrow(arrowPos, direction, statusViz.PathLineWidth * 2f);
            }
        }

        private void DrawDashedLine(Vector3 start, Vector3 end, float width)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            float dashLength = 0.5f;
            float gapLength = 0.3f;
            float currentDist = 0f;
            
            while (currentDist < distance)
            {
                float remainingDist = distance - currentDist;
                float currentDashLength = Mathf.Min(dashLength, remainingDist);
                
                Vector3 dashStart = start + direction * currentDist;
                Vector3 dashEnd = dashStart + direction * currentDashLength;
                
                Gizmos.DrawLine(dashStart, dashEnd);
                
                currentDist += currentDashLength + gapLength;
            }
            
            // Draw arrow head
            Vector3 arrowBase = end - direction * statusViz.PathLineWidth;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            
            Gizmos.DrawLine(end, arrowBase + right * statusViz.PathLineWidth * 0.5f);
            Gizmos.DrawLine(end, arrowBase - right * statusViz.PathLineWidth * 0.5f);
        }

        private void DrawDirectionArrow(Vector3 position, Vector3 direction, float size)
        {
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 tip = position + direction * size;
            Vector3 basePos = position - direction * size * 0.5f;
            
            Gizmos.DrawLine(tip, basePos + right * size * 0.5f);
            Gizmos.DrawLine(tip, basePos - right * size * 0.5f);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw existing visualizations
            if (tacticalViz.ShowSquadRoles)
                DrawSquadRoles();
            if (tacticalViz.ShowSoundDetection)
                DrawSoundDetection();
            if (tacticalViz.ShowThreatMemory)
                DrawThreatMemory();
            if (tacticalViz.ShowAlertness)
                DrawSquadAlertness();
                
            // Draw unit status for all units
            foreach (var squadEntry in squadDataMap)
            {
                var squad = squadEntry.Value.TargetSquad;
                if (squad != null)
                {
                    Vector3 position = GetSquadPosition(squad);
                    DrawUnitStatus(squad, position);
                }
            }
        }

        [System.Serializable]
        public class AIVisualization
        {
            public bool ShowAIState = true;
            public bool ShowSquadSynergy = true;
            public bool ShowCoordination = true;
            public bool ShowTacticalLinks = true;
            
            public float StateIconSize = 1.5f;
            public float SynergyLineWidth = 2f;
            public float CoordinationArrowSize = 3f;
            
            public Color SupportRangeColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            public Color CoordinationColor = new Color(0.8f, 0.8f, 0.2f, 0.4f);
            public Color TacticalLinkColor = new Color(0.2f, 0.6f, 1f, 0.3f);
        }

        [SerializeField] private AIVisualization aiViz;

        private void DrawAIFeedback()
        {
            if (!Application.isPlaying) return;
            
            foreach (var squadEntry in squadDataMap)
            {
                var squad = squadEntry.Value.TargetSquad;
                if (squad == null || !IsAIControlled(squad)) continue;
                
                Vector3 position = GetSquadPosition(squad);
                
                if (aiViz.ShowAIState)
                    DrawAIState(squad, position);
                    
                if (aiViz.ShowSquadSynergy)
                    DrawSquadSynergy(squad);
                    
                if (aiViz.ShowCoordination)
                    DrawCoordinationLinks(squad);
                    
                if (aiViz.ShowTacticalLinks)
                    DrawTacticalLinks(squad);
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            // Draw existing visualizations
            if (tacticalViz.ShowSquadRoles)
                DrawSquadRoles();
            if (tacticalViz.ShowSoundDetection)
                DrawSoundDetection();
            if (tacticalViz.ShowThreatMemory)
                DrawThreatMemory();
            if (tacticalViz.ShowAlertness)
                DrawSquadAlertness();
            
            // Draw unit status
            foreach (var squadEntry in squadDataMap)
            {
                var squad = squadEntry.Value.TargetSquad;
                if (squad != null)
                {
                    Vector3 position = GetSquadPosition(squad);
                    DrawUnitStatus(squad, position);
                }
            }
            
            // Draw AI feedback
            DrawAIFeedback();
        }

        public enum AIState
        {
            Searching,
            Engaging,
            Supporting,
            Retreating
        }

        private void DrawAIState(GameObject squad, Vector3 position)
        {
            AIState state = GetSquadAIState(squad);
            Vector3 iconPos = position + Vector3.up * 4f;
            
            // Draw state icon
            switch (state)
            {
                case AIState.Searching:
                    DrawSearchingIcon(iconPos);
                    break;
                case AIState.Engaging:
                    DrawEngagingIcon(iconPos);
                    break;
                case AIState.Supporting:
                    DrawSupportingIcon(iconPos);
                    break;
                case AIState.Retreating:
                    DrawRetreatingIcon(iconPos);
                    break;
            }
        }

        private void DrawSearchingIcon(Vector3 position)
        {
            float size = aiViz.StateIconSize;
            float time = Time.time * 2f;
            
            // Draw rotating search pattern
            for (int i = 0; i < 4; i++)
            {
                float angle = time + i * Mathf.PI / 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * size * 0.5f, 0f, Mathf.Sin(angle) * size * 0.5f);

                Gizmos.color = new Color(0.2f, 0.8f, 0.8f, (Mathf.Sin(time + i) + 1f) * 0.3f);
                Gizmos.DrawWireSphere(position + offset, size * 0.2f);
            }
        }

        private void DrawEngagingIcon(Vector3 position)
        {
            float size = aiViz.StateIconSize;
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            
            // Draw combat indicator
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.4f + pulse * 0.4f);
            
            // Draw crossed swords
            Vector3 right = Vector3.right * size * 0.7f;
            Vector3 forward = Vector3.forward * size * 0.7f;
            
            Gizmos.DrawLine(position - right - forward, position + right + forward);
            Gizmos.DrawLine(position - right + forward, position + right - forward);
        }

        private void DrawSupportingIcon(Vector3 position)
        {
            float size = aiViz.StateIconSize;
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            
            // Draw support symbol
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.4f + pulse * 0.4f);
            
            // Draw plus symbol
            Gizmos.DrawLine(position + Vector3.up * size, position - Vector3.up * size);
            Gizmos.DrawLine(position + Vector3.right * size, position - Vector3.right * size);
            
            // Draw circle
            DrawCircle(position, size * 1.2f, 16);
        }

        private void DrawRetreatingIcon(Vector3 position)
        {
            float size = aiViz.StateIconSize;
            float time = Time.time * 4f;
            
            // Draw retreat arrows
            for (int i = 0; i < 3; i++)
            {
                float offset = (i - 1) * size * 0.4f;
                float alpha = Mathf.Clamp01(1f - ((time + i * 0.3f) % 1f));
                Gizmos.color = new Color(0.8f, 0.4f, 0.1f, alpha * 0.6f);
                
                Vector3 arrowPos = position + Vector3.right * offset;
                
                // Draw appearing retreat arrows
                Vector3 tip = arrowPos + Vector3.up * size;
                Vector3 basePos = arrowPos - Vector3.up * size;
                
                Gizmos.DrawLine(basePos, tip);
                
                float headSize = size * 0.3f;
                Gizmos.DrawLine(tip, tip - Vector3.up * headSize + Vector3.right * headSize);
                Gizmos.DrawLine(tip, tip - Vector3.up * headSize - Vector3.right * headSize);
            }
        }

        private void DrawSquadSynergy(GameObject squad)
        {
            Vector3 squadPos = GetSquadPosition(squad);
            float synergyRange = GetSquadSynergyRange(squad);
            
            // Draw synergy range
            Gizmos.color = aiViz.SupportRangeColor;
            DrawCircle(squadPos, synergyRange, 32);
            
            // Draw synergy links to nearby friendly squads
            var nearbySquads = GetNearbySquads(squadPos, synergyRange)
                .Where(s => !IsEnemySquad(s) && s != squad);
            
            foreach (var nearbySquad in nearbySquads)
            {
                Vector3 nearbyPos = GetSquadPosition(nearbySquad);
                float synergyStrength = CalculateSynergyStrength(squad, nearbySquad);
                
                // Draw synergy line with strength-based opacity
                Gizmos.color = new Color(
                    aiViz.SupportRangeColor.r,
                    aiViz.SupportRangeColor.g,
                    aiViz.SupportRangeColor.b,
                    synergyStrength * aiViz.SupportRangeColor.a
                );
                
                DrawDashedLine(squadPos + Vector3.up * 0.5f, 
                    nearbyPos + Vector3.up * 0.5f, 
                    aiViz.SynergyLineWidth);
            }
        }

        private void DrawCoordinationLinks(GameObject squad)
        {
            if (!squadAwareness.ContainsKey(GetSquadId(squad))) return;
            
            var awareness = squadAwareness[GetSquadId(squad)];
            Vector3 squadPos = GetSquadPosition(squad);
            
            // Draw coordination links with other squads
            foreach (var coordSquad in GetCoordinatingSquads(squad))
            {
                Vector3 coordPos = GetSquadPosition(coordSquad);
                CoordinationType coordType = GetCoordinationType(squad, coordSquad);
                
                // Draw coordination indicator
                DrawCoordinationLink(squadPos, coordPos, coordType);
            }
        }

        private void DrawCoordinationLink(Vector3 start, Vector3 end, CoordinationType type)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            Vector3 midPoint = (start + end) * 0.5f + Vector3.up * (distance * 0.2f);
            
            // Draw curved coordination line
            Gizmos.color = aiViz.CoordinationColor;
            DrawBezierLine(start, midPoint, end, 12);
            
            // Draw coordination type indicator at midpoint
            switch (type)
            {
                case CoordinationType.Support:
                    DrawSupportIndicator(midPoint);
                    break;
                case CoordinationType.Flanking:
                    DrawFlankingIndicator(midPoint);
                    break;
                case CoordinationType.Ambush:
                    DrawAmbushIndicator(midPoint);
                    break;
            }
        }

        private void DrawBezierLine(Vector3 start, Vector3 mid, Vector3 end, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float t1 = i / (float)segments;
                float t2 = (i + 1) / (float)segments;
                
                Vector3 p1 = CalculateBezierPoint(start, mid, end, t1);
                Vector3 p2 = CalculateBezierPoint(start, mid, end, t2);
                
                Gizmos.DrawLine(p1, p2);
            }
        }

        private Vector3 CalculateBezierPoint(Vector3 start, Vector3 mid, Vector3 end, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            
            return (uu * start) + (2f * u * t * mid) + (tt * end);
        }

        private void DrawSupportIndicator(Vector3 position)
        {
            float size = aiViz.CoordinationArrowSize * 0.5f;
            
            // Draw shield symbol
            Gizmos.DrawWireSphere(position, size);
            Gizmos.DrawLine(position + Vector3.up * size, 
                position + Vector3.up * size * 1.2f);
            Gizmos.DrawLine(position - Vector3.up * size, 
                position - Vector3.up * size * 1.2f);
        }

        private void DrawFlankingIndicator(Vector3 position)
        {
            float size = aiViz.CoordinationArrowSize * 0.5f;
            
            // Draw curved flanking arrows
            for (int i = 0; i < 2; i++)
            {
                float angle = i * Mathf.PI;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 start = position + dir * size;
                Vector3 end = position + Vector3.Cross(dir, Vector3.up) * size;
                
                DrawArrowCurve(start, end, 8);
            }
        }

        private void DrawAmbushIndicator(Vector3 position)
        {
            float size = aiViz.CoordinationArrowSize * 0.5f;
            
            // Draw exclamation mark
            Gizmos.DrawLine(position + Vector3.up * size,
                position - Vector3.up * size * 0.2f);
            Gizmos.DrawWireSphere(position - Vector3.up * size * 0.4f, size * 0.2f);
        }

        private void DrawArrowCurve(Vector3 start, Vector3 end, int segments)
        {
            Vector3 mid = (start + end) * 0.5f + Vector3.up * (Vector3.Distance(start, end) * 0.3f);
            
            for (int i = 0; i < segments; i++)
            {
                float t1 = i / (float)segments;
                float t2 = (i + 1) / (float)segments;
                
                Vector3 p1 = CalculateBezierPoint(start, mid, end, t1);
                Vector3 p2 = CalculateBezierPoint(start, mid, end, t2);
                
                Gizmos.DrawLine(p1, p2);
            }
            
            // Draw arrow head
            Vector3 dir = (end - CalculateBezierPoint(start, mid, end, 0.8f)).normalized;
            float headSize = aiViz.CoordinationArrowSize * 0.2f;
            
            Gizmos.DrawLine(end, end - dir * headSize + Vector3.Cross(dir, Vector3.up) * headSize);
            Gizmos.DrawLine(end, end - dir * headSize - Vector3.Cross(dir, Vector3.up) * headSize);
        }

        private void DrawTacticalLinks(GameObject squad)
        {
            Vector3 squadPos = GetSquadPosition(squad);
            
            // Draw links to tactical objectives
            foreach (var objective in GetTacticalObjectives(squad))
            {
                float priority = GetObjectivePriority(objective);
                Vector3 objPos = GetObjectivePosition(objective);
                
                // Draw tactical link with priority-based opacity
                Gizmos.color = new Color(
                    aiViz.TacticalLinkColor.r,
                    aiViz.TacticalLinkColor.g,
                    aiViz.TacticalLinkColor.b,
                    priority * aiViz.TacticalLinkColor.a
                );
                
                DrawTacticalArrow(squadPos, objPos, priority);
                DrawObjectiveMarker(objPos, priority);
            }
        }

        private void DrawTacticalArrow(Vector3 start, Vector3 end, float priority)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            
            // Draw dashed line with varying dash length based on priority
            float dashLength = 0.5f + priority * 0.5f;
            float gapLength = 0.3f;
            float currentDist = 0f;
            
            while (currentDist < distance)
            {
                float remainingDist = distance - currentDist;
                float currentDashLength = Mathf.Min(dashLength, remainingDist);
                
                Vector3 dashStart = start + direction * currentDist;
                Vector3 dashEnd = dashStart + direction * currentDashLength;
                
                Gizmos.DrawLine(dashStart, dashEnd);
                
                currentDist += currentDashLength + gapLength;
            }
            
            // Draw arrow head
            Vector3 arrowBase = end - direction * aiViz.CoordinationArrowSize;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            
            Gizmos.DrawLine(end, arrowBase + right * aiViz.CoordinationArrowSize * 0.5f);
            Gizmos.DrawLine(end, arrowBase - right * aiViz.CoordinationArrowSize * 0.5f);
        }

        private void DrawObjectiveMarker(Vector3 position, float priority)
        {
            float size = aiViz.CoordinationArrowSize * (0.8f + priority * 0.4f);
            float time = Time.time * 2f;
            float pulse = (Mathf.Sin(time) + 1f) * 0.5f;
            
            // Draw pulsing diamond marker
            Vector3[] points = new Vector3[]
            {
                position + Vector3.up * size,
                position + Vector3.right * size,
                position - Vector3.up * size,
                position - Vector3.right * size
            };
            
            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawLine(points[i], points[(i + 1) % points.Length]);
            }
            
            // Draw inner marker
            float innerSize = size * 0.6f * (0.8f + pulse * 0.2f);
            Vector3[] innerPoints = new Vector3[]
            {
                position + Vector3.up * innerSize,
                position + Vector3.right * innerSize,
                position - Vector3.up * innerSize,
                position - Vector3.right * innerSize
            };
            
            for (int i = 0; i < innerPoints.Length; i++)
            {
                Gizmos.DrawLine(innerPoints[i], innerPoints[(i + 1) % innerPoints.Length]);
            }
        }

        [System.Serializable]
        public class AIStateTransition
        {
            public AIState FromState;
            public AIState ToState;
            public float TransitionTime;
            public float StartTime;
            public float Progress => Mathf.Clamp01((Time.time - StartTime) / TransitionTime);
        }

        private Dictionary<int, AIStateTransition> stateTransitions = new Dictionary<int, AIStateTransition>();
        private Dictionary<int, float> alertnessLevels = new Dictionary<int, float>();

        private void UpdateAIBehavior(GameObject squad)
        {
            int squadId = GetSquadId(squad);
            AIState currentState = GetSquadAIState(squad);
            
            // Update alertness based on nearby threats
            float alertness = CalculateSquadAlertness(squad);
            alertnessLevels[squadId] = Mathf.Lerp(
                alertnessLevels.GetValueOrDefault(squadId, 0f),
                alertness,
                Time.deltaTime * 2f
            );
            
            // Handle state transitions
            if (stateTransitions.TryGetValue(squadId, out var transition))
            {
                if (transition.ToState != currentState)
                {
                    // State changed during transition, start new transition
                    StartStateTransition(squad, transition.FromState, currentState);
                }
                else if (transition.Progress >= 1f)
                {
                    // Transition complete
                    stateTransitions.Remove(squadId);
                }
                else
                {
                    // Draw transitioning state
                    DrawTransitioningState(squad, transition);
                }
            }
            else if (HasStateChanged(squad))
            {
                // Start new transition
                StartStateTransition(squad, GetPreviousState(squad), currentState);
            }
            
            // Update position tracking and draw movement trail
            if (lastKnownPositions.ContainsKey(squadId))
            {
                Vector3 lastPos = lastKnownPositions[squadId];
                float moveSpeed = Vector3.Distance(GetSquadPosition(squad), lastPos) / Time.deltaTime;
                
                // Affect state visualization based on movement
                if (moveSpeed > 0.1f)
                {
                    DrawMovementTrail(lastPos, GetSquadPosition(squad), moveSpeed);
                }
            }
            lastKnownPositions[squadId] = GetSquadPosition(squad);
        }

        private void StartStateTransition(GameObject squad, AIState fromState, AIState toState)
        {
            int squadId = GetSquadId(squad);
            
            stateTransitions[squadId] = new AIStateTransition
            {
                FromState = fromState,
                ToState = toState,
                TransitionTime = 0.5f,
                StartTime = Time.time
            };
        }

        private void DrawTransitioningState(GameObject squad, AIStateTransition transition)
        {
            Vector3 position = GetSquadPosition(squad);
            float progress = transition.Progress;
            
            // Blend between state visualizations
            Color fromColor = GetStateColor(transition.FromState);
            Color toColor = GetStateColor(transition.ToState);
            Gizmos.color = Color.Lerp(fromColor, toColor, progress);
            
            // Draw transitioning icon
            switch (transition.ToState)
            {
                case AIState.Searching:
                    DrawTransitioningSearchIcon(position, progress);
                    break;
                case AIState.Engaging:
                    DrawTransitioningEngageIcon(position, progress);
                    break;
                case AIState.Supporting:
                    DrawTransitioningSupportIcon(position, progress);
                    break;
                case AIState.Retreating:
                    DrawTransitioningRetreatIcon(position, progress);
                    break;
            }
        }

        private Color GetStateColor(AIState state)
        {
            switch (state)
            {
                case AIState.Searching:
                    return new Color(0.2f, 0.8f, 0.8f, 0.6f);
                case AIState.Engaging:
                    return new Color(0.8f, 0.2f, 0.2f, 0.8f);
                case AIState.Supporting:
                    return new Color(0.2f, 0.8f, 0.2f, 0.8f);
                case AIState.Retreating:
                    return new Color(0.8f, 0.4f, 0.1f, 0.6f);
                default:
                    return Color.white;
            }
        }

        private void DrawTransitioningSearchIcon(Vector3 position, float progress)
        {
            float size = aiViz.StateIconSize;
            float time = Time.time * 2f;
            float alpha = 0.3f + progress * 0.3f;
            
            for (int i = 0; i < 4; i++)
            {
                float angle = time + i * Mathf.PI / 2f;
                float radius = size * 0.5f * progress;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                
                Gizmos.color = new Color(0.2f, 0.8f, 0.8f, alpha * (Mathf.Sin(time + i) + 1f) * 0.5f);
                Gizmos.DrawWireSphere(position + offset, size * 0.2f * progress);
            }
        }

        private void DrawTransitioningEngageIcon(Vector3 position, float progress)
        {
            float size = aiViz.StateIconSize * progress;
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, (0.4f + pulse * 0.4f) * progress);
            
            Vector3 right = Vector3.right * size * 0.7f;
            Vector3 forward = Vector3.forward * size * 0.7f;
            
            // Draw expanding crossed swords
            Gizmos.DrawLine(position - right * progress - forward * progress, 
                position + right * progress + forward * progress);
            Gizmos.DrawLine(position - right * progress + forward * progress, 
                position + right * progress - forward * progress);
        }

        private void DrawTransitioningSupportIcon(Vector3 position, float progress)
        {
            float size = aiViz.StateIconSize * progress;
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, (0.4f + pulse * 0.4f) * progress);
            
            // Draw expanding support symbol
            DrawCircle(position, size * 1.2f * progress, 16);
            
            Gizmos.DrawLine(position + Vector3.up * size * progress, 
                position - Vector3.up * size * progress);
            Gizmos.DrawLine(position + Vector3.right * size * progress, 
                position - Vector3.right * size * progress);
        }

        private void DrawTransitioningRetreatIcon(Vector3 position, float progress)
        {
            float size = aiViz.StateIconSize;
            float time = Time.time * 4f;
            
            for (int i = 0; i < 3; i++)
            {
                float offset = (i - 1) * size * 0.4f * progress;
                float alpha = Mathf.Clamp01(1f - ((time + i * 0.3f) % 1f)) * progress;
                
                Gizmos.color = new Color(0.8f, 0.4f, 0.1f, alpha * 0.6f);
                Vector3 arrowPos = position + Vector3.right * offset;
                
                // Draw appearing retreat arrows
                Vector3 tip = arrowPos + Vector3.up * size * progress;
                Vector3 basePos = arrowPos - Vector3.up * size * progress;
                
                Gizmos.DrawLine(basePos, tip);
                
                float headSize = size * 0.3f * progress;
                Gizmos.DrawLine(tip, tip - Vector3.up * headSize + Vector3.right * headSize);
                Gizmos.DrawLine(tip, tip - Vector3.up * headSize - Vector3.right * headSize);
            }
        }

        private void DrawMovementTrail(Vector3 from, Vector3 to, float speed)
        {
            float trailLength = Mathf.Min(speed * 0.5f, 5f);
            int segments = Mathf.CeilToInt(trailLength * 2f);
            Vector3 direction = (to - from).normalized;
            
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float alpha = (1f - t) * 0.3f;
                
                Gizmos.color = new Color(0.8f, 0.8f, 0.8f, alpha);
                Vector3 pos = Vector3.Lerp(from, to, t);
                float width = 0.2f * (1f - t);
                
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * width;
                Gizmos.DrawLine(pos - right, pos + right);
            }
        }

        [System.Serializable]
        public class TacticalMapOverlay
        {
            public bool ShowOverlay = true;
            public float MapScale = 1f;
            public float MapHeight = 20f;
            public Color MapBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            public Color GridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            public float GridSize = 5f;
            public bool ShowTooltips = true;
            public float TooltipDelay = 0.3f;
        }

        [SerializeField] private TacticalMapOverlay mapOverlay;
        private bool isMapVisible = false;
        private Dictionary<int, SquadTooltipData> squadTooltips = new Dictionary<int, SquadTooltipData>();
        private float lastTooltipTime;
        private Vector3? lastHoverPosition;

        private class SquadTooltipData
        {
            public string Title;
            public string State;
            public string[] Actions;
            public float[] Stats;
            public Vector3 WorldPosition;
            public bool IsVisible;
            public float HoverTime;
        }

        private void UpdateTacticalMap()
        {
            if (!isMapVisible || !mapOverlay.ShowOverlay) return;

            // Draw map background
            Vector3 mapCenter = Camera.main.transform.position + Vector3.up * mapOverlay.MapHeight;
            float mapSize = 50f * mapOverlay.MapScale;
            DrawMapBackground(mapCenter, mapSize);
            
            // Draw grid
            DrawMapGrid(mapCenter, mapSize);
            
            // Draw squad states and relationships
            foreach (var squadEntry in squadDataMap)
            {
                var squad = squadEntry.Value.TargetSquad;
                if (squad == null) continue;
                
                Vector3 squadPos = GetSquadPosition(squad);
                Vector3 mapPos = WorldToMapPosition(squadPos, mapCenter, mapSize);
                
                // Draw squad icon with state
                DrawMapSquadState(squad, mapPos);
                
                // Draw relationships if squad is hovered
                if (IsSquadHovered(squad))
                {
                    DrawMapSquadRelationships(squad, mapCenter, mapSize);
                    UpdateSquadTooltip(squad, mapPos);
                }
            }
            
            // Draw objectives
            DrawMapObjectives(mapCenter, mapSize);
        }

        private void DrawMapBackground(Vector3 center, float size)
        {
            Vector3[] corners = new Vector3[]
            {
                center + new Vector3(-size, 0, -size),
                center + new Vector3(size, 0, -size),
                center + new Vector3(size, 0, size),
                center + new Vector3(-size, 0, size)
            };
            
            Gizmos.color = mapOverlay.MapBackgroundColor;
            for (int i = 0; i < corners.Length; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);
            }
        }

        private void DrawMapGrid(Vector3 center, float size)
        {
            Gizmos.color = mapOverlay.GridColor;
            float gridStep = mapOverlay.GridSize * mapOverlay.MapScale;
            
            for (float x = -size; x <= size; x += gridStep)
            {
                Gizmos.DrawLine(
                    center + new Vector3(x, 0, -size),
                    center + new Vector3(x, 0, size)
                );
            }
            
            for (float z = -size; z <= size; z += gridStep)
            {
                Gizmos.DrawLine(
                    center + new Vector3(-size, 0, z),
                    center + new Vector3(size, 0, z)
                );
            }
        }

        private void DrawMapSquadState(GameObject squad, Vector3 mapPos)
        {
            AIState state = GetSquadAIState(squad);
            float iconSize = aiViz.StateIconSize * mapOverlay.MapScale;
            
            // Draw state-specific icon
            switch (state)
            {
                case AIState.Searching:
                    DrawMapSearchingIcon(mapPos, iconSize);
                    break;
                case AIState.Engaging:
                    DrawMapEngagingIcon(mapPos, iconSize);
                    break;
                case AIState.Supporting:
                    DrawMapSupportingIcon(mapPos, iconSize);
                    break;
                case AIState.Retreating:
                    DrawMapRetreatingIcon(mapPos, iconSize);
                    break;
            }
        }

        private void DrawMapSquadRelationships(GameObject squad, Vector3 mapCenter, float mapSize)
        {
            Vector3 squadMapPos = WorldToMapPosition(GetSquadPosition(squad), mapCenter, mapSize);
            
            // Draw synergy links
            foreach (var nearbySquad in GetNearbySquads(GetSquadPosition(squad), GetSquadSynergyRange(squad)))
            {
                if (IsEnemySquad(nearbySquad) || nearbySquad == squad) continue;
                
                Vector3 nearbyMapPos = WorldToMapPosition(GetSquadPosition(nearbySquad), mapCenter, mapSize);
                float synergyStrength = CalculateSynergyStrength(squad, nearbySquad);
                
                DrawMapSynergyLink(squadMapPos, nearbyMapPos, synergyStrength);
            }
            
            // Draw coordination links
            foreach (var coordSquad in GetCoordinatingSquads(squad))
            {
                Vector3 coordMapPos = WorldToMapPosition(GetSquadPosition(coordSquad), mapCenter, mapSize);
                CoordinationType coordType = GetCoordinationType(squad, coordSquad);
                
                DrawMapCoordinationLink(squadMapPos, coordMapPos, coordType);
            }
        }

        private Vector3 WorldToMapPosition(Vector3 worldPos, Vector3 mapCenter, float mapSize)
        {
            float x = Mathf.Lerp(-mapSize, mapSize, (worldPos.x + mapSize) / (2f * mapSize));
            float z = Mathf.Lerp(-mapSize, mapSize, (worldPos.z + mapSize) / (2f * mapSize));
            
            return mapCenter + new Vector3(x, 0, z);
        }

        private void UpdateSquadTooltip(GameObject squad, Vector3 position)
        {
            int squadId = GetSquadId(squad);
            
            if (!squadTooltips.ContainsKey(squadId))
            {
                squadTooltips[squadId] = new SquadTooltipData
                {
                    Title = GetSquadName(squad),
                    State = GetSquadAIState(squad).ToString(),
                    Actions = GetSquadCurrentActions(squad),
                    Stats = GetSquadStats(squad),
                    WorldPosition = position,
                    IsVisible = false,
                    HoverTime = 0f
                };
            }
            
            var tooltip = squadTooltips[squadId];
            tooltip.HoverTime += Time.deltaTime;
            
            if (tooltip.HoverTime >= mapOverlay.TooltipDelay && !tooltip.IsVisible)
            {
                tooltip.IsVisible = true;
                ShowSquadTooltip(tooltip);
            }
        }

        private void ShowSquadTooltip(SquadTooltipData tooltip)
        {
            if (!mapOverlay.ShowTooltips) return;
            
            Vector3 screenPos = Camera.main.WorldToScreenPoint(tooltip.WorldPosition);
            if (screenPos.z < 0) return; // Behind camera
            
            // Draw tooltip background
            Rect tooltipRect = new Rect(screenPos.x + 10, Screen.height - screenPos.y + 10, 200, 150);
            GUI.Box(tooltipRect, "");
            
            // Draw tooltip content
            GUILayout.BeginArea(tooltipRect);
            GUILayout.Label($"<b>{tooltip.Title}</b>");
            GUILayout.Label($"State: {tooltip.State}");
            
            GUILayout.Label("Actions:");
            foreach (var action in tooltip.Actions)
            {
                GUILayout.Label($"- {action}");
            }
            
            GUILayout.Label("Stats:");
            string[] statNames = { "Health", "Morale", "Ammo", "Efficiency" };
            for (int i = 0; i < tooltip.Stats.Length; i++)
            {
                float stat = tooltip.Stats[i];
                GUILayout.Label($"{statNames[i]}: {stat:P0}");
            }
            
            GUILayout.EndArea();
        }

        private void OnGUI()
        {
            if (!isMapVisible || !mapOverlay.ShowOverlay) return;
            
            // Draw tooltips for hovered squads
            foreach (var tooltip in squadTooltips.Values)
            {
                if (tooltip.IsVisible)
                {
                    ShowSquadTooltip(tooltip);
                }
            }
        }

        private void Update()
        {
            // Toggle tactical map
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                isMapVisible = !isMapVisible;
            }
            
            // Update map if visible
            if (isMapVisible)
            {
                UpdateTacticalMap();
                HandleMapInput();
            }
            
            // Clear tooltips when map is hidden
            if (!isMapVisible)
            {
                squadTooltips.Clear();
            }
        }

        private void HandleMapInput()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                Vector3 hitPoint = hit.point;
                
                // Check for squad hover
                GameObject hoveredSquad = GetSquadAtPosition(hitPoint);
                if (hoveredSquad != null)
                {
                    lastHoverPosition = hitPoint;
                    lastTooltipTime = Time.time;
                }
                else if (lastHoverPosition.HasValue && 
                    Vector3.Distance(hitPoint, lastHoverPosition.Value) > 1f)
                {
                    lastHoverPosition = null;
                    squadTooltips.Clear();
                }
                
                // Handle click interactions
                if (Input.GetMouseButtonDown(0))
                {
                    HandleMapClick(hitPoint);
                }
            }
        }

        private void HandleMapClick(Vector3 position)
        {
            GameObject clickedSquad = GetSquadAtPosition(position);
            if (clickedSquad != null)
            {
                // Toggle detailed info or select squad
                SelectSquad(clickedSquad);
            }
            else
            {
                // Check for objective click
                var objective = GetObjectiveAtPosition(position);
                if (objective != null)
                {
                    SelectObjective(objective);
                }
            }
        }

        private void DrawMapSearchingIcon(Vector3 position, float size)
        {
            float time = Time.time * 2f;
            
            // Draw simplified search pattern
            for (int i = 0; i < 4; i++)
            {
                float angle = time + i * Mathf.PI / 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * size * 0.3f;
                Gizmos.color = new Color(0.2f, 0.8f, 0.8f, (Mathf.Sin(time + i) + 1f) * 0.3f);
                Gizmos.DrawWireSphere(position + offset, size * 0.15f);
            }
        }

        private void DrawMapEngagingIcon(Vector3 position, float size)
        {
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.4f + pulse * 0.4f);
            
            // Draw simplified combat indicator
            Vector3 right = Vector3.right * size * 0.5f;
            Vector3 forward = Vector3.forward * size * 0.5f;
            
            Gizmos.DrawLine(position - right - forward, position + right + forward);
            Gizmos.DrawLine(position - right + forward, position + right - forward);
        }

        private void DrawMapSupportingIcon(Vector3 position, float size)
        {
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.4f + pulse * 0.4f);
            
            // Draw simplified support symbol
            Gizmos.DrawWireSphere(position, size * 0.5f);
            Gizmos.DrawLine(position + Vector3.up * size * 0.5f, 
                position - Vector3.up * size * 0.5f);
            Gizmos.DrawLine(position + Vector3.right * size * 0.5f, 
                position - Vector3.right * size * 0.5f);
        }

        private void DrawMapRetreatingIcon(Vector3 position, float size)
        {
            float time = Time.time * 4f;
            
            // Draw simplified retreat arrows
            for (int i = 0; i < 3; i++)
            {
                float offset = (i - 1) * size * 0.3f;
                float alpha = Mathf.Clamp01(1f - ((time + i * 0.3f) % 1f));
                Gizmos.color = new Color(0.8f, 0.4f, 0.1f, alpha * 0.6f);
                
                Vector3 arrowPos = position + Vector3.right * offset;
                
                // Draw appearing retreat arrows
                Vector3 tip = arrowPos + Vector3.up * size * 0.5f;
                Vector3 basePos = arrowPos - Vector3.up * size * 0.5f;
                
                Gizmos.DrawLine(basePos, tip);
                
                float headSize = size * 0.3f * 0.5f;
                Gizmos.DrawLine(tip, tip - Vector3.up * headSize + Vector3.right * headSize);
                Gizmos.DrawLine(tip, tip - Vector3.up * headSize - Vector3.right * headSize);
            }
        }

        private void DrawMapSynergyLink(Vector3 start, Vector3 end, float strength)
        {
            Gizmos.color = new Color(
                aiViz.SupportRangeColor.r,
                aiViz.SupportRangeColor.g,
                aiViz.SupportRangeColor.b,
                strength * aiViz.SupportRangeColor.a * 0.7f
            );
            
            DrawDashedLine(start, end, aiViz.SynergyLineWidth * 0.5f);
        }

        private void DrawMapCoordinationLink(Vector3 start, Vector3 end, CoordinationType type)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            Vector3 midPoint = (start + end) * 0.5f + Vector3.up * (distance * 0.1f);
            
            // Draw simplified coordination line
            Gizmos.color = new Color(
                aiViz.CoordinationColor.r,
                aiViz.CoordinationColor.g,
                aiViz.CoordinationColor.b,
                aiViz.CoordinationColor.a * 0.7f
            );
            
            DrawBezierLine(start, midPoint, end, 8);
            
            // Draw coordination type indicator at midpoint
            Vector3 iconPos = CalculateBezierPoint(start, midPoint, end, 0.5f);
            float iconSize = aiViz.CoordinationArrowSize * 0.3f;
            
            switch (type)
            {
                case CoordinationType.Support:
                    DrawMapSupportIcon(iconPos);
                    break;
                case CoordinationType.Flanking:
                    DrawMapFlankingIcon(iconPos);
                    break;
                case CoordinationType.Ambush:
                    DrawMapAmbushIcon(iconPos);
                    break;
            }
        }

        private void DrawMapSupportIcon(Vector3 position)
        {
            Gizmos.DrawWireSphere(position, 1f);
            Gizmos.DrawLine(position + Vector3.up, position - Vector3.up);
        }

        private void DrawMapFlankingIcon(Vector3 position)
        {
            Vector3 right = Vector3.right;
            Vector3 up = Vector3.up;
            
            Gizmos.DrawLine(position - right, position);
            Gizmos.DrawLine(position, position + up);
            Gizmos.DrawLine(position + up, position + up + right);
        }

        private void DrawMapAmbushIcon(Vector3 position)
        {
            Gizmos.DrawLine(position + Vector3.up,
                position - Vector3.up * 0.2f);
            Gizmos.DrawWireSphere(position - Vector3.up * 0.4f, 0.2f);
        }

        private void DrawMapObjectives(Vector3 mapCenter, float mapSize)
        {
            foreach (var squad in squadDataMap.Values.Select(data => data.TargetSquad))
            {
                if (squad == null) continue;
                
                foreach (var objective in GetTacticalObjectives(squad))
                {
                    Vector3 objPos = WorldToMapPosition(GetObjectivePosition(objective), mapCenter, mapSize);
                    float priority = GetObjectivePriority(objective);
                    
                    DrawMapObjectiveMarker(objPos, priority);
                }
            }
        }

        private void DrawMapObjectiveMarker(Vector3 position, float priority)
        {
            float size = aiViz.CoordinationArrowSize * (0.6f + priority * 0.3f);
            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
            
            Gizmos.color = new Color(
                aiViz.TacticalLinkColor.r,
                aiViz.TacticalLinkColor.g,
                aiViz.TacticalLinkColor.b,
                (0.4f + pulse * 0.3f) * priority
            );
            
            // Draw simplified objective marker
            Vector3[] points = new Vector3[]
            {
                position + Vector3.up * size,
                position + Vector3.right * size,
                position - Vector3.up * size,
                position - Vector3.right * size
            };
            
            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawLine(points[i], points[(i + 1) % points.Length]);
            }
            
            // Draw inner marker
            float innerSize = size * 0.6f * (0.8f + pulse * 0.2f);
            Vector3[] innerPoints = new Vector3[]
            {
                position + Vector3.up * innerSize,
                position + Vector3.right * innerSize,
                position - Vector3.up * innerSize,
                position - Vector3.right * innerSize
            };
            
            for (int i = 0; i < innerPoints.Length; i++)
            {
                Gizmos.DrawLine(innerPoints[i], innerPoints[(i + 1) % innerPoints.Length]);
            }
        }

        [System.Serializable]
        public class AIStateTransition
        {
            public AIState FromState;
            public AIState ToState;
            public float TransitionTime;
            public float StartTime;
            public float Progress => Mathf.Clamp01((Time.time - StartTime) / TransitionTime);
        }

        private Dictionary<int, AIStateTransition> stateTransitions = new Dictionary<int, AIStateTransition>();
        private Dictionary<int, float> alertnessLevels = new Dictionary<int, float>();

        private void UpdateAIBehavior(GameObject squad)
        {
            int squadId = GetSquadId(squad);
            AIState currentState = GetSquadAIState(squad);
            
            // Update alertness based on nearby threats
            float alertness = CalculateSquadAlertness(squad);
            alertnessLevels[squadId] = Mathf.Lerp(
                alertnessLevels.GetValueOrDefault(squadId, 0f),
                alertness,
                Time.deltaTime * 2f
            );
            
            // Handle state transitions
            if (stateTransitions.TryGetValue(squadId, out var transition))
            {
                if (transition.ToState != currentState)
                {
                    // State changed during transition, start new transition
                    StartStateTransition(squad, transition.FromState, currentState);
                }
                else if (transition.Progress >= 1f)
                {
                    // Transition complete
                    stateTransitions.Remove(squadId);
                }
                else
                {
                    // Draw transitioning state
                    DrawTransitioningState(squad, transition);
                }
            }
            else if (HasStateChanged(squad))
            {
                // Start new transition
                StartStateTransition(squad, GetPreviousState(squad), currentState);
            }
            
            // Update position tracking and draw movement trail
            if (lastKnownPositions.ContainsKey(squadId))
            {
                Vector3 lastPos = lastKnownPositions[squadId];
                float moveSpeed = Vector3.Distance(GetSquadPosition(squad), lastPos) / Time.deltaTime;
                
                // Affect state visualization based on movement
                if (moveSpeed > 0.1f)
                {
                    DrawMovementTrail(lastPos, GetSquadPosition(squad), moveSpeed);
                }
            }
            lastKnownPositions[squadId] = GetSquadPosition(squad);
        }

        private void StartStateTransition(GameObject squad, AIState fromState, AIState toState)
        {
            int squadId = GetSquadId(squad);
            
            stateTransitions[squadId] = new AIStateTransition
            {
                FromState = fromState,
                ToState = toState,
                TransitionTime = 0.5f,
                StartTime = Time.time
            };
        }

        private void DrawTransitioningState(GameObject squad, AIStateTransition transition)
        {
            Vector3 position = GetSquadPosition(squad);
            float progress = transition.Progress;
            
            // Blend between state visualizations
            Color fromColor = GetStateColor(transition.FromState);
            Color toColor = GetStateColor(transition.ToState);
            Gizmos.color = Color.Lerp(fromColor, toColor, progress);
            
            // Draw transitioning icon
            switch (transition.ToState)
            {
                case AIState.Searching:
                    DrawTransitioningSearchIcon(position, progress);
                    break;
                case AIState.Engaging:
                    DrawTransitioningEngageIcon(position, progress);
                    break;
                case AIState.Supporting:
                    DrawTransitioningSupportIcon(position, progress);
                    break;
                case AIState.Retreating:
                    DrawTransitioningRetreatIcon(position, progress);
                    break;
            }
        }

        private Color GetStateColor(AIState state)
        {
            switch (state)
            {
                case AIState.Searching:
                    return new Color(0.2f, 0.8f, 0.8f, 0.6f);
                case AIState.Engaging:
                    return new Color(0.8f, 0.2f, 0.2f, 0.8f);
                case AIState.Supporting:
                    return new Color(0.2f, 0.8f, 0.2f, 0.8f);
                case AIState.Retreating:
                    return new Color(0.8f, 0.4f, 0.1f, 0.6f);
                default:
                    return Color.white;
            }
        }

        private void DrawTransitioningSearchIcon(Vector3 position, float progress)
        {
            float size = aiViz.StateIconSize;
            float time = Time.time * 2f;
            float alpha = 0.3f + progress * 0.3f;
            
            for (int i = 0; i < 4; i++)
            {
                float angle = time + i * Mathf.PI / 2f;
                float radius = size * 0.5f * progress;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                
                Gizmos.color = new Color(0.2f, 0.8f, 0.8f, alpha * (Mathf.Sin(time + i) + 1f) * 0.5f);
                Gizmos.DrawWireSphere(position + offset, size * 0.2f * progress);
            }
        }

        private void DrawTransitioningEngageIcon(Vector3 position, float progress)
        {
            float size = aiViz.StateIconSize * progress;
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, (0.4f + pulse * 0.4f) * progress);
            
            Vector3 right = Vector3.right * size * 0.7f;
            Vector3 forward = Vector3.forward * size * 0.7f;
            
            // Draw expanding crossed swords
            Gizmos.DrawLine(position - right * progress - forward * progress, 
                position + right * progress + forward * progress);
            Gizmos.DrawLine(position - right * progress + forward * progress, 
                position + right * progress - forward * progress);
        }

        private void DrawTransitioningSupportIcon(Vector3 position, float progress)
        {
            float size = aiViz.StateIconSize * progress;
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, (0.4f + pulse * 0.4f) * progress);
            
            // Draw expanding support symbol
            DrawCircle(position, size * 1.2f * progress, 16);
            
            Gizmos.DrawLine(position + Vector3.up * size * progress, 
                position - Vector3.up * size * progress);
            Gizmos.DrawLine(position + Vector3.right * size * progress, 
                position - Vector3.right * size * progress);
        }

        private void DrawTransitioningRetreatIcon(Vector3 position, float progress)
        {
            float size = aiViz.StateIconSize;
            float time = Time.time * 4f;
            
            for (int i = 0; i < 3; i++)
            {
                float offset = (i - 1) * size * 0.4f * progress;
                float alpha = Mathf.Clamp01(1f - ((time + i * 0.3f) % 1f)) * progress;
                
                Gizmos.color = new Color(0.8f, 0.4f, 0.1f, alpha * 0.6f);
                Vector3 arrowPos = position + Vector3.right * offset;
                
                // Draw appearing retreat arrows
                Vector3 tip = arrowPos + Vector3.up * size * progress;
                Vector3 basePos = arrowPos - Vector3.up * size * progress;
                
                Gizmos.DrawLine(basePos, tip);
                
                float headSize = size * 0.3f * progress;
                Gizmos.DrawLine(tip, tip - Vector3.up * headSize + Vector3.right * headSize);
                Gizmos.DrawLine(tip, tip - Vector3.up * headSize - Vector3.right * headSize);
            }
        }

        private void DrawMovementTrail(Vector3 from, Vector3 to, float speed)
        {
            float trailLength = Mathf.Min(speed * 0.5f, 5f);
            int segments = Mathf.CeilToInt(trailLength * 2f);
            Vector3 direction = (to - from).normalized;
            
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float alpha = (1f - t) * 0.3f;
                
                Gizmos.color = new Color(0.8f, 0.8f, 0.8f, alpha);
                Vector3 pos = Vector3.Lerp(from, to, t);
                float width = 0.2f * (1f - t);
                
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * width;
                Gizmos.DrawLine(pos - right, pos + right);
            }
        }

        public enum TerrainType
        {
            Default,
            Water,
            Grass,
            Cover
        }

        [System.Serializable]
        public class TrailSegment
        {
            public Vector3 Position;
            public float Width;
            public float Alpha;
            public float CreationTime;
            public TerrainType TerrainType;
            public float Speed;
        }

        [System.Serializable]
        public class UnitTrailData
        {
            public List<TrailSegment> Segments = new List<TrailSegment>();
            public float MaxTrailTime = 3f;
            public int MaxSegments = 50;
        }

        private Dictionary<int, UnitTrailData> unitTrails = new Dictionary<int, UnitTrailData>();

        private void UpdateMovementTrail(GameObject squad, Vector3 currentPos, float speed)
        {
            int squadId = GetSquadId(squad);
            if (!unitTrails.ContainsKey(squadId))
            {
                unitTrails[squadId] = new UnitTrailData();
            }

            var trailData = unitTrails[squadId];
            float currentTime = Time.time;

            // Clean up old segments
            trailData.Segments.RemoveAll(s => currentTime - s.CreationTime > trailData.MaxTrailTime);
            while (trailData.Segments.Count > trailData.MaxSegments)
            {
                trailData.Segments.RemoveAt(0);
            }

            // Add new segment
            if (trailData.Segments.Count == 0 || Vector3.Distance(currentPos, trailData.Segments[trailData.Segments.Count - 1].Position) > 0.1f)
            {
                var segment = new TrailSegment
                {
                    Position = currentPos,
                    Width = CalculateTrailWidth(speed, GetSquadAlertness(squad)),
                    Alpha = 1f,
                    CreationTime = currentTime,
                    TerrainType = GetTerrainTypeAtPosition(currentPos),
                    Speed = speed
                };
                trailData.Segments.Add(segment);
            }

            DrawTrail(trailData, squad);
        }

        private TerrainType GetTerrainTypeAtPosition(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 2f, terrainData.TerrainMask))
            {
                // Check for deformation
                float deformation = GetTerrainDeformation(position);
                if (deformation > terrainData.TerrainDeformationDepth * 0.5f)
                {
                    return TerrainType.Deformed;
                }

                // Check for water with weather effects
                if (hit.point.y < terrainData.HeightThreshold + (terrainData.WeatherIntensity * 0.1f))
                {
                    return TerrainType.Water;
                }

                // Check slope for terrain type with weather consideration
                Vector3 normal = hit.normal;
                float slope = Vector3.Angle(normal, Vector3.up);
                float effectiveSlope = slope * (1f + envData.TerrainMoisture * 0.5f);
                
                // Check for nearby cover with weather visibility
                if (Physics.CheckSphere(position, terrainData.CoverDistance * envData.VisibilityModifier, terrainData.CoverMask))
                {
                    return TerrainType.Cover;
                }

                // Check for grass with wind effects
                if (effectiveSlope < terrainData.SlopeThreshold)
                {
                    return TerrainType.Grass;
                }
            }

            return TerrainType.Default;
        }

        private float GetStealthModifier(Vector3 position, TerrainType terrain)
        {
            float baseModifier = terrainData.StealthModifier;
            
            // Apply terrain-specific modifiers
            switch (terrain)
            {
                case TerrainType.Grass:
                    baseModifier *= 1.5f + (envData.WindStrength * 0.2f);
                    break;
                case TerrainType.Cover:
                    baseModifier *= 2f;
                    break;
                case TerrainType.Water:
                    baseModifier *= 0.7f + (terrainData.WeatherIntensity * 0.3f);
                    break;
                case TerrainType.Deformed:
                    baseModifier *= 0.5f;
                    break;
            }
            
            // Apply weather effects
            baseModifier *= Mathf.Lerp(1f, 1.5f, terrainData.WeatherIntensity);
            baseModifier *= envData.VisibilityModifier;
            
            return baseModifier;
        }

        private void DrawTrail(UnitTrailData trailData, GameObject squad)
        {
            if (trailData.Segments.Count < 2) return;

            float currentTime = Time.time;
            AIState currentState = GetSquadAIState(squad);
            
            Color baseColor = GetStateColor(currentState);

            for (int i = 1; i < trailData.Segments.Count; i++)
            {
                var prev = trailData.Segments[i - 1];
                var curr = trailData.Segments[i];
                
                float ageAlpha = 1f - (currentTime - curr.CreationTime) / trailData.MaxTrailTime;
                float terrainAlpha = GetTerrainAlphaModifier(curr.TerrainType);
                float finalAlpha = ageAlpha * terrainAlpha * baseColor.a;

                Vector3 direction = (curr.Position - prev.Position).normalized;
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
                
                // Apply terrain-specific effects
                switch (curr.TerrainType)
                {
                    case TerrainType.Water:
                        DrawWaterEffect(prev.Position, curr.Position, right, curr.Width, finalAlpha, baseColor);
                        break;
                    case TerrainType.Grass:
                        DrawGrassEffect(prev.Position, curr.Position, right, curr.Width, finalAlpha, baseColor);
                        break;
                    case TerrainType.Cover:
                        DrawCoverEffect(curr.Position, curr.Width, finalAlpha, baseColor);
                        DrawDefaultTrail(prev.Position, curr.Position, right, curr.Width * 0.7f, finalAlpha * 0.7f, baseColor);
                        break;
                    default:
                        DrawDefaultTrail(prev.Position, curr.Position, right, curr.Width, finalAlpha, baseColor);
                        break;
                }
            }
        }

        private void DrawWaterEffect(Vector3 prev, Vector3 curr, Vector3 right, float width, float alpha, Color baseColor)
        {
            float rippleTime = Time.time * 3f;
            float distance = Vector3.Distance(prev, curr);
            int rippleCount = Mathf.Max(3, Mathf.FloorToInt(distance * 2f));
            
            for (int i = 0; i < rippleCount; i++)
            {
                float t = i / (float)(rippleCount - 1);
                float ripplePhase = rippleTime + t * Mathf.PI;
                float rippleOffset = Mathf.Sin(ripplePhase) * width * 0.3f;
                Vector3 offset = right * rippleOffset;
                
                // Adjust color based on depth
                float depthFactor = Mathf.Clamp01((curr.y - prev.y + 0.1f) * 5f);
                Color rippleColor = new Color(
                    baseColor.r * 0.8f,
                    baseColor.g * 0.9f,
                    baseColor.b + (1f - baseColor.b) * 0.2f * depthFactor,
                    alpha * (0.5f + depthFactor * 0.3f)
                );
                Gizmos.color = rippleColor;
                
                Vector3 pos = Vector3.Lerp(prev, curr, t);
                float rippleWidth = width * (1f + Mathf.Sin(ripplePhase * 2f) * 0.2f);
                Vector3 rippleRight = right * rippleWidth;
                
                // Draw ripple effect
                Gizmos.DrawLine(pos + offset - rippleRight, pos + offset + rippleRight);
                
                // Draw wake effect
                if (i > 0)
                {
                    Vector3 prevPos = Vector3.Lerp(prev, curr, (i - 1) / (float)(rippleCount - 1));
                    Gizmos.DrawLine(prevPos + offset, pos + offset);
                }
            }
        }

        private void DrawGrassEffect(Vector3 prev, Vector3 curr, Vector3 right, float width, float alpha, Color baseColor)
        {
            float grassTime = Time.time * 2f;
            float distance = Vector3.Distance(prev, curr);
            int grassCount = Mathf.Max(4, Mathf.FloorToInt(distance * 3f));
            
            for (int i = 0; i < grassCount; i++)
            {
                float t = i / (float)(grassCount - 1);
                float grassPhase = grassTime + t * Mathf.PI;
                
                // Calculate grass blade positions
                Vector3 basePos = Vector3.Lerp(prev, curr, t);
                float bladeSway = Mathf.Sin(grassPhase) * width * 0.2f;
                Vector3 swayOffset = right * bladeSway;
                
                // Adjust color based on movement speed
                float speedFactor = Vector3.Distance(prev, curr) / Time.deltaTime;
                Color grassColor = new Color(
                    baseColor.r * 0.7f,
                    baseColor.g * 1.1f,
                    baseColor.b * 0.7f,
                    alpha * (0.4f + speedFactor * 0.1f)
                );
                Gizmos.color = grassColor;
                
                // Draw grass blades
                for (int j = 0; j < 3; j++)
                {
                    float bladeOffset = (j - 1) * width * 0.3f;
                    Vector3 bladeBase = basePos + right * bladeOffset;
                    Vector3 bladeTip = bladeBase + swayOffset + Vector3.up * width * 0.5f;
                    
                    Gizmos.DrawLine(bladeBase, bladeTip);
                    
                    // Draw blade details
                    float tipSway = Mathf.Sin(grassPhase * 1.5f + j) * width * 0.1f;
                    Vector3 tipLeft = bladeTip + (right * tipSway - Vector3.up * width * 0.1f);
                    Vector3 tipRight = bladeTip - (right * tipSway - Vector3.up * width * 0.1f);
                    
                    Gizmos.DrawLine(bladeTip, tipLeft);
                    Gizmos.DrawLine(bladeTip, tipRight);
                }
            }
        }

        private void DrawCoverEffect(Vector3 position, float width, float alpha, Color baseColor)
        {
            float coverTime = Time.time * 4f;
            float coverPulse = (Mathf.Sin(coverTime) + 1f) * 0.5f;
            
            // Draw cover interaction markers
            float coverSize = width * 1.5f;
            int segments = 8;
            
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float nextAngle = (i + 1) * Mathf.PI * 2f / segments;
                
                Vector3 start = position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * coverSize;
                Vector3 end = position + new Vector3(Mathf.Cos(nextAngle), 0, Mathf.Sin(nextAngle)) * coverSize;
                
                // Pulse effect
                float pulseOffset = Mathf.Sin(coverTime + angle * 2f) * width * 0.2f;
                start += start.normalized * pulseOffset;
                end += end.normalized * pulseOffset;
                
                Color coverColor = new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    alpha * (0.3f + coverPulse * 0.2f)
                );
                Gizmos.color = coverColor;
                
                Gizmos.DrawLine(start, end);
                Gizmos.DrawLine(position, start);
            }
        }

        private float GetTerrainAlphaModifier(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Water: return 0.7f;
                case TerrainType.Grass: return 0.5f;
                default: return 1f;
            }
        }

        private float CalculateTrailWidth(float speed, float alertness)
        {
            float baseWidth = 0.2f;
            float speedMod = Mathf.Clamp01(speed / 10f);
            float alertMod = Mathf.Clamp01(1f - alertness);
            
            return baseWidth * (0.5f + speedMod * 0.8f) * (0.6f + alertMod * 0.4f);
        }

        private void UpdateVisualization()
        {
            if (!showVisualization) return;

            foreach (var squadData in squadDataMap.Values)
            {
                var squad = squadData.TargetSquad;
                if (!squad) continue;

                Vector3 position = GetSquadPosition(squad);
                float speed = squadData.CurrentSpeed;
                
                // Update movement trail
                UpdateMovementTrail(squad, position, speed);
                
                // Update AI behavior and state transitions
                UpdateAIBehavior(squad);
                
                // Draw squad synergy and coordination
                if (tacticalViz.ShowSquadSynergy)
                {
                    DrawSquadSynergy(squad);
                }

                // Draw tactical indicators
                if (tacticalViz.ShowTacticalMarkers)
                {
                    DrawTacticalMarkers(squad);
                }
            }

            // Update tactical map overlay
            if (tacticalMap.ShowOverlay)
            {
                UpdateTacticalMap();
            }
        }

        [System.Serializable]
        private class TerrainInteractionData
        {
            public float HeightThreshold = 0.1f;
            public float SlopeThreshold = 30f;
            public float CoverDistance = 2f;
            public float StealthModifier = 1f;
            public float TerrainDeformationRadius = 1f;
            public float TerrainDeformationDepth = 0.2f;
            public LayerMask TerrainMask;
            public LayerMask CoverMask;
            public float WeatherIntensity = 0f; // 0-1 range for weather effects
            public float Temperature = 20f; // Celsius
            public float TerrainTemperature = 20f;
            public float SeasonProgress = 0f; // 0-1 range for season progression
        }

        [System.Serializable]
        private class EnvironmentalEffectData
        {
            public float WindDirection = 0f;
            public float WindStrength = 1f;
            public float RainIntensity = 0f;
            public float TerrainMoisture = 0f;
            public float VisibilityModifier = 1f;
            public float ThunderTimer = 0f;
            public bool IsThunderstorm = false;
            public float LightningIntensity = 0f;
            public float FrostLevel = 0f;
            public float HeatLevel = 0f;
        }

        private TerrainInteractionData terrainData = new TerrainInteractionData();
        private EnvironmentalEffectData envData = new EnvironmentalEffectData();
        private Dictionary<Vector3, float> terrainDeformations = new Dictionary<Vector3, float>();

        private void UpdateTerrainDeformation(Vector3 position, float weight)
        {
            Vector3 deformPos = new Vector3(
                Mathf.Round(position.x / terrainData.TerrainDeformationRadius) * terrainData.TerrainDeformationRadius,
                position.y,
                Mathf.Round(position.z / terrainData.TerrainDeformationRadius) * terrainData.TerrainDeformationRadius
            );

            if (terrainDeformations.ContainsKey(deformPos))
            {
                terrainDeformations[deformPos] = Mathf.Min(
                    terrainDeformations[deformPos] + weight * Time.deltaTime,
                    terrainData.TerrainDeformationDepth
                );
            }
            else
            {
                terrainDeformations[deformPos] = weight * Time.deltaTime;
            }
        }

        private float GetTerrainDeformation(Vector3 position)
        {
            Vector3 deformPos = new Vector3(
                Mathf.Round(position.x / terrainData.TerrainDeformationRadius) * terrainData.TerrainDeformationRadius,
                position.y,
                Mathf.Round(position.z / terrainData.TerrainDeformationRadius) * terrainData.TerrainDeformationRadius
            );

            return terrainDeformations.ContainsKey(deformPos) ? terrainDeformations[deformPos] : 0f;
        }

        private void UpdateEnvironmentalEffects()
        {
            float deltaTime = Time.deltaTime;
            
            // Update weather effects
            if (envData.IsThunderstorm)
            {
                envData.ThunderTimer -= deltaTime;
                if (envData.ThunderTimer <= 0f)
                {
                    // Trigger lightning
                    envData.LightningIntensity = 1f;
                    envData.ThunderTimer = Random.Range(5f, 15f);
                    envData.VisibilityModifier = Mathf.Min(2f, envData.VisibilityModifier + 0.5f);
                }
            }
            
            // Fade lightning
            if (envData.LightningIntensity > 0f)
            {
                envData.LightningIntensity = Mathf.Max(0f, envData.LightningIntensity - deltaTime * 2f);
                envData.VisibilityModifier = Mathf.Lerp(envData.VisibilityModifier, 1f, deltaTime * 2f);
            }
            
            // Update terrain temperature based on season and weather
            float targetTemp = GetSeasonalTemperature();
            terrainData.TerrainTemperature = Mathf.Lerp(
                terrainData.TerrainTemperature,
                targetTemp - (envData.RainIntensity * 5f),
                deltaTime * 0.1f
            );
            
            // Update terrain states
            foreach (var kvp in terrainStates)
            {
                var state = kvp.Value;
                
                // Update moisture
                state.Moisture = Mathf.Lerp(
                    state.Moisture,
                    envData.RainIntensity + envData.TerrainMoisture,
                    deltaTime * 0.2f
                );
                
                // Update temperature
                state.Temperature = Mathf.Lerp(
                    state.Temperature,
                    terrainData.TerrainTemperature,
                    deltaTime * 0.1f
                );
                
                // Update snow and ice based on temperature
                if (state.Temperature < 0f)
                {
                    state.SnowCover = Mathf.Min(1f, state.SnowCover + deltaTime * 0.1f);
                    if (state.Moisture > 0.5f)
                    {
                        state.IceThickness = Mathf.Min(1f, state.IceThickness + deltaTime * 0.05f);
                    }
                }
                else
                {
                    state.SnowCover = Mathf.Max(0f, state.SnowCover - deltaTime * 0.2f);
                    state.IceThickness = Mathf.Max(0f, state.IceThickness - deltaTime * 0.1f);
                }
                
                // Update vegetation based on season and moisture
                float targetVegetation = GetSeasonalVegetationDensity() * (0.5f + state.Moisture * 0.5f);
                state.VegetationDensity = Mathf.Lerp(state.VegetationDensity, targetVegetation, deltaTime * 0.05f);
                
                // Update erosion
                if (state.Deformation > 0f && state.Moisture > 0.7f)
                {
                    state.ErosionLevel = Mathf.Min(1f, state.ErosionLevel + deltaTime * 0.1f);
                    state.Deformation = Mathf.Max(0f, state.Deformation - deltaTime * 0.05f * state.ErosionLevel);
                }
            }
            
            // Update extreme weather effects
            UpdateExtremeWeatherEffects();
        }

        private float GetSeasonalTemperature()
        {
            switch (currentSeason)
            {
                case Season.Winter:
                    return -5f + Random.Range(-3f, 3f);
                case Season.Spring:
                    return 15f + Random.Range(-5f, 5f);
                case Season.Summer:
                    return 25f + Random.Range(-3f, 3f);
                case Season.Autumn:
                    return 10f + Random.Range(-5f, 5f);
                default:
                    return 20f;
            }
        }

        private float GetSeasonalVegetationDensity()
        {
            switch (currentSeason)
            {
                case Season.Winter:
                    return 0.2f;
                case Season.Spring:
                    return 0.8f;
                case Season.Summer:
                    return 1f;
                case Season.Autumn:
                    return 0.6f;
                default:
                    return 0.5f;
            }
        }

        private void UpdateExtremeWeatherEffects()
        {
            float temp = terrainData.TerrainTemperature;
            
            // Update frost effects
            if (temp < 0f)
            {
                envData.FrostLevel = Mathf.Min(1f, envData.FrostLevel + Time.deltaTime * 0.1f);
            }
            else
            {
                envData.FrostLevel = Mathf.Max(0f, envData.FrostLevel - Time.deltaTime * 0.2f);
            }
            
            // Update heat effects
            if (temp > 30f)
            {
                envData.HeatLevel = Mathf.Min(1f, envData.HeatLevel + Time.deltaTime * 0.1f);
            }
            else
            {
                envData.HeatLevel = Mathf.Max(0f, envData.HeatLevel - Time.deltaTime * 0.2f);
            }
        }
    }
}
