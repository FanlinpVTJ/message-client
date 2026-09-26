using UnityEngine;

namespace WindowsManager.UI
{
    /// <summary>
    /// Обработка SafeArea
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rect;
        [SerializeField]
        private bool _applyMax = true;
        [SerializeField]
        private bool _applyMin = true;

        private void Awake()
        {
            rect = transform as RectTransform;
            SetSafeArea();
        }

#if UNITY_EDITOR
        private void Update()
        {
            SetSafeArea();
        }
#endif

        private void SetSafeArea()
        {
            Vector2 minAnchor = Screen.safeArea.position;
            Vector2 maxAnchor = Screen.safeArea.size + minAnchor;

            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            if (_applyMin)
                rect.anchorMin = minAnchor;
            if (_applyMax)
                rect.anchorMax = maxAnchor;
        }
    }
}