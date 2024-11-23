using UnityEngine;
using System.Collections.Generic;

namespace RTS.Combat
{
    public class DamageHandler : MonoBehaviour
    {
        private Dictionary<string, float> damageReductions = new Dictionary<string, float>();

        public void AddDamageReduction(string source, float reduction)
        {
            damageReductions[source] = Mathf.Clamp01(reduction);
        }

        public void RemoveDamageReduction(string source)
        {
            if (damageReductions.ContainsKey(source))
            {
                damageReductions.Remove(source);
            }
        }

        public float CalculateDamageReduction(float incomingDamage)
        {
            float totalReduction = 0f;
            foreach (var reduction in damageReductions.Values)
            {
                // Stack damage reductions multiplicatively
                totalReduction = 1f - (1f - totalReduction) * (1f - reduction);
            }

            return incomingDamage * (1f - totalReduction);
        }

        public void ClearAllReductions()
        {
            damageReductions.Clear();
        }
    }
}
