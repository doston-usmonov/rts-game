using UnityEngine;
using System;

namespace RTS.UI.TacticalMap.Filters
{
    [Serializable]
    public class FilterCriteria
    {
        public enum FilterType
        {
            UnitType,
            FactionType,
            HealthThreshold,
            AmmoThreshold,
            Distance,
            Custom
        }

        public enum ComparisonType
        {
            Equal,
            NotEqual,
            GreaterThan,
            LessThan,
            GreaterThanOrEqual,
            LessThanOrEqual,
            Between,
            Contains,
            NotContains
        }

        [SerializeField] private FilterType filterType;
        [SerializeField] private ComparisonType comparisonType;
        [SerializeField] private string value;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue;
        [SerializeField] private bool isEnabled = true;

        public FilterType Type => filterType;
        public ComparisonType Comparison => comparisonType;
        public string StringValue => value;
        public float MinValue => minValue;
        public float MaxValue => maxValue;
        public bool IsEnabled => isEnabled;

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public void SetValues(string value)
        {
            this.value = value;
        }

        public void SetValues(float min, float max)
        {
            minValue = min;
            maxValue = max;
        }

        public bool Evaluate<T>(T value)
        {
            if (!isEnabled) return true;

            switch (comparisonType)
            {
                case ComparisonType.Equal:
                    return value.Equals(this.value);
                case ComparisonType.NotEqual:
                    return !value.Equals(this.value);
                case ComparisonType.Contains:
                    return value.ToString().Contains(this.value);
                case ComparisonType.NotContains:
                    return !value.ToString().Contains(this.value);
                default:
                    if (value is IComparable comparable && float.TryParse(this.value, out float compareValue))
                    {
                        int comparison = comparable.CompareTo(compareValue);
                        switch (comparisonType)
                        {
                            case ComparisonType.GreaterThan:
                                return comparison > 0;
                            case ComparisonType.LessThan:
                                return comparison < 0;
                            case ComparisonType.GreaterThanOrEqual:
                                return comparison >= 0;
                            case ComparisonType.LessThanOrEqual:
                                return comparison <= 0;
                            case ComparisonType.Between:
                                return comparable.CompareTo(minValue) >= 0 && comparable.CompareTo(maxValue) <= 0;
                        }
                    }
                    return false;
            }
        }
    }
}
