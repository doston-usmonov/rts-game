using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTS.Units;
using RTS.Units.Combat;

namespace RTS.UI
{
    public class GroupTacticalOverlay : MonoBehaviour
    {
        [Header("Status Bars")]
        public GameObject statusBarPrefab;
        public float barWidth = 50f;
        public float barHeight = 5f;
        public float barSpacing = 2f;
        public float statusUpdateInterval = 0.2f;

        [Header("Colors")]
        public Color healthBarColor = Color.green;
        public Color ammoBarColor = Color.blue;
        public Color moraleBarColor = Color.yellow;
        public Color fatigueBarColor = new Color(1f, 0.5f, 0f);

        [Header("Effect Indicators")]
        public GameObject effectIconPrefab;
        public float iconSize = 20f;
        public float iconSpacing = 5f;
        public float iconFadeTime = 0.5f;

        [Header("Combat Visualization")]
        public GameObject engagementLinePrefab;
        public Color engagementLineColor = new Color(1f, 0f, 0f, 0.5f);
        public float engagementLineWidth = 1f;
        public float maxEngagementRange = 50f;

        [Header("Effect Visualization")]
        [SerializeField] private float effectIconSize = 24f;
        [SerializeField] private float effectIconSpacing = 4f;
        [SerializeField] private float effectPulseSpeed = 2f;
        [SerializeField] private float effectPulseIntensity = 0.2f;
        [SerializeField] private Color healEffectColor = new Color(0f, 1f, 0.5f, 1f);
        [SerializeField] private Color damageEffectColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color buffEffectColor = new Color(0.4f, 0.8f, 1f, 1f);
        [SerializeField] private Color debuffEffectColor = new Color(0.8f, 0.4f, 1f, 1f);

        [Header("Combat Prediction")]
        [SerializeField] private CombatPredictor combatPredictor;

        private Dictionary<int, GroupOverlayData> groupOverlays = new Dictionary<int, GroupOverlayData>();
        private Dictionary<int, List<ActiveEffect>> activeEffects = new Dictionary<int, List<ActiveEffect>>();
        private ObjectPool<Image> effectIconPool;
        private Camera mainCamera;
        private float lastUpdateTime;

        private class GroupOverlayData
        {
            public GameObject container;
            public StatusBars statusBars;
            public List<EffectIcon> effectIcons = new List<EffectIcon>();
            public List<EngagementLine> engagementLines = new List<EngagementLine>();
        }

        private class StatusBars
        {
            public Image healthBar;
            public Image ammoBar;
            public Image moraleBar;
            public Image fatigueBar;
            public TextMeshProUGUI statusText;
        }

        private class EffectIcon
        {
            public Image icon;
            public float duration;
            public float startTime;
        }

        private class EngagementLine
        {
            public LineRenderer line;
            public MonoBehaviour target;
            public float damage;
        }

        private class ActiveEffect
        {
            public Image icon;
            public float startTime;
            public float duration;
            public EffectType type;
            public float intensity;
        }

        public enum EffectType
        {
            Heal,
            Damage,
            Buff,
            Debuff
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            InitializeEffectPool();
        }

        private void InitializeEffectPool()
        {
            GameObject poolContainer = new GameObject("EffectIconPool");
            poolContainer.transform.SetParent(transform);
            effectIconPool = poolContainer.AddComponent<ObjectPool<Image>>();
            effectIconPool.Initialize(CreateEffectIcon);
        }

        private Image CreateEffectIcon()
        {
            GameObject iconObj = new GameObject("EffectIcon");
            iconObj.transform.SetParent(transform);
            var icon = iconObj.AddComponent<Image>();
            icon.rectTransform.sizeDelta = new Vector2(effectIconSize, effectIconSize);
            return icon;
        }

        private void Update()
        {
            if (Time.time - lastUpdateTime >= statusUpdateInterval)
            {
                UpdateAllOverlays();
                lastUpdateTime = Time.time;
            }
            UpdateEffects();
        }

        public void CreateGroupOverlay(int groupIndex, HashSet<MonoBehaviour> units)
        {
            if (groupOverlays.ContainsKey(groupIndex))
            {
                DestroyGroupOverlay(groupIndex);
            }

            GameObject container = new GameObject($"GroupOverlay_{groupIndex}");
            container.transform.SetParent(transform);

            var overlayData = new GroupOverlayData
            {
                container = container,
                statusBars = CreateStatusBars(container)
            };

            groupOverlays[groupIndex] = overlayData;
            UpdateGroupStatus(groupIndex, units);
        }

