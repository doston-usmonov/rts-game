using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.UI.TacticalMap
{
    [Serializable]
    public class FilterPreset
    {
        [SerializeField] private string name;
        [SerializeField] private string description;
        [SerializeField] private List<FilterCriteria> criteria = new List<FilterCriteria>();
        [SerializeField] private KeyCode shortcutKey = KeyCode.None;
        
        public string Name => name;
        public string Description => description;
        public List<FilterCriteria> Criteria => criteria;
        public KeyCode ShortcutKey => shortcutKey;

        public FilterPreset(string name, string description, KeyCode shortcutKey = KeyCode.None)
        {
            this.name = name;
            this.description = description;
            this.shortcutKey = shortcutKey;
        }

        public void AddCriteria(FilterCriteria criterion)
        {
            criteria.Add(criterion);
        }

        public void RemoveCriteria(FilterCriteria criterion)
        {
            criteria.Remove(criterion);
        }

        public void ClearCriteria()
        {
            criteria.Clear();
        }

        public bool Evaluate(object target)
        {
            if (target == null || criteria.Count == 0) return false;

            foreach (var criterion in criteria)
            {
                if (!criterion.Evaluate(target))
                    return false;
            }

            return true;
        }
    }
}
