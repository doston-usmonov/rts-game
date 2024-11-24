using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Units;

namespace RTS.UI
{
    public class GroupMarkerUI : MonoBehaviour
    {
        public Image iconImage;
        public Image backgroundImage;
        public TextMeshProUGUI roleText;
        public float fadeDistance = 50f;
        public float minScale = 0.5f;
        public float maxScale = 1.5f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Camera mainCamera;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            mainCamera = Camera.main;
        }

        public void UpdateMarker(Sprite icon, Color color, string role)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
            }
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }
            if (roleText != null)
            {
                roleText.text = role;
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null) return;

            // Update visibility based on distance
            float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
            float normalizedDistance = Mathf.Clamp01(distance / fadeDistance);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - normalizedDistance;
            }

            // Scale based on distance
            float scale = Mathf.Lerp(maxScale, minScale, normalizedDistance);
            rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
