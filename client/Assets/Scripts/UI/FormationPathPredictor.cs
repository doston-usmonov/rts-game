using UnityEngine;
using System.Collections.Generic;
using RTS.Units;
using RTS.Vision;

namespace RTS.UI
{
    public class FormationPathPredictor : MonoBehaviour
    {
        [Header("Path Visualization")]
        [SerializeField] private float pathUpdateInterval = 0.2f;
        [SerializeField] private int pathSegments = 20;
        [SerializeField] private float pathLength = 10f;
        [SerializeField] private Color pathColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private float pathWidth = 0.2f;
        [SerializeField] private float collisionCheckRadius = 1f;

        [Header("Terrain Adaptation")]
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private float maxSlopeAngle = 45f;
        [SerializeField] private float heightCheckDistance = 1f;
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0f, 0.5f);
        [SerializeField] private Color dangerColor = new Color(1f, 0f, 0f, 0.5f);

        private Dictionary<MonoBehaviour, LineRenderer> unitPaths = new Dictionary<MonoBehaviour, LineRenderer>();
        private Dictionary<MonoBehaviour, List<Vector3>> predictedPaths = new Dictionary<MonoBehaviour, List<Vector3>>();
        private float lastUpdateTime;
        private bool isActive;
        private HashSet<Vector3> targetPositions = new HashSet<Vector3>();

        private void Awake()
        {
            isActive = true;
        }

        private void Update()
        {
            if (!isActive || targetPositions == null || targetPositions.Count == 0)
                return;

            // Optimize path prediction calculations
            if (Time.time - lastUpdateTime < pathUpdateInterval)
                return;

            lastUpdateTime = Time.time;
            
            UpdateAllPaths();
        }

        public void UpdateGroupPath(HashSet<MonoBehaviour> units, Vector3 destination, FormationType formationType)
        {
            if (units == null || units.Count == 0)
                return;

            Vector3 groupCenter = GetGroupCenter(units);
            Vector2[] formationOffsets = GenerateFormationOffsets(units.Count, formationType);

            int index = 0;
            foreach (var unit in units)
            {
                if (index >= formationOffsets.Length) break;

                Vector3 targetPosition = CalculateFormationPosition(destination, formationOffsets[index], formationType);
                UpdateUnitPath(unit, targetPosition);
                index++;
            }
        }

        private Vector3 GetGroupCenter(HashSet<MonoBehaviour> units)
        {
            if (units == null || units.Count == 0)
                return Vector3.zero;

            Vector3 center = Vector3.zero;
            foreach (var unit in units)
            {
                center += unit.transform.position;
            }
            return center / units.Count;
        }

        private Vector2[] GenerateFormationOffsets(int unitCount, FormationType formationType)
        {
            switch (formationType)
            {
                case FormationType.Line:
                    return GenerateLineFormation(unitCount);
                case FormationType.Column:
                    return GenerateColumnFormation(unitCount);
                case FormationType.Wedge:
                    return GenerateWedgeFormation(unitCount);
                case FormationType.Box:
                    return GenerateBoxFormation(unitCount);
                default:
                    return new Vector2[unitCount];
            }
        }

        private Vector2[] GenerateLineFormation(int unitCount)
        {
            Vector2[] offsets = new Vector2[unitCount];
            float spacing = 2f;
            float startX = -(unitCount - 1) * spacing * 0.5f;

            for (int i = 0; i < unitCount; i++)
            {
                offsets[i] = new Vector2(startX + i * spacing, 0f);
            }

            return offsets;
        }

        private Vector2[] GenerateColumnFormation(int unitCount)
        {
            Vector2[] offsets = new Vector2[unitCount];
            float spacing = 2f;
            float startZ = -(unitCount - 1) * spacing * 0.5f;

            for (int i = 0; i < unitCount; i++)
            {
                offsets[i] = new Vector2(0f, startZ + i * spacing);
            }

            return offsets;
        }

        private Vector2[] GenerateWedgeFormation(int unitCount)
        {
            Vector2[] offsets = new Vector2[unitCount];
            float spacing = 2f;
            int rowCount = Mathf.CeilToInt(Mathf.Sqrt(unitCount * 2));
            int currentUnit = 0;

            for (int row = 0; row < rowCount && currentUnit < unitCount; row++)
            {
                int unitsInRow = Mathf.Min(row + 1, unitCount - currentUnit);
                float rowWidth = (unitsInRow - 1) * spacing;
                float startX = -rowWidth * 0.5f;

                for (int i = 0; i < unitsInRow && currentUnit < unitCount; i++)
                {
                    offsets[currentUnit] = new Vector2(startX + i * spacing, -row * spacing);
                    currentUnit++;
                }
            }

            return offsets;
        }

