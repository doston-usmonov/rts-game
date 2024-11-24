using UnityEngine;
using System.Collections.Generic;

namespace RTS.Units.Formation
{
    public class UnitFormation : MonoBehaviour
    {
        [Header("Formation Settings")]
        [SerializeField] private float spacing = 2f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private bool maintainFormation = true;

        private List<Unit> units = new List<Unit>();
        private Vector3 formationCenter;
        private Quaternion formationRotation;

        public void AddUnit(Unit unit)
        {
            if (!units.Contains(unit))
            {
                units.Add(unit);
                RecalculateFormationPositions();
            }
        }

        public void RemoveUnit(Unit unit)
        {
            if (units.Remove(unit))
            {
                RecalculateFormationPositions();
            }
        }

        public void SetFormationCenter(Vector3 position)
        {
            formationCenter = position;
            RecalculateFormationPositions();
        }

        public void SetFormationRotation(Quaternion rotation)
        {
            formationRotation = rotation;
            RecalculateFormationPositions();
        }

        private void RecalculateFormationPositions()
        {
            if (units.Count == 0) return;

            int rows = Mathf.CeilToInt(Mathf.Sqrt(units.Count));
            int cols = Mathf.CeilToInt((float)units.Count / rows);

            Vector3 startPos = formationCenter - new Vector3(
                (cols - 1) * spacing * 0.5f,
                0,
                (rows - 1) * spacing * 0.5f
            );

            for (int i = 0; i < units.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                Vector3 localPosition = new Vector3(
                    col * spacing,
                    0,
                    row * spacing
                );

                Vector3 worldPosition = startPos + formationRotation * localPosition;
                
                if (maintainFormation)
                {
                    units[i].MoveTo(worldPosition);
                }
                else
                {
                    units[i].transform.position = worldPosition;
                }

                // Rotate unit to match formation rotation
                units[i].transform.rotation = Quaternion.Lerp(
                    units[i].transform.rotation,
                    formationRotation,
                    Time.deltaTime * rotationSpeed
                );
            }
        }

        private void Update()
        {
            if (maintainFormation)
            {
                RecalculateFormationPositions();
            }
        }

        public void SetSpacing(float newSpacing)
        {
            spacing = Mathf.Max(1f, newSpacing);
            RecalculateFormationPositions();
        }

        public void SetMaintainFormation(bool maintain)
        {
            maintainFormation = maintain;
        }

        public List<Unit> GetUnits()
        {
            return new List<Unit>(units);
        }

        private void OnDrawGizmosSelected()
        {
            if (units.Count == 0) return;

            Gizmos.color = Color.yellow;
            
            // Draw formation bounds
            int rows = Mathf.CeilToInt(Mathf.Sqrt(units.Count));
            int cols = Mathf.CeilToInt((float)units.Count / rows);
            
            Vector3 size = new Vector3(
                (cols - 1) * spacing,
                0.1f,
                (rows - 1) * spacing
            );
            
            Gizmos.matrix = Matrix4x4.TRS(formationCenter, formationRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
