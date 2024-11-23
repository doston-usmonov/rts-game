using System;
using UnityEngine;

namespace RTS.UI.TacticalMap
{
    [Serializable]
    public class FilterCriteria
    {
        public enum ComparisonType
        {
            Equal,
            NotEqual,
            GreaterThan,
            LessThan,
            GreaterThanOrEqual,
            LessThanOrEqual,
            Contains,
            NotContains
        }

        public enum ValueType
        {
            Number,
            Text,
            Boolean
        }

        [SerializeField] private string propertyName;
        [SerializeField] private ComparisonType comparison;
        [SerializeField] private ValueType valueType;
        [SerializeField] private string value;
        
        public string PropertyName => propertyName;
        public ComparisonType Comparison => comparison;
        public ValueType ValueType => valueType;
        public string Value => value;

        public FilterCriteria(string propertyName, ComparisonType comparison, ValueType valueType, string value)
        {
            this.propertyName = propertyName;
            this.comparison = comparison;
            this.valueType = valueType;
            this.value = value;
        }

        public bool Evaluate(object target)
        {
            if (target == null) return false;

            var property = target.GetType().GetProperty(propertyName);
            if (property == null) return false;

            var targetValue = property.GetValue(target);
            if (targetValue == null) return false;

            switch (valueType)
            {
                case ValueType.Number:
                    return EvaluateNumber(targetValue);
                case ValueType.Text:
                    return EvaluateText(targetValue);
                case ValueType.Boolean:
                    return EvaluateBoolean(targetValue);
                default:
                    return false;
            }
        }

        private bool EvaluateNumber(object targetValue)
        {
            if (!float.TryParse(value, out float filterValue)) return false;
            float targetNumber = Convert.ToSingle(targetValue);

            switch (comparison)
            {
                case ComparisonType.Equal:
                    return Math.Abs(targetNumber - filterValue) < float.Epsilon;
                case ComparisonType.NotEqual:
                    return Math.Abs(targetNumber - filterValue) > float.Epsilon;
                case ComparisonType.GreaterThan:
                    return targetNumber > filterValue;
                case ComparisonType.LessThan:
                    return targetNumber < filterValue;
                case ComparisonType.GreaterThanOrEqual:
                    return targetNumber >= filterValue;
                case ComparisonType.LessThanOrEqual:
                    return targetNumber <= filterValue;
                default:
                    return false;
            }
        }

        private bool EvaluateText(object targetValue)
        {
            string targetText = targetValue.ToString();

            switch (comparison)
            {
                case ComparisonType.Equal:
                    return targetText.Equals(value, StringComparison.OrdinalIgnoreCase);
                case ComparisonType.NotEqual:
                    return !targetText.Equals(value, StringComparison.OrdinalIgnoreCase);
                case ComparisonType.Contains:
                    return targetText.Contains(value, StringComparison.OrdinalIgnoreCase);
                case ComparisonType.NotContains:
                    return !targetText.Contains(value, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        private bool EvaluateBoolean(object targetValue)
        {
            if (!bool.TryParse(value, out bool filterValue)) return false;
            bool targetBool = Convert.ToBoolean(targetValue);

            switch (comparison)
            {
                case ComparisonType.Equal:
                    return targetBool == filterValue;
                case ComparisonType.NotEqual:
                    return targetBool != filterValue;
                default:
                    return false;
            }
        }
    }
}
