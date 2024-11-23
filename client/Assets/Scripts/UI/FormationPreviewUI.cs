using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RTS.UI
{
    public class FormationPreviewUI : MonoBehaviour
    {
        public GameObject unitMarkerPrefab;
        public float previewScale = 1f;
        public Color previewColor = new Color(1f, 1f, 1f, 0.5f);
        public float fadeTime = 0.5f;
        public float displayTime = 2f;

        private List<GameObject> markerInstances = new List<GameObject>();
        private float displayTimer;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void UpdatePreview(Vector3 centerPosition, FormationType formationType, float rotation, int unitCount)
        {
            // Clear existing markers
            ClearMarkers();

            // Generate formation positions
            Vector2[] positions = GenerateFormationPositions(formationType, unitCount);

            // Create markers for each position
            foreach (var position in positions)
            {
                Vector3 worldPos = centerPosition + new Vector3(
                    position.x * Mathf.Cos(rotation) - position.y * Mathf.Sin(rotation),
                    0,
                    position.x * Mathf.Sin(rotation) + position.y * Mathf.Cos(rotation)
                ) * previewScale;

                CreateMarker(worldPos);
            }

            // Show preview
            ShowPreview();
        }

        private Vector2[] GenerateFormationPositions(FormationType formationType, int unitCount)
        {
            switch (formationType)
            {
                case FormationType.Line:
                    return GenerateLinePositions(unitCount);
                case FormationType.Column:
                    return GenerateColumnPositions(unitCount);
                case FormationType.Wedge:
                    return GenerateWedgePositions(unitCount);
                case FormationType.Square:
                    return GenerateSquarePositions(unitCount);
                case FormationType.Circle:
                    return GenerateCirclePositions(unitCount);
                default:
                    return GenerateLinePositions(unitCount);
            }
        }

        private Vector2[] GenerateLinePositions(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            float startX = -(unitCount - 1) * 2f / 2f;
            for (int i = 0; i < unitCount; i++)
            {
                positions[i] = new Vector2(startX + i * 2f, 0);
            }
            return positions;
        }

        private Vector2[] GenerateColumnPositions(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            float startZ = -(unitCount - 1) * 2f / 2f;
            for (int i = 0; i < unitCount; i++)
            {
                positions[i] = new Vector2(0, startZ + i * 2f);
            }
            return positions;
        }

        private Vector2[] GenerateWedgePositions(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            int rows = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int currentUnit = 0;

            for (int row = 0; row < rows && currentUnit < unitCount; row++)
            {
                int unitsInRow = Mathf.Min(row * 2 + 1, unitCount - currentUnit);
                float startX = -(unitsInRow - 1) * 2f / 2f;
                float z = -row * 2f;

                for (int i = 0; i < unitsInRow && currentUnit < unitCount; i++)
                {
                    positions[currentUnit] = new Vector2(startX + i * 2f, z);
                    currentUnit++;
                }
            }
            return positions;
        }

        private Vector2[] GenerateSquarePositions(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            int side = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int currentUnit = 0;

            for (int row = 0; row < side && currentUnit < unitCount; row++)
            {
                for (int col = 0; col < side && currentUnit < unitCount; col++)
                {
                    float x = (col - (side - 1) / 2f) * 2f;
                    float z = (row - (side - 1) / 2f) * 2f;
                    positions[currentUnit] = new Vector2(x, z);
                    currentUnit++;
                }
            }
            return positions;
        }

        private Vector2[] GenerateCirclePositions(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            float radius = unitCount * 0.5f;
            float angleStep = 360f / unitCount;

            for (int i = 0; i < unitCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                positions[i] = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
            }
            return positions;
        }

        private void CreateMarker(Vector3 position)
        {
            GameObject marker = Instantiate(unitMarkerPrefab, transform);
            marker.transform.position = position;

            // Set marker appearance
            var image = marker.GetComponent<Image>();
            if (image != null)
            {
                image.color = previewColor;
            }

            markerInstances.Add(marker);
        }

        private void ClearMarkers()
        {
            foreach (var marker in markerInstances)
            {
                Destroy(marker);
            }
            markerInstances.Clear();
        }

        private void ShowPreview()
        {
            displayTimer = displayTime;
            canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (displayTimer > 0)
            {
                displayTimer -= Time.deltaTime;
                if (displayTimer <= fadeTime)
                {
                    canvasGroup.alpha = displayTimer / fadeTime;
                }
            }
        }

        private void OnDestroy()
        {
            ClearMarkers();
        }
    }
}
