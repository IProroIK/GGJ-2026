using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class MaskItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private CanvasGroup _canvasGroup;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_icon == null)
            _icon = GetComponent<Image>();
        var background = GetComponent<Image>();
        if (background != null && background != _icon)
            background.enabled = false;
        _icon.raycastTarget = false;
        _icon.preserveAspect = true;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    public void SetData(Sprite sprite)
    {
        _icon.raycastTarget = false;
        _icon.preserveAspect = true;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _icon.sprite = sprite;
        _icon.enabled = sprite != null;
    }

    public void SetLayout(Vector2 position, float size)
    {
        // A pooled slot may be configured before its first activation/Awake.
        if (_rect == null)
            _rect = (RectTransform)transform;
        _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchoredPosition = position;
        _rect.sizeDelta = Vector2.one * size;
        _rect.localRotation = Quaternion.identity;
        if (_icon.rectTransform != _rect)
        {
            _icon.rectTransform.anchorMin = _icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _icon.rectTransform.anchoredPosition = Vector2.zero;
            _icon.rectTransform.sizeDelta = Vector2.one * size;
        }
    }

    public void SetHovered(bool hovered, float hoverScale)
    {
        if (_rect == null)
            _rect = (RectTransform)transform;
        _rect.localScale = Vector3.one * (hovered ? hoverScale : 1f);
        _canvasGroup.alpha = hovered ? 1f : 0.78f;
        _icon.color = hovered ? Color.white : new Color(0.88f, 0.91f, 0.94f, 1f);
    }
}
