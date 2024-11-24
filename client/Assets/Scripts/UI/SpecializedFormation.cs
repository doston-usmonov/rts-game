using UnityEngine;
using System.Collections.Generic;
using RTS.Units;
using RTS.Units.Combat;

namespace RTS.UI
{
    public enum FormationType
    {
        Line,
        Column,
        Wedge,
        Square,
        Circle
    }

    public class SpecializedFormation : MonoBehaviour
    {
        public enum UnitType
        {
            Artillery,
            Defense,
            Support,
            Standard
        }

        [Header("Formation Settings")]
        [SerializeField] private FormationType formationType = FormationType.Line;
        [SerializeField] private float spacing = 2f;
        [SerializeField] private float depth = 1f;
        
        [Header("Combat Bonuses")]
        [SerializeField] private float formationDamageBonus = 0.2f;
        [SerializeField] private float formationArmorBonus = 0.15f;
        [SerializeField] private float formationSpeedBonus = 0.1f;

        private List<HeavyUnit> units = new List<HeavyUnit>();
        private Vector3 formationCenter;
        private bool isInFormation = false;

        private void Awake()
        {
            InitializeFormation();
        }

        private void InitializeFormation()
        {
            // Find all units in the formation
            units.Clear();
            var foundUnits = GetComponentsInChildren<HeavyUnit>();
            units.AddRange(foundUnits);

            // Calculate initial formation center
            UpdateFormationCenter();
        }

        private void UpdateFormationCenter()
        {
            if (units.Count == 0) return;

            Vector3 sum = Vector3.zero;
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    sum += unit.transform.position;
                }
            }

