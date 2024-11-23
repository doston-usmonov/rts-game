using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RTS.UI
{
    public class TerrainAnalyzer : MonoBehaviour
    {
        [Header("Terrain Analysis Settings")]
        public LayerMask terrainLayer;
        public float analysisGridSize = 5f;
        public float heightSampleSpacing = 1f;
        public float maxAnalysisHeight = 100f;
        public int maxConcurrentRaycasts = 100;

        [Header("Tactical Parameters")]
        public float maxTraversableSlope = 35f;
        public float highGroundThreshold = 3f;
        public float coverHeight = 1.5f;
        public float waterLevel = 0f;
        public float mudSlowdownFactor = 0.5f;

        [Header("Visual Debug")]
        public bool showDebugVisualization = false;
        public Color highGroundColor = new Color(0f, 1f, 0f, 0.3f);
        public Color coverColor = new Color(0f, 0f, 1f, 0.3f);
        public Color dangerZoneColor = new Color(1f, 0f, 0f, 0.3f);
        public Color impassableColor = new Color(1f, 0f, 1f, 0.3f);

        private Dictionary<Vector2Int, TerrainCell> terrainGrid = new Dictionary<Vector2Int, TerrainCell>();
        private HashSet<Vector2Int> activeAnalysisCells = new HashSet<Vector2Int>();
        private ObjectPool<GameObject> debugMarkerPool;

        public class TerrainCell
        {
            public float height;
            public float averageSlope;
            public bool isHighGround;
            public bool providesCover;
            public bool isImpassable;
            public float movementModifier = 1f;
            public List<Vector3> coverPoints = new List<Vector3>();
            public TerrainType terrainType;
            public float lastUpdateTime;
            public GameObject debugMarker;
        }

        public enum TerrainType
        {
            Normal,
            HighGround,
            Cover,
            Water,
            Mud,
            Impassable
        }

        private void Awake()
        {
            InitializeDebugMarkerPool();
        }

        private void InitializeDebugMarkerPool()
        {
            if (!showDebugVisualization) return;

            GameObject poolContainer = new GameObject("DebugMarkerPool");
            poolContainer.transform.SetParent(transform);
            debugMarkerPool = poolContainer.AddComponent<ObjectPool<GameObject>>();
            
            debugMarkerPool.Initialize(() =>
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.transform.SetParent(poolContainer.transform);
                marker.GetComponent<Collider>().enabled = false;
                return marker;
            }, 100);
        }

        public async Task<TerrainCell> AnalyzeTerrainAtPosition(Vector3 position)
        {
            Vector2Int gridPos = WorldToGrid(position);
            
            if (terrainGrid.TryGetValue(gridPos, out TerrainCell existingCell))
            {
                if (Time.time - existingCell.lastUpdateTime < 1f)
                {
                    return existingCell;
                }
            }

            if (!activeAnalysisCells.Add(gridPos))
            {
                return null;
            }

            TerrainCell cell = await AnalyzeCell(gridPos);
            
            if (cell != null)
            {
                terrainGrid[gridPos] = cell;
                cell.lastUpdateTime = Time.time;
                
                if (showDebugVisualization)
                {
                    UpdateDebugVisualization(gridPos, cell);
                }
            }

            activeAnalysisCells.Remove(gridPos);
            return cell;
        }

        private async Task<TerrainCell> AnalyzeCell(Vector2Int gridPos)
        {
            Vector3 worldPos = GridToWorld(gridPos);
            TerrainCell cell = new TerrainCell();

            // Sample heights in a grid pattern
            List<float> heights = new List<float>();
            List<float> slopes = new List<float>();
            List<Task<RaycastHit>> raycastTasks = new List<Task<RaycastHit>>();

            for (float x = -analysisGridSize/2; x <= analysisGridSize/2; x += heightSampleSpacing)
            {
                for (float z = -analysisGridSize/2; z <= analysisGridSize/2; z += heightSampleSpacing)
                {
                    Vector3 samplePos = worldPos + new Vector3(x, maxAnalysisHeight, z);
                    raycastTasks.Add(RaycastAsync(samplePos));

                    // Process in batches to avoid overwhelming the physics system
                    if (raycastTasks.Count >= maxConcurrentRaycasts)
                    {
                        await ProcessRaycastBatch(raycastTasks, heights, slopes);
                        raycastTasks.Clear();
                    }
                }
            }

            // Process any remaining raycasts
            if (raycastTasks.Count > 0)
            {
                await ProcessRaycastBatch(raycastTasks, heights, slopes);
            }

            if (heights.Count == 0)
            {
                return null;
            }

            // Calculate average height and slope
            cell.height = heights.Count > 0 ? heights.Average() : 0f;
            cell.averageSlope = slopes.Count > 0 ? slopes.Average() : 0f;

            // Analyze terrain characteristics
            AnalyzeTerrainCharacteristics(cell, heights, worldPos);

            return cell;
        }

        private async Task ProcessRaycastBatch(
            List<Task<RaycastHit>> raycastTasks,
            List<float> heights,
            List<float> slopes)
        {
            RaycastHit[] results = await Task.WhenAll(raycastTasks);
            
            foreach (var hit in results)
            {
                if (hit.collider != null)
                {
                    heights.Add(hit.point.y);
                    slopes.Add(Vector3.Angle(hit.normal, Vector3.up));
                }
            }
        }

        private async Task<RaycastHit> RaycastAsync(Vector3 position)
        {
            RaycastHit hit;
            return await Task.Run(() =>
            {
                Physics.Raycast(position, Vector3.down, out hit, maxAnalysisHeight * 2f, terrainLayer);
                return hit;
            });
        }

        private void AnalyzeTerrainCharacteristics(TerrainCell cell, List<float> heights, Vector3 worldPos)
        {
            // Check if terrain is traversable
            cell.isImpassable = cell.averageSlope > maxTraversableSlope;

            // Determine terrain type and movement modifier
            if (cell.height <= waterLevel)
            {
                cell.terrainType = TerrainType.Water;
                cell.movementModifier = 0.3f;
            }
            else if (cell.height <= waterLevel + 0.5f)
            {
                cell.terrainType = TerrainType.Mud;
                cell.movementModifier = mudSlowdownFactor;
            }
            else
            {
                cell.terrainType = TerrainType.Normal;
                cell.movementModifier = 1f - (cell.averageSlope / maxTraversableSlope * 0.5f);
            }

            // Check for high ground
            float maxSurroundingHeight = heights.Max();
            float minSurroundingHeight = heights.Min();
            cell.isHighGround = (maxSurroundingHeight - minSurroundingHeight) >= highGroundThreshold;

            if (cell.isHighGround)
            {
                cell.terrainType = TerrainType.HighGround;
                cell.movementModifier *= 0.8f; // Slower movement on high ground
            }

            // Analyze cover points
            AnalyzeCoverPoints(cell, worldPos);
        }

        private void AnalyzeCoverPoints(TerrainCell cell, Vector3 centerPos)
        {
            float radius = analysisGridSize / 2f;
            Collider[] colliders = Physics.OverlapSphere(centerPos, radius, terrainLayer);

            foreach (var collider in colliders)
            {
                // Check if object provides cover
                Bounds bounds = collider.bounds;
                if (bounds.size.y >= coverHeight)
                {
                    Vector3 coverPoint = collider.ClosestPoint(centerPos);
                    if (Vector3.Distance(coverPoint, centerPos) <= radius)
                    {
                        cell.coverPoints.Add(coverPoint);
                    }
                }
            }

            cell.providesCover = cell.coverPoints.Count > 0;
            if (cell.providesCover)
            {
                cell.terrainType = TerrainType.Cover;
            }
        }

        private void UpdateDebugVisualization(Vector2Int gridPos, TerrainCell cell)
        {
            if (!showDebugVisualization) return;

            if (cell.debugMarker == null)
            {
                cell.debugMarker = debugMarkerPool.Get();
            }

            Vector3 worldPos = GridToWorld(gridPos);
            cell.debugMarker.transform.position = new Vector3(worldPos.x, cell.height, worldPos.z);
            cell.debugMarker.transform.localScale = Vector3.one * analysisGridSize * 0.9f;

            Material material = cell.debugMarker.GetComponent<Renderer>().material;
            material.color = GetDebugColor(cell);
        }

        private Color GetDebugColor(TerrainCell cell)
        {
            if (cell.isImpassable)
                return impassableColor;
            if (cell.isHighGround)
                return highGroundColor;
            if (cell.providesCover)
                return coverColor;
            if (cell.terrainType == TerrainType.Water || cell.terrainType == TerrainType.Mud)
                return dangerZoneColor;
            
            return Color.clear;
        }

        public float GetMovementModifier(Vector3 position)
        {
            Vector2Int gridPos = WorldToGrid(position);
            if (terrainGrid.TryGetValue(gridPos, out TerrainCell cell))
            {
                return cell.movementModifier;
            }
            return 1f;
        }

        public bool IsHighGround(Vector3 position)
        {
            Vector2Int gridPos = WorldToGrid(position);
            if (terrainGrid.TryGetValue(gridPos, out TerrainCell cell))
            {
                return cell.isHighGround;
            }
            return false;
        }

        public bool ProvidesCover(Vector3 position)
        {
            Vector2Int gridPos = WorldToGrid(position);
            if (terrainGrid.TryGetValue(gridPos, out TerrainCell cell))
            {
                return cell.providesCover;
            }
            return false;
        }

        public Vector3? GetNearestCoverPoint(Vector3 position)
        {
            Vector2Int gridPos = WorldToGrid(position);
            if (terrainGrid.TryGetValue(gridPos, out TerrainCell cell) && cell.coverPoints.Count > 0)
            {
                return cell.coverPoints.OrderBy(p => Vector3.Distance(p, position)).First();
            }
            return null;
        }

        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / analysisGridSize),
                Mathf.FloorToInt(worldPos.z / analysisGridSize)
            );
        }

        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(
                gridPos.x * analysisGridSize + analysisGridSize/2,
                0f,
                gridPos.y * analysisGridSize + analysisGridSize/2
            );
        }

        private void OnDestroy()
        {
            if (debugMarkerPool != null)
            {
                foreach (var cell in terrainGrid.Values)
                {
                    if (cell.debugMarker != null)
                    {
                        debugMarkerPool.ReturnToPool(cell.debugMarker);
                    }
                }
                Destroy(debugMarkerPool.gameObject);
            }
        }
    }
}
