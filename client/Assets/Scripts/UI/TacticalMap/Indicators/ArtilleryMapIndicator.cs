using UnityEngine;
using UnityEngine.UI;
using RTS.Units;

namespace RTS.UI.TacticalMap
{
    public class ArtilleryMapIndicator : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Image indicatorImage;
        [SerializeField] private Image rangeIndicator;
        [SerializeField] private Image selectionHighlight;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color selectedColor = Color.green;
        
        private Artillery artillery;
        private bool isSelected;
        
        private void Awake()
        {
            if (!indicatorImage) indicatorImage = GetComponent<Image>();
            if (!rangeIndicator) rangeIndicator = transform.Find("RangeIndicator")?.GetComponent<Image>();
            if (!selectionHighlight) selectionHighlight = transform.Find("SelectionHighlight")?.GetComponent<Image>();
            
            ResetVisuals();
        }
        
        public void Initialize(Artillery artilleryUnit)
        {
            artillery = artilleryUnit;
            UpdateStatus();
        }
        
        public void UpdateStatus()
        {
            if (!artillery) return;
            
            // Update position
            transform.position = artillery.transform.position;
            
            // Update range indicator
            if (rangeIndicator)
            {
                float range = artillery.GetRange();
                rangeIndicator.transform.localScale = Vector3.one * range;
            }
            
            // Update visual state based on unit status
            UpdateVisualState();
        }
        
        private void UpdateVisualState()
        {
            if (!artillery || !indicatorImage) return;
            
            Color targetColor = normalColor;
            
            // Check unit status
            if (artillery.IsUnderAttack())
                targetColor = Color.red;
            else if (artillery.IsLowAmmo())
                targetColor = Color.yellow;
            
            indicatorImage.color = targetColor;
        }
        
        public void SetHighlight(bool highlight)
        {
            if (indicatorImage)
                indicatorImage.color = highlight ? highlightColor : normalColor;
        }
        
        public void SetSelectionHighlight(bool selected)
        {
            isSelected = selected;
            if (selectionHighlight)
                selectionHighlight.gameObject.SetActive(selected);
        }
        
        private void ResetVisuals()
        {
            if (indicatorImage) indicatorImage.color = normalColor;
            if (selectionHighlight) selectionHighlight.gameObject.SetActive(false);
        }
    }
}
