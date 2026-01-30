using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MaskItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private CanvasGroup _canvasGroup;

    private RectTransform _rect;
    private Tween _move;
    private Tween _scale;
    private Sequence _fade;

    private const float MainScale = 1.4f;
    private const float SideScale = 0.85f;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetData(Sprite sprite, bool animate = true)
    {
        if (!animate || _icon.sprite == null)
        {
            // Instant change, no animation
            _fade?.Kill();
            _icon.sprite = sprite;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
            return;
        }

        // Animated change
        _fade?.Kill();
        
        var sequence = DOTween.Sequence();
        sequence.Append(_canvasGroup.DOFade(0f, 0.1f))
            .AppendCallback(() => _icon.sprite = sprite)
            .Append(_canvasGroup.DOFade(1f, 0.15f))
            .SetUpdate(true);
        
        _fade = sequence;
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
        _fade?.Kill();
        
        _rect.anchoredPosition = pos;
        _rect.localScale = Vector3.one * scale;
        
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }
}