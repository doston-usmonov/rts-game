using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Buildings;
using RTS.Units;
using System.Collections.Generic;

namespace RTS.UI
{
    public class BunkerUI : MonoBehaviour
    {
        [Header("References")]
        public HeavyDefenseBunker bunker;
        
        [Header("Status Panels")]
        public GameObject mainPanel;
        public GameObject upgradePanel;
        
        [Header("Status Indicators")]
        public Image healthBar;
        public Image armorBar;
        public Image fortifyStatusBar;
        public Image upgradeProgressBar;
        
        [Header("Garrison Display")]
        public Transform garrisonContainer;
        public GameObject garrisonUnitPrefab;
        public TextMeshProUGUI garrisonCountText;
        
        [Header("Buttons")]
        public Button fortifyButton;
        public Button laserUpgradeButton;
        public Button missileUpgradeButton;
        
        [Header("Cooldown Overlays")]
        public Image fortifyCooldownOverlay;
        public TextMeshProUGUI fortifyCooldownText;

        [Header("Synergy Display")]
        public Transform artillerySynergyContainer;
        public GameObject artillerySynergyPrefab;
        public Image shieldRangeIndicator;
        public Image resupplyRangeIndicator;
        
        [Header("Synergy Effects")]
        public ParticleSystem shieldActivationEffect;
        public ParticleSystem resupplyEffect;
        public LineRenderer synergyBeam;

        private List<GameObject> activeSynergyDisplays = new List<GameObject>();
        private Dictionary<Artillery, Image> artilleryRangeIndicators = new Dictionary<Artillery, Image>();

        private void Start()
        {
            if (bunker == null)
            {
                bunker = GetComponentInParent<HeavyDefenseBunker>();
            }

            // Initialize UI elements
            SetupButtons();
            UpdateUI();

            // Initialize synergy visualization
            if (synergyBeam != null)
            {
                synergyBeam.enabled = false;
            }
        }

        private void Update()
        {
            if (bunker == null) return;

            UpdateStatusBars();
            UpdateGarrisonDisplay();
            UpdateCooldowns();
            UpdateUpgradeProgress();
            UpdateSynergyDisplay();
        }

        private void SetupButtons()
        {
            if (fortifyButton != null)
            {
                fortifyButton.onClick.AddListener(() => bunker.ActivateFortifyMode());
            }

            if (laserUpgradeButton != null)
            {
                laserUpgradeButton.onClick.AddListener(() => 
                    bunker.StartWeaponUpgrade(HeavyDefenseBunker.WeaponUpgradeType.LaserCannon));
            }

            if (missileUpgradeButton != null)
            {
                missileUpgradeButton.onClick.AddListener(() => 
                    bunker.StartWeaponUpgrade(HeavyDefenseBunker.WeaponUpgradeType.MissileLauncher));
            }
        }

        private void UpdateStatusBars()
        {
            if (healthBar != null)
            {
                healthBar.fillAmount = bunker.Health / bunker.MaxHealth;
            }

            if (armorBar != null)
            {
                armorBar.fillAmount = bunker.Armor / (bunker.BaseArmor * 2); // Assuming max armor is double base armor
            }
        }

        private void UpdateGarrisonDisplay()
        {
            if (garrisonCountText != null)
            {
                garrisonCountText.text = $"Garrison: {bunker.GarrisonedUnits.Count}/{bunker.MaxGarrisonCapacity}";
            }

            // Update individual unit displays in garrison container
            // This would need to be implemented based on your specific UI design
        }

