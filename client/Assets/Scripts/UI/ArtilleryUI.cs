using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Units;
using RTS.Units.Combat;

namespace RTS.UI
{
    public class ArtilleryUI : MonoBehaviour
    {
        [Header("References")]
        public Artillery artillery;
        public Canvas worldSpaceCanvas;
        
        [Header("Status Indicators")]
        public Image ammoBar;
        public Image shieldBar;
        public GameObject lowAmmoWarning;
        public GameObject resupplyIndicator;
        
        [Header("Text Displays")]
        public TextMeshProUGUI ammoText;
        public TextMeshProUGUI statusText;
        
        [Header("Visual Effects")]
        public GameObject shieldEffectOverlay;
        public ParticleSystem lowAmmoEffect;
        public ParticleSystem resupplyEffect;
        
        [Header("Animation")]
        public Animator uiAnimator;
        public float updateInterval = 0.1f;
        
        [Header("Colors")]
        public Color normalAmmoColor = Color.green;
        public Color lowAmmoColor = Color.yellow;
        public Color criticalAmmoColor = Color.red;
        public Color shieldActiveColor = new Color(0.4f, 0.6f, 1f, 0.8f);

        private float nextUpdateTime;
        private bool isLowAmmoWarningActive;
        private bool isResupplyActive;

        private void Start()
        {
            if (artillery == null)
            {
                artillery = GetComponentInParent<Artillery>();
            }

            if (worldSpaceCanvas != null)
            {
                // Ensure the canvas faces the camera
                worldSpaceCanvas.worldCamera = Camera.main;
            }

            UpdateUI();
        }

        private void Update()
        {
            if (Time.time >= nextUpdateTime)
            {
                UpdateUI();
                nextUpdateTime = Time.time + updateInterval;
            }
        }

        private void UpdateUI()
        {
            if (artillery == null) return;

            UpdateAmmoDisplay();
            UpdateShieldStatus();
            UpdateStatusText();
            UpdateVisualEffects();
        }

        private void UpdateAmmoDisplay()
        {
            float ammoPercentage = artillery.GetAmmoPercentage();

            // Update ammo bar
            if (ammoBar != null)
            {
                ammoBar.fillAmount = ammoPercentage / 100f;
                
                // Update color based on ammo level
                ammoBar.color = GetAmmoBarColor(ammoPercentage);
            }

            // Update ammo text
            if (ammoText != null)
            {
                ammoText.text = $"{ammoPercentage:F0}%";
            }

            // Handle low ammo warning
            bool isLowAmmo = artillery.IsLowOnAmmo();
            if (isLowAmmo != isLowAmmoWarningActive)
            {
                isLowAmmoWarningActive = isLowAmmo;
                if (lowAmmoWarning != null)
                {
                    lowAmmoWarning.SetActive(isLowAmmo);
                }
                if (lowAmmoEffect != null)
                {
                    if (isLowAmmo)
                        lowAmmoEffect.Play();
                    else
                        lowAmmoEffect.Stop();
                }
                
                if (uiAnimator != null)
                {
                    uiAnimator.SetBool("LowAmmo", isLowAmmo);
                }
            }
        }

        private void UpdateShieldStatus()
        {
            if (shieldBar != null)
            {
                bool hasShield = artillery.GetComponent<DamageHandler>()?.GetDamageReductions().ContainsKey("BunkerShield") ?? false;
                shieldBar.gameObject.SetActive(hasShield);
                
                if (hasShield && shieldEffectOverlay != null)
                {
                    shieldEffectOverlay.SetActive(true);
                    shieldBar.color = shieldActiveColor;
                }
                else if (shieldEffectOverlay != null)
                {
                    shieldEffectOverlay.SetActive(false);
                }
            }
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;

            string status = "Ready";
            if (artillery.IsDeployed)
            {
                status = "Deployed";
            }
            if (artillery.IsLowOnAmmo())
            {
                status = "Low Ammo";
            }
            if (isResupplyActive)
            {
                status = "Resupplying";
            }

            statusText.text = status;
        }

        private void UpdateVisualEffects()
        {
            // Handle resupply effect
            if (resupplyEffect != null)
            {
                bool shouldShowResupply = artillery.IsResupplying;
                if (shouldShowResupply != isResupplyActive)
                {
                    isResupplyActive = shouldShowResupply;
                    if (isResupplyActive)
                        resupplyEffect.Play();
                    else
                        resupplyEffect.Stop();
                }
            }
        }

        private Color GetAmmoBarColor(float percentage)
        {
            if (percentage <= 25f)
                return criticalAmmoColor;
            if (percentage <= artillery.lowAmmoThreshold)
                return lowAmmoColor;
            return normalAmmoColor;
        }

        public void ShowDeploymentRadius(bool show)
        {
            // Implementation for showing deployment radius visualization
        }

        public void ShowResupplyRadius(bool show)
        {
            // Implementation for showing resupply radius visualization
        }

        private void OnEnable()
        {
            if (uiAnimator != null)
            {
                uiAnimator.SetTrigger("Show");
            }
        }

        private void OnDisable()
        {
            if (uiAnimator != null)
            {
                uiAnimator.SetTrigger("Hide");
            }
        }
    }
}
