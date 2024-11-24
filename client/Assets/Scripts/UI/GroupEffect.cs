using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
    public class GroupEffect
    {
        public Sprite icon;
        public float duration;
        public float intensity;
        public float startTime;
        public EffectType type;

        public GroupEffect(Sprite icon, float duration, float intensity = 1f, EffectType type = EffectType.Default)
        {
            this.icon = icon;
            this.duration = duration;
            this.intensity = intensity;
            this.startTime = Time.time;
            this.type = type;
        }
    }

    public enum EffectType
    {
        Default,
        Buff,
        Debuff,
        Damage,
        Healing,
        Status
    }
}