        private void UpdateCooldowns()
        {
            if (fortifyCooldownOverlay != null && fortifyCooldownText != null)
            {
                if (bunker.IsFortifyOnCooldown)
                {
                    float remainingCooldown = bunker.FortifyCooldownRemaining;
                    fortifyCooldownOverlay.fillAmount = remainingCooldown / bunker.FortifyCooldown;
                    fortifyCooldownText.text = Mathf.Ceil(remainingCooldown).ToString();
                    fortifyCooldownOverlay.gameObject.SetActive(true);
                    fortifyCooldownText.gameObject.SetActive(true);
                }
                else
                {
                    fortifyCooldownOverlay.gameObject.SetActive(false);
                    fortifyCooldownText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateUpgradeProgress()
        {
            if (upgradeProgressBar != null)
            {
                upgradeProgressBar.fillAmount = bunker.UpgradeProgress;
                upgradeProgressBar.gameObject.SetActive(bunker.IsUpgrading);
            }

            // Update upgrade button states
            if (laserUpgradeButton != null)
            {
                laserUpgradeButton.interactable = !bunker.IsUpgrading && 
                    bunker.CurrentWeaponUpgrade != HeavyDefenseBunker.WeaponUpgradeType.LaserCannon;
            }

            if (missileUpgradeButton != null)
            {
                missileUpgradeButton.interactable = !bunker.IsUpgrading && 
                    bunker.CurrentWeaponUpgrade != HeavyDefenseBunker.WeaponUpgradeType.MissileLauncher;
            }
        }

        private void UpdateSynergyDisplay()
        {
            var nearbyArtillery = Physics.OverlapSphere(bunker.transform.position, bunker.GetComponent<HeavyAssaultTactics>()?.artilleryShieldRadius ?? 20f)
                .Select(col => col.GetComponent<Artillery>())
                .Where(art => art != null && art.factionType == bunker.factionType)
                .ToList();

            // Update or create synergy displays for each artillery unit
            foreach (var artillery in nearbyArtillery)
            {
                UpdateArtillerySynergyDisplay(artillery);
            }

            // Remove displays for artillery units no longer in range
            CleanupSynergyDisplays(nearbyArtillery);

            // Update range indicators
            UpdateRangeIndicators();
        }

        private void UpdateArtillerySynergyDisplay(Artillery artillery)
        {
            GameObject synergyDisplay = activeSynergyDisplays.FirstOrDefault(d => d.GetComponent<ArtillerySynergyDisplay>()?.artillery == artillery);
            
            if (synergyDisplay == null)
            {
                // Create new display
                synergyDisplay = Instantiate(artillerySynergyPrefab, artillerySynergyContainer);
                var display = synergyDisplay.GetComponent<ArtillerySynergyDisplay>();
                display.Initialize(artillery, bunker);
                activeSynergyDisplays.Add(synergyDisplay);

                // Create range indicator
                CreateRangeIndicator(artillery);
            }

            // Update position and status
            Vector3 screenPos = Camera.main.WorldToScreenPoint(artillery.transform.position);
            synergyDisplay.transform.position = screenPos;

            // Update synergy beam
            if (synergyBeam != null && artillery.IsDeployed)
            {
                synergyBeam.enabled = true;
                Vector3[] positions = new Vector3[] { 
                    bunker.transform.position, 
                    artillery.transform.position 
                };
                synergyBeam.SetPositions(positions);
            }
        }

        private void CreateRangeIndicator(Artillery artillery)
        {
            if (artilleryRangeIndicators.ContainsKey(artillery)) return;

            var indicator = Instantiate(shieldRangeIndicator, transform);
            indicator.transform.position = artillery.transform.position;
            artilleryRangeIndicators[artillery] = indicator;
        }

        private void UpdateRangeIndicators()
        {
            foreach (var pair in artilleryRangeIndicators)
            {
                if (pair.Key == null || pair.Value == null) continue;

                // Update position and scale based on range
                pair.Value.transform.position = pair.Key.transform.position;
                float range = bunker.GetComponent<HeavyAssaultTactics>()?.artilleryShieldRadius ?? 20f;
                pair.Value.transform.localScale = new Vector3(range * 2f, range * 2f, 1f);

                // Update color based on status
                bool isInRange = Vector3.Distance(bunker.transform.position, pair.Key.transform.position) <= range;
                pair.Value.color = isInRange ? new Color(0.4f, 0.6f, 1f, 0.3f) : new Color(1f, 1f, 1f, 0.1f);
            }
        }

        private void CleanupSynergyDisplays(List<Artillery> currentArtillery)
        {
            activeSynergyDisplays.RemoveAll(display =>
            {
                var synergyDisplay = display.GetComponent<ArtillerySynergyDisplay>();
                if (synergyDisplay == null || !currentArtillery.Contains(synergyDisplay.artillery))
                {
                    Destroy(display);
                    return true;
                }
                return false;
            });

            // Clean up range indicators
            var deadKeys = artilleryRangeIndicators.Keys
                .Where(art => art == null || !currentArtillery.Contains(art))
                .ToList();

            foreach (var key in deadKeys)
            {
                if (artilleryRangeIndicators[key] != null)
                {
                    Destroy(artilleryRangeIndicators[key].gameObject);
                }
                artilleryRangeIndicators.Remove(key);
            }
        }

        public void Show()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }
        }

        public void ToggleUpgradePanel()
        {
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(!upgradePanel.activeSelf);
            }
        }

        private void UpdateUI()
        {
            UpdateStatusBars();
            UpdateGarrisonDisplay();
            UpdateCooldowns();
            UpdateUpgradeProgress();
        }
    }

    // Helper class for artillery synergy display
    public class ArtillerySynergyDisplay : MonoBehaviour
    {
        public Artillery artillery;
        public HeavyDefenseBunker bunker;
        public Image statusIcon;
        public TextMeshProUGUI statusText;
        public Image ammoBar;
        public Image shieldBar;

        public void Initialize(Artillery art, HeavyDefenseBunker bunk)
        {
            artillery = art;
            bunker = bunk;
            UpdateDisplay();
        }

        private void Update()
        {
            if (artillery == null || bunker == null)
            {
                Destroy(gameObject);
                return;
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (ammoBar != null)
            {
                ammoBar.fillAmount = artillery.GetAmmoPercentage() / 100f;
            }

            if (shieldBar != null)
            {
                bool hasShield = Vector3.Distance(artillery.transform.position, bunker.transform.position) <= 
                    bunker.GetComponent<HeavyAssaultTactics>()?.artilleryShieldRadius ?? 20f;
                shieldBar.gameObject.SetActive(hasShield);
            }

            if (statusText != null)
            {
                string status = artillery.IsDeployed ? "Deployed" : "Mobile";
                if (artillery.IsLowOnAmmo()) status += " - Low Ammo";
                statusText.text = status;
            }
        }
    }
}