        private Vector2[] GenerateBoxFormation(int unitCount)
        {
            Vector2[] offsets = new Vector2[unitCount];
            float spacing = 2f;
            int sideLength = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            
            for (int i = 0; i < unitCount; i++)
            {
                int row = i / sideLength;
                int col = i % sideLength;
                float x = (col - (sideLength - 1) * 0.5f) * spacing;
                float z = (row - (sideLength - 1) * 0.5f) * spacing;
                offsets[i] = new Vector2(x, z);
            }

            return offsets;
        }

        private Vector3 CalculateFormationPosition(Vector3 destination, Vector2 offset, FormationType formationType)
        {
            Vector3 forward = (destination - transform.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            
            return destination + right * offset.x + forward * offset.y;
        }

        private void UpdateUnitPath(MonoBehaviour unit, Vector3 targetPosition)
        {
            if (unit == null) return;

            // Get or create line renderer
            if (!unitPaths.TryGetValue(unit, out LineRenderer pathRenderer))
            {
                GameObject pathObj = new GameObject($"Path_{unit.name}");
                pathRenderer = pathObj.AddComponent<LineRenderer>();
                pathRenderer.startWidth = pathWidth;
                pathRenderer.endWidth = pathWidth;
                pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
                unitPaths[unit] = pathRenderer;
            }

            // Calculate path points
            List<Vector3> pathPoints = CalculatePathPoints(unit.transform.position, targetPosition);
            predictedPaths[unit] = pathPoints;

            // Update line renderer
            pathRenderer.positionCount = pathPoints.Count;
            pathRenderer.SetPositions(pathPoints.ToArray());

            // Update color based on terrain
            UpdatePathColor(pathRenderer, pathPoints);
        }

        private List<Vector3> CalculatePathPoints(Vector3 start, Vector3 end)
        {
            List<Vector3> points = new List<Vector3>();
            float segmentLength = pathLength / pathSegments;
            Vector3 direction = (end - start).normalized;

            points.Add(start);
            Vector3 currentPoint = start;

            for (int i = 1; i < pathSegments; i++)
            {
                currentPoint += direction * segmentLength;
                
                // Adjust for terrain height
                if (Physics.Raycast(currentPoint + Vector3.up * heightCheckDistance, Vector3.down, out RaycastHit hit, heightCheckDistance * 2f, terrainLayer))
                {
                    currentPoint.y = hit.point.y;
                }

                points.Add(currentPoint);
            }

            points.Add(end);
            return points;
        }

        private void UpdatePathColor(LineRenderer pathRenderer, List<Vector3> pathPoints)
        {
            bool hasWarning = false;
            bool hasDanger = false;

            for (int i = 1; i < pathPoints.Count; i++)
            {
                Vector3 direction = pathPoints[i] - pathPoints[i - 1];
                
                // Check for collisions
                if (Physics.SphereCast(pathPoints[i - 1], collisionCheckRadius, direction.normalized, out RaycastHit hit, direction.magnitude))
                {
                    hasDanger = true;
                    break;
                }

                // Check terrain slope
                if (Physics.Raycast(pathPoints[i] + Vector3.up * heightCheckDistance, Vector3.down, out hit, heightCheckDistance * 2f, terrainLayer))
                {
                    float slope = Vector3.Angle(hit.normal, Vector3.up);
                    if (slope > maxSlopeAngle)
                    {
                        hasDanger = true;
                        break;
                    }
                    else if (slope > maxSlopeAngle * 0.7f)
                    {
                        hasWarning = true;
                    }
                }
            }

            pathRenderer.startColor = pathRenderer.endColor = hasDanger ? dangerColor : (hasWarning ? warningColor : pathColor);
        }

        private void UpdateAllPaths()
        {
            foreach (var kvp in predictedPaths)
            {
                if (kvp.Key != null && unitPaths.TryGetValue(kvp.Key, out LineRenderer pathRenderer))
                {
                    UpdatePathColor(pathRenderer, kvp.Value);
                }
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
            foreach (var pathRenderer in unitPaths.Values)
            {
                if (pathRenderer != null)
                    pathRenderer.gameObject.SetActive(active);
            }
        }

        private void OnDestroy()
        {
            foreach (var pathRenderer in unitPaths.Values)
            {
                if (pathRenderer != null)
                    Destroy(pathRenderer.gameObject);
            }
            unitPaths.Clear();
            predictedPaths.Clear();
        }
    }
}