        private StatusBars CreateStatusBars(GameObject container)
        {
            GameObject barsObj = new GameObject("StatusBars");
            barsObj.transform.SetParent(container.transform);

            var bars = new StatusBars();

            // Create health bar
            bars.healthBar = CreateBar(barsObj.transform, "HealthBar", healthBarColor);

            // Create ammo bar
            bars.ammoBar = CreateBar(barsObj.transform, "AmmoBar", ammoBarColor);

            // Create morale bar
            bars.moraleBar = CreateBar(barsObj.transform, "MoraleBar", moraleBarColor);

            // Create fatigue bar
            bars.fatigueBar = CreateBar(barsObj.transform, "FatigueBar", fatigueBarColor);

            // Create status text
            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(barsObj.transform);
            bars.statusText = textObj.AddComponent<TextMeshProUGUI>();
            bars.statusText.fontSize = 12;
            bars.statusText.alignment = TextAlignmentOptions.Center;

            return bars;
        }

        private Image CreateBar(Transform parent, string name, Color color)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent);

            var bar = barObj.AddComponent<Image>();
            bar.color = color;
            var rect = bar.rectTransform;
            rect.sizeDelta = new Vector2(barWidth, barHeight);

            return bar;
        }

        public void UpdateGroupStatus(int groupIndex, HashSet<MonoBehaviour> units)
        {
            if (!groupOverlays.TryGetValue(groupIndex, out var overlayData)) return;

            float avgHealth = 0f, avgAmmo = 0f, avgMorale = 0f, avgFatigue = 0f;
            int count = 0;

            foreach (var unit in units)
            {
                if (unit is Artillery artillery)
                {
                    avgHealth += artillery.CurrentHealth / artillery.MaxHealth;
                    avgAmmo += artillery.CurrentAmmo / artillery.MaxAmmo;
                    avgMorale += artillery.CurrentMorale;
                    avgFatigue += artillery.CurrentFatigue;
                    count++;
                }
                else if (unit is HeavyDefenseBunker bunker)
                {
                    avgHealth += bunker.CurrentHealth / bunker.MaxHealth;
                    avgAmmo += bunker.CurrentAmmo / bunker.MaxAmmo;
                    avgMorale += bunker.CurrentMorale;
                    avgFatigue += bunker.CurrentFatigue;
                    count++;
                }
            }

            if (count > 0)
            {
                avgHealth /= count;
                avgAmmo /= count;
                avgMorale /= count;
                avgFatigue /= count;

                UpdateStatusBars(overlayData.statusBars, avgHealth, avgAmmo, avgMorale, avgFatigue);
                UpdateStatusText(overlayData.statusBars, units.Count);
            }

            UpdateOverlayPosition(groupIndex, units);

            // Update combat predictions if enemies are nearby
            var nearbyEnemies = FindNearbyEnemies(units);
            if (nearbyEnemies.Count > 0 && combatPredictor != null)
            {
                combatPredictor.UpdateCombatPredictions(groupIndex, units, nearbyEnemies);
            }
        }

        private void UpdateStatusBars(StatusBars bars, float health, float ammo, float morale, float fatigue)
        {
            bars.healthBar.fillAmount = health;
            bars.ammoBar.fillAmount = ammo;
            bars.moraleBar.fillAmount = morale;
            bars.fatigueBar.fillAmount = fatigue;

            // Update colors based on status
            bars.healthBar.color = Color.Lerp(Color.red, healthBarColor, health);
            bars.ammoBar.color = Color.Lerp(Color.gray, ammoBarColor, ammo);
            bars.moraleBar.color = Color.Lerp(Color.red, moraleBarColor, morale);
            bars.fatigueBar.color = Color.Lerp(fatigueBarColor, Color.green, 1f - fatigue);
        }

        private void UpdateStatusText(StatusBars bars, int unitCount)
        {
            bars.statusText.text = $"Units: {unitCount}";
        }

        public void AddGroupEffect(int groupIndex, Sprite effectIcon, float duration)
        {
            if (!groupOverlays.TryGetValue(groupIndex, out var overlayData)) return;

            GameObject iconObj = Instantiate(effectIconPrefab, overlayData.container.transform);
            var icon = iconObj.GetComponent<Image>();
            icon.sprite = effectIcon;
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);

            overlayData.effectIcons.Add(new EffectIcon
            {
                icon = icon,
                duration = duration,
                startTime = Time.time
            });

            ArrangeEffectIcons(overlayData);
        }

        private void ArrangeEffectIcons(GroupOverlayData overlayData)
        {
            float xOffset = 0f;
            foreach (var effect in overlayData.effectIcons)
            {
                effect.icon.rectTransform.anchoredPosition = new Vector2(xOffset, 0f);
                xOffset += iconSize + iconSpacing;
            }
        }

        public void UpdateGroupEngagement(int groupIndex, HashSet<MonoBehaviour> units, HashSet<MonoBehaviour> targets)
        {
            if (!groupOverlays.TryGetValue(groupIndex, out var overlayData)) return;

            // Clear old engagement lines
            foreach (var engagement in overlayData.engagementLines)
            {
                Destroy(engagement.line.gameObject);
            }
            overlayData.engagementLines.Clear();

            // Create new engagement lines
            foreach (var unit in units)
            {
                foreach (var target in targets)
                {
                    if (Vector3.Distance(unit.transform.position, target.transform.position) <= maxEngagementRange)
                    {
                        CreateEngagementLine(overlayData, unit, target);
                    }
                }
            }
        }

        private void CreateEngagementLine(GroupOverlayData overlayData, MonoBehaviour source, MonoBehaviour target)
        {
            GameObject lineObj = Instantiate(engagementLinePrefab, overlayData.container.transform);
            var line = lineObj.GetComponent<LineRenderer>();

            line.startWidth = engagementLineWidth;
            line.endWidth = engagementLineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = engagementLineColor;
            line.endColor = new Color(engagementLineColor.r, engagementLineColor.g, engagementLineColor.b, 0f);

            overlayData.engagementLines.Add(new EngagementLine
            {
                line = line,
                target = target,
                damage = CalculateDamage(source, target)
            });

            UpdateEngagementLine(line, source.transform.position, target.transform.position);
        }

        private float CalculateDamage(MonoBehaviour source, MonoBehaviour target)
        {
            // Calculate potential damage based on unit types and status
            float damage = 0f;

            if (source is Artillery artillery)
            {
                damage = artillery.CurrentAmmo * artillery.DamageMultiplier;
            }
            else if (source is HeavyDefenseBunker bunker)
            {
                damage = bunker.CurrentAmmo * bunker.DamageMultiplier;
            }

            return damage;
        }

        private void UpdateEngagementLine(LineRenderer line, Vector3 start, Vector3 end)
        {
            Vector3[] points = new Vector3[2];
            points[0] = start;
            points[1] = end;
            line.positionCount = 2;
            line.SetPositions(points);
        }

        private void UpdateOverlayPosition(int groupIndex, HashSet<MonoBehaviour> units)
        {
            if (!groupOverlays.TryGetValue(groupIndex, out var overlayData)) return;

            Vector3 groupCenter = Vector3.zero;
            foreach (var unit in units)
            {
                groupCenter += unit.transform.position;
            }
            groupCenter /= units.Count;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(groupCenter);
            overlayData.container.transform.position = screenPos;
        }

        private void UpdateAllOverlays()
        {
            foreach (var overlay in groupOverlays.Values)
            {
                // Update effect icons
                for (int i = overlay.effectIcons.Count - 1; i >= 0; i--)
                {
                    var effect = overlay.effectIcons[i];
                    float elapsed = Time.time - effect.startTime;

                    if (elapsed >= effect.duration)
                    {
                        Destroy(effect.icon.gameObject);
                        overlay.effectIcons.RemoveAt(i);
                    }
                    else if (elapsed >= effect.duration - iconFadeTime)
                    {
                        float alpha = 1f - (elapsed - (effect.duration - iconFadeTime)) / iconFadeTime;
                        var color = effect.icon.color;
                        effect.icon.color = new Color(color.r, color.g, color.b, alpha);
                    }
                }

                // Update engagement lines
                foreach (var engagement in overlay.engagementLines)
                {
                    if (engagement.target != null)
                    {
                        UpdateEngagementLine(engagement.line, 
                            engagement.line.transform.position, 
                            engagement.target.transform.position);
                    }
                }
            }
        }

        public void AddEffect(int groupIndex, Sprite effectIcon, EffectType type, float duration, float intensity = 1f)
        {
            if (!activeEffects.ContainsKey(groupIndex))
            {
                activeEffects[groupIndex] = new List<ActiveEffect>();
            }

            var effect = new ActiveEffect
            {
                icon = effectIconPool.Get(),
                startTime = Time.time,
                duration = duration,
                type = type,
                intensity = intensity
            };

            effect.icon.sprite = effectIcon;
            effect.icon.color = GetEffectColor(type);
            activeEffects[groupIndex].Add(effect);
            ArrangeEffectIcons(groupIndex);
        }

        private Color GetEffectColor(EffectType type)
        {
            switch (type)
            {
                case EffectType.Heal: return healEffectColor;
                case EffectType.Damage: return damageEffectColor;
                case EffectType.Buff: return buffEffectColor;
                case EffectType.Debuff: return debuffEffectColor;
                default: return Color.white;
            }
        }

        private void UpdateEffects()
        {
            foreach (var groupEffects in activeEffects)
            {
                int groupIndex = groupEffects.Key;
                var effects = groupEffects.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    var effect = effects[i];
                    float elapsed = Time.time - effect.startTime;

                    if (elapsed >= effect.duration)
                    {
                        effectIconPool.ReturnToPool(effect.icon);
                        effects.RemoveAt(i);
                        continue;
                    }

                    // Update effect visualization
                    float pulseValue = Mathf.Sin(Time.time * effectPulseSpeed) * effectPulseIntensity;
                    Color baseColor = GetEffectColor(effect.type);
                    effect.icon.color = new Color(
                        baseColor.r + pulseValue,
                        baseColor.g + pulseValue,
                        baseColor.b + pulseValue,
                        baseColor.a
                    );

                    // Scale effect based on remaining duration
                    float remainingTime = 1f - (elapsed / effect.duration);
                    effect.icon.transform.localScale = Vector3.one * (1f + pulseValue * effect.intensity);
                }

                if (effects.Count > 0)
                {
                    ArrangeEffectIcons(groupIndex);
                }
            }
        }

        private void ArrangeEffectIcons(int groupIndex)
        {
            if (!activeEffects.TryGetValue(groupIndex, out var effects)) return;

            float totalWidth = (effects.Count - 1) * effectIconSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                effect.icon.rectTransform.anchoredPosition = new Vector2(
                    startX + i * effectIconSpacing,
                    effectIconSize
                );
            }
        }

        private HashSet<MonoBehaviour> FindNearbyEnemies(HashSet<MonoBehaviour> units)
        {
            HashSet<MonoBehaviour> enemies = new HashSet<MonoBehaviour>();
            float detectionRange = 50f;

            foreach (var unit in units)
            {
                Collider[] colliders = Physics.OverlapSphere(unit.transform.position, detectionRange);
                foreach (var collider in colliders)
                {
                    if (collider.TryGetComponent<EnemyUnit>(out var enemy))
                    {
                        enemies.Add(enemy);
                    }
                }
            }

            return enemies;
        }

        public void DestroyGroupOverlay(int groupIndex)
        {
            if (groupOverlays.TryGetValue(groupIndex, out var overlayData))
            {
                Destroy(overlayData.container);
                groupOverlays.Remove(groupIndex);
            }

            // Clean up effects
            if (activeEffects.TryGetValue(groupIndex, out var effects))
            {
                foreach (var effect in effects)
                {
                    effectIconPool.ReturnToPool(effect.icon);
                }
                activeEffects.Remove(groupIndex);
            }

            // Clean up combat predictions
            if (combatPredictor != null)
            {
                combatPredictor.OnGroupDestroyed(groupIndex);
            }
        }

        private void OnDestroy()
        {
            foreach (var overlay in groupOverlays.Values)
            {
                Destroy(overlay.container);
            }

            // Clean up effect pool
            if (effectIconPool != null)
            {
                Destroy(effectIconPool.gameObject);
            }
        }
    }
}