            formationCenter = sum / units.Count;
        }

        public void SetFormationType(FormationType type)
        {
            formationType = type;
            ArrangeUnits();
        }

        private void ArrangeUnits()
        {
            if (units.Count == 0) return;

            switch (formationType)
            {
                case FormationType.Line:
                    ArrangeInLine();
                    break;
                case FormationType.Column:
                    ArrangeInColumn();
                    break;
                case FormationType.Wedge:
                    ArrangeInWedge();
                    break;
                case FormationType.Square:
                    ArrangeInBox();
                    break;
                case FormationType.Circle:
                    // TO DO: implement circle formation
                    break;
            }

            isInFormation = true;
        }

        private void ArrangeInLine()
        {
            float totalWidth = (units.Count - 1) * spacing;
            Vector3 startPos = formationCenter - new Vector3(totalWidth / 2, 0, 0);

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null)
                {
                    Vector3 targetPos = startPos + new Vector3(i * spacing, 0, 0);
                    units[i].MoveTo(targetPos);
                }
            }
        }

        private void ArrangeInColumn()
        {
            float totalDepth = (units.Count - 1) * depth;
            Vector3 startPos = formationCenter - new Vector3(0, 0, totalDepth / 2);

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null)
                {
                    Vector3 targetPos = startPos + new Vector3(0, 0, i * depth);
                    units[i].MoveTo(targetPos);
                }
            }
        }

        private void ArrangeInWedge()
        {
            int rows = Mathf.CeilToInt(Mathf.Sqrt(units.Count));
            int currentUnit = 0;

            for (int row = 0; row < rows && currentUnit < units.Count; row++)
            {
                int unitsInRow = Mathf.Min(row + 1, units.Count - currentUnit);
                float rowWidth = (unitsInRow - 1) * spacing;
                Vector3 rowStart = formationCenter + new Vector3(-rowWidth / 2, 0, -row * depth);

                for (int i = 0; i < unitsInRow; i++)
                {
                    if (units[currentUnit] != null)
                    {
                        Vector3 targetPos = rowStart + new Vector3(i * spacing, 0, 0);
                        units[currentUnit].MoveTo(targetPos);
                    }
                    currentUnit++;
                }
            }
        }

        private void ArrangeInBox()
        {
            int sideLength = Mathf.CeilToInt(Mathf.Sqrt(units.Count));
            float totalWidth = (sideLength - 1) * spacing;
            float totalDepth = (sideLength - 1) * depth;
            Vector3 startPos = formationCenter - new Vector3(totalWidth / 2, 0, totalDepth / 2);

            int currentUnit = 0;
            for (int row = 0; row < sideLength && currentUnit < units.Count; row++)
            {
                for (int col = 0; col < sideLength && currentUnit < units.Count; col++)
                {
                    if (units[currentUnit] != null)
                    {
                        Vector3 targetPos = startPos + new Vector3(col * spacing, 0, row * depth);
                        units[currentUnit].MoveTo(targetPos);
                    }
                    currentUnit++;
                }
            }
        }

        public void AddUnit(HeavyUnit unit)
        {
            if (unit != null && !units.Contains(unit))
            {
                units.Add(unit);
                UpdateFormationCenter();
                ArrangeUnits();
            }
        }

        public void RemoveUnit(HeavyUnit unit)
        {
            if (unit != null && units.Contains(unit))
            {
                units.Remove(unit);
                UpdateFormationCenter();
                ArrangeUnits();
            }
        }

        public float GetFormationBonus(BonusType type)
        {
            if (!isInFormation) return 0f;

            switch (type)
            {
                case BonusType.Damage:
                    return formationDamageBonus;
                case BonusType.Armor:
                    return formationArmorBonus;
                case BonusType.Speed:
                    return formationSpeedBonus;
                case BonusType.Vision:
                    return 0f;
                case BonusType.Morale:
                    return 0f;
                default:
                    return 0f;
            }
        }

        private void Update()
        {
            if (!isInFormation || units.Count == 0) return;

            // Check if formation is still maintained
            bool formationMaintained = CheckFormationIntegrity();
            if (!formationMaintained)
            {
                isInFormation = false;
                // Notify units that formation is broken
                foreach (var unit in units)
                {
                    if (unit != null)
                    {
                        unit.OnFormationBroken();
                    }
                }
            }
        }

        private bool CheckFormationIntegrity()
        {
            if (units.Count < 2) return false;

            UpdateFormationCenter();
            
            // Check if all units are within acceptable distance of their assigned positions
            foreach (var unit in units)
            {
                if (unit == null) continue;
                
                float distanceToCenter = Vector3.Distance(unit.transform.position, formationCenter);
                if (distanceToCenter > spacing * 2f)
                {
                    return false;
                }
            }
            
            return true;
        }

        public void RotateFormation(float angle)
        {
            if (units.Count == 0) return;

            // Rotate formation around center
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    Vector3 directionFromCenter = unit.transform.position - formationCenter;
                    Vector3 rotatedDirection = rotation * directionFromCenter;
                    Vector3 newPosition = formationCenter + rotatedDirection;
                    unit.MoveTo(newPosition);
                    unit.transform.rotation *= rotation;
                }
            }
        }

        public void MoveFormation(Vector3 targetPosition)
        {
            if (units.Count == 0) return;

            // Calculate offset from current formation center
            Vector3 offset = targetPosition - formationCenter;
            formationCenter = targetPosition;

            // Move all units maintaining their relative positions
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    Vector3 newPosition = unit.transform.position + offset;
                    unit.MoveTo(newPosition);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!isInFormation || units.Count == 0) return;

            // Draw formation bounds
            Gizmos.color = Color.yellow;
            switch (formationType)
            {
                case FormationType.Line:
                    DrawLineFormationGizmos();
                    break;
                case FormationType.Column:
                    DrawColumnFormationGizmos();
                    break;
                case FormationType.Wedge:
                    DrawWedgeFormationGizmos();
                    break;
                case FormationType.Square:
                    DrawSquareFormationGizmos();
                    break;
                case FormationType.Circle:
                    DrawCircleFormationGizmos();
                    break;
            }
        }

        private void DrawLineFormationGizmos()
        {
            float totalWidth = (units.Count - 1) * spacing;
            Vector3 startPos = formationCenter - new Vector3(totalWidth / 2, 0, 0);
            Vector3 endPos = formationCenter + new Vector3(totalWidth / 2, 0, 0);
            Gizmos.DrawLine(startPos, endPos);
        }

        private void DrawColumnFormationGizmos()
        {
            float totalDepth = (units.Count - 1) * depth;
            Vector3 startPos = formationCenter - new Vector3(0, 0, totalDepth / 2);
            Vector3 endPos = formationCenter + new Vector3(0, 0, totalDepth / 2);
            Gizmos.DrawLine(startPos, endPos);
        }

        private void DrawWedgeFormationGizmos()
        {
            int rows = Mathf.CeilToInt(Mathf.Sqrt(units.Count));
            Vector3 tip = formationCenter + new Vector3(0, 0, rows * depth / 2);
            Vector3 leftBase = formationCenter - new Vector3(rows * spacing / 2, 0, -rows * depth / 2);
            Vector3 rightBase = formationCenter + new Vector3(rows * spacing / 2, 0, -rows * depth / 2);
            
            Gizmos.DrawLine(tip, leftBase);
            Gizmos.DrawLine(tip, rightBase);
            Gizmos.DrawLine(leftBase, rightBase);
        }

        private void DrawSquareFormationGizmos()
        {
            int sideLength = Mathf.CeilToInt(Mathf.Sqrt(units.Count));
            float halfWidth = sideLength * spacing / 2;
            float halfDepth = sideLength * depth / 2;
            
            Vector3[] corners = new Vector3[4]
            {
                formationCenter + new Vector3(-halfWidth, 0, -halfDepth),
                formationCenter + new Vector3(halfWidth, 0, -halfDepth),
                formationCenter + new Vector3(halfWidth, 0, halfDepth),
                formationCenter + new Vector3(-halfWidth, 0, halfDepth)
            };
            
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
        }

        private void DrawCircleFormationGizmos()
        {
            float radius = spacing * units.Count / (2 * Mathf.PI);
            int segments = 32;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                
                Vector3 pos1 = formationCenter + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                Vector3 pos2 = formationCenter + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
                
                Gizmos.DrawLine(pos1, pos2);
            }
        }
    }

    public enum BonusType
    {
        Damage,
        Armor,
        Speed,
        Vision,
        Morale
    }
}
