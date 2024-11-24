using UnityEngine;
using System.Collections.Generic;
using RTS.Units;

namespace RTS.Effects
{
    public class GroupEffect : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color groupColor = Color.blue;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private float fadeSpeed = 1f;
        
        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 1f;
        [SerializeField] private float pulseMinAlpha = 0.2f;
        [SerializeField] private float pulseMaxAlpha = 0.8f;

        private List<Unit> groupUnits = new List<Unit>();
        private LineRenderer lineRenderer;
        private float currentAlpha;
        private bool isActive;

        private void Awake()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.loop = true;
            
            SetColor(groupColor);
            currentAlpha = pulseMaxAlpha;
        }

        public void SetUnits(List<Unit> units)
        {
            groupUnits = new List<Unit>(units);
            UpdateLinePositions();
            isActive = true;
        }

        public void ClearUnits()
        {
            groupUnits.Clear();
            lineRenderer.positionCount = 0;
            isActive = false;
        }

        private void Update()
        {
            if (!isActive) return;

            UpdateLinePositions();
            UpdatePulseEffect();
        }

        private void UpdateLinePositions()
        {
            if (groupUnits.Count < 2)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            Vector3[] positions = new Vector3[groupUnits.Count + 1];
            for (int i = 0; i < groupUnits.Count; i++)
            {
                if (groupUnits[i] != null)
                {
                    positions[i] = groupUnits[i].transform.position + Vector3.up * 0.1f;
                }
            }
            positions[groupUnits.Count] = positions[0]; // Close the loop

            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
        }

        private void UpdatePulseEffect()
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            currentAlpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);
            
            Color currentColor = lineRenderer.startColor;
            currentColor.a = currentAlpha;
            SetColor(currentColor);
        }

        public void SetColor(Color color)
        {
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }

        private void OnDestroy()
        {
            if (lineRenderer != null && lineRenderer.material != null)
            {
                Destroy(lineRenderer.material);
            }
        }
    }
}
