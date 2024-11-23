using UnityEngine;

namespace RTS.Units.Environment
{
    [System.Serializable]
    public class UnitEnvironmentState
    {
        [Header("Current State")]
        [SerializeField] private float fatigue;
        [SerializeField] private float coldEffect;
        [SerializeField] private float heatEffect;
        [SerializeField] private float moistureEffect;

        [Header("Effect Coefficients")]
        [SerializeField] private float fatigueCoefficient = 0.1f;
        [SerializeField] private float coldCoefficient = 0.15f;
        [SerializeField] private float heatCoefficient = 0.12f;
        [SerializeField] private float moistureCoefficient = 0.08f;

        [Header("Recovery Parameters")]
        [SerializeField] private float recoveryRate = 0.05f;
        [SerializeField] private float maxFatigue = 100f;
        [SerializeField] private float maxColdEffect = 100f;
        [SerializeField] private float maxHeatEffect = 100f;

        [Header("Unit Type Resistance")]
        [SerializeField] private float coldResistance = 0.8f;
        [SerializeField] private float heatResistance = 0.9f;
        [SerializeField] private float moistureResistance = 0.95f;

        public float Fatigue => fatigue;
        public float ColdEffect => coldEffect;
        public float HeatEffect => heatEffect;
        public float MoistureEffect => moistureEffect;

        public void UpdateEnvironmentalEffects(float deltaTime, float temperature, float moisture)
        {
            // Update fatigue
            fatigue = Mathf.Clamp(fatigue + (fatigueCoefficient * deltaTime), 0f, maxFatigue);

            // Temperature effects
            if (temperature < 0)
            {
                coldEffect = Mathf.Clamp(
                    coldEffect + (coldCoefficient * (1f - coldResistance) * Mathf.Abs(temperature) * deltaTime),
                    0f,
                    maxColdEffect
                );
                heatEffect = Mathf.Max(0f, heatEffect - (recoveryRate * deltaTime));
            }
            else
            {
                heatEffect = Mathf.Clamp(
                    heatEffect + (heatCoefficient * (1f - heatResistance) * temperature * deltaTime),
                    0f,
                    maxHeatEffect
                );
                coldEffect = Mathf.Max(0f, coldEffect - (recoveryRate * deltaTime));
            }

            // Moisture effects
            moistureEffect = Mathf.Clamp(
                moistureEffect + (moistureCoefficient * (1f - moistureResistance) * moisture * deltaTime),
                0f,
                100f
            );
        }

        public void Rest(float deltaTime)
        {
            fatigue = Mathf.Max(0f, fatigue - (recoveryRate * deltaTime));
        }

        public float GetOverallEffectMultiplier()
        {
            float fatigueMultiplier = 1f - (fatigue / maxFatigue);
            float temperatureMultiplier = 1f - ((coldEffect + heatEffect) / (maxColdEffect + maxHeatEffect));
            float moistureMultiplier = 1f - (moistureEffect / 100f);

            return fatigueMultiplier * temperatureMultiplier * moistureMultiplier;
        }

        public void Reset()
        {
            fatigue = 0f;
            coldEffect = 0f;
            heatEffect = 0f;
            moistureEffect = 0f;
        }
    }
}
