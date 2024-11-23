using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace RTS.UI.Components
{
    public class FilterManager : MonoBehaviour
    {
        private Dictionary<string, FilterCondition> activeFilters = new Dictionary<string, FilterCondition>();
        private List<FilterPreset> filterPresets = new List<FilterPreset>();

        public void AddFilter(string filterName, FilterCondition condition)
        {
            if (!activeFilters.ContainsKey(filterName))
            {
                activeFilters.Add(filterName, condition);
            }
        }

        public void RemoveFilter(string filterName)
        {
            if (activeFilters.ContainsKey(filterName))
            {
                activeFilters.Remove(filterName);
            }
        }

        public void SavePreset(string presetName)
        {
            var preset = new FilterPreset
            {
                Name = presetName,
                Conditions = new List<FilterCondition>(activeFilters.Values)
            };
            filterPresets.Add(preset);
        }

        public void LoadPreset(string presetName)
        {
            var preset = filterPresets.Find(p => p.Name == presetName);
            if (preset != null)
            {
                activeFilters.Clear();
                foreach (var condition in preset.Conditions)
                {
                    AddFilter(condition.Name, condition);
                }
            }
        }

        public bool PassesFilters(GameObject unit)
        {
            foreach (var filter in activeFilters.Values)
            {
                if (!filter.Evaluate(unit))
                    return false;
            }
            return true;
        }
    }
}
