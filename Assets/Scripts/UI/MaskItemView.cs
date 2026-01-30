using DG.Tweening;
using Settings;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class MaskItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;

        private RectTransform _rect;
        private Tween _move;
        private Tween _scale;

        private const float MainScale = 1.4f;
        private const float SideScale = 0.85f;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void SetData(Sprite sprite)
        {
            _icon.sprite = sprite;
        }

        public void Move(Vector2 pos, float duration)
        {
            _move?.Kill();
            _move = _rect.DOAnchorPos(pos, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        public void SetMain(float duration)
        {
            _scale?.Kill();
            _scale = _rect.DOScale(MainScale, duration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        public void SetSecondary(float duration)
        {
            _scale?.Kill();
            _scale = _rect.DOScale(SideScale, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        public void SetInstant(Vector2 pos, float scale)
        {
            DOTween.Kill(_rect);
            _rect.anchoredPosition = pos;
            _rect.localScale = Vector3.one * scale;
        }
    }
}