using System;
using System.Collections.Generic;
using DG.Tweening;
using Mask;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameUI
{
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(CanvasGroup))]
    public class MaskPopup : MonoBehaviour
    {
        public event Action<bool> MaskPopupOpened;
        public event Action<MaskModel> MaskSelected;

        // True through the closing animation; false is broadcast on completion or disable.
        public bool IsOpen { get; private set; }
        public int HoveredIndex => _hoveredIndex;
        public MaskModel HoveredMask => _hoveredIndex >= 0 && _hoveredIndex < _masks.Count
            ? _masks[_hoveredIndex]
            : null;

        [Header("Views")]
        [SerializeField] private List<MaskItemView> _iconSlots = new();
        [SerializeField] private TMP_Text _maskName;
        [SerializeField] private TMP_Text _maskDescription;
        [Header("Layout")]
        [SerializeField, Range(0.1f, 0.95f)] private float _iconRadius = 0.79f;
        [SerializeField, Min(1f)] private float _iconSize = 96f;
        [SerializeField, Min(1f)] private float _hoverScale = 1.12f;
        [SerializeField, Min(0f)] private float _animDuration = 0.18f;
        [Header("Mouse")]
        [Tooltip("Fraction of wheel radius. Angles also select beyond the outside edge.")]
        [SerializeField, Range(0f, 1f)] private float _mouseDeadZone = 0.15f;
        [SerializeField] private bool _keepSelectionInDeadZone = true;

        private readonly List<MaskModel> _masks = new();
        private RectTransform _wheelRect;
        private Image _wheelImage;
        private CanvasGroup _canvasGroup;
        private Camera _canvasCamera;
        private Material _material;
        private Sequence _animation;
        private int _segmentCountId;
        private int _selectedIndexId;
        private int _hoveredIndex = -1;
        private bool _closing;
        private bool _controllerSelection;
        private bool _initialized;
        private Vector2 _layoutSize;

        private void Awake()
        {
            _wheelRect = GetComponent<RectTransform>();
            _wheelImage = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _canvasCamera = canvas.rootCanvas.worldCamera;
            _segmentCountId = Shader.PropertyToID("_SegmentCount");
            _selectedIndexId = Shader.PropertyToID("_SelectedIndex");
            if (!_wheelImage.material.HasProperty(_segmentCountId))
            {
                Debug.LogError("MaskPopup requires the MaskPopup radial material on its Image.", this);
                enabled = false;
                return;
            }
            _material = new Material(_wheelImage.material) { name = "MaskPopup (Instance)" };
            _wheelImage.material = _material;
            _wheelImage.sprite = null;
            _wheelImage.type = Image.Type.Simple;
            _wheelImage.color = Color.white;
            _iconSlots.RemoveAll(slot => slot == null);
            foreach (var slot in _iconSlots)
                slot.gameObject.SetActive(false);
            if (_maskName == null)
                _maskName = CreateLabel("MaskPopup Name", new Vector2(0f, 65f), new Vector2(290f, 70f), 29f);
            if (_maskDescription == null)
                _maskDescription = CreateLabel("MaskPopup Description", new Vector2(0f, -30f), new Vector2(280f, 125f), 17f);
            var hint = CreateLabel("MaskPopup Hint", new Vector2(0f, -118f), new Vector2(265f, 25f), 13f);
            hint.text = "POINT TO CHOOSE  /  RELEASE OR CLICK TO SELECT";
            hint.color = new Color(0.8f, 0.9f, 0.94f, 0.8f);
            _initialized = true;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        public void Open(IReadOnlyList<MaskModel> masks)
        {
            if (IsOpen && !_closing)
                return;
            // Activating an initially inactive object runs Awake before configuration.
            if (!_initialized)
                gameObject.SetActive(true);
            if (!_initialized || !enabled)
                return;
            SetMasks(masks);
            if (_masks.Count == 0 || (transform.parent != null && !transform.parent.gameObject.activeInHierarchy))
                return;
            _animation?.Kill();
            _closing = false;
            _controllerSelection = false;
            bool wasOpen = IsOpen;
            gameObject.SetActive(true);
            IsOpen = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            if (!wasOpen)
            {
                _canvasGroup.alpha = 0f;
                _wheelRect.localScale = Vector3.one * 0.92f;
                MaskPopupOpened?.Invoke(true);
            }
            // Subscribers may close or disable MaskPopup while handling its open event.
            if (!IsOpen || _closing || !isActiveAndEnabled)
                return;
            UpdateMouseHover();
            _animation = DOTween.Sequence().SetUpdate(true)
                .Join(_canvasGroup.DOFade(1f, _animDuration))
                .Join(_wheelRect.DOScale(1f, _animDuration).SetEase(Ease.OutCubic));
        }

        public void Close()
        {
            if (!IsOpen || _closing)
                return;
            _closing = true;
            _animation?.Kill();
            _canvasGroup.interactable = false;
            if (_animDuration <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }
            _animation = DOTween.Sequence().SetUpdate(true)
                .Join(_canvasGroup.DOFade(0f, _animDuration))
                .Join(_wheelRect.DOScale(0.92f, _animDuration).SetEase(Ease.InCubic))
                .OnComplete(() => gameObject.SetActive(false));
        }

        public void Navigate(int direction)
        {
            if (!IsOpen || _closing || _masks.Count == 0 || direction == 0)
                return;
            _controllerSelection = true;
            int next = _hoveredIndex < 0
                ? (direction > 0 ? 0 : _masks.Count - 1)
                : (_hoveredIndex + direction + _masks.Count) % _masks.Count;
            SetHoveredIndex(next);
        }

        public void SetMasks(IReadOnlyList<MaskModel> masks)
        {
            MaskModel previous = _hoveredIndex >= 0 && _hoveredIndex < _masks.Count ? _masks[_hoveredIndex] : null;
            _masks.Clear();
            if (masks != null)
            {
                for (int i = 0; i < masks.Count; i++)
                {
                    if (masks[i] == null)
                    {
                        Debug.LogWarning("MaskPopup cannot display a list containing null entries.", this);
                        _masks.Clear();
                        break;
                    }
                    _masks.Add(masks[i]);
                }
            }
            if (!_initialized)
                return;
            while (_iconSlots.Count < _masks.Count)
            {
                MaskItemView slot;
                if (_iconSlots.Count > 0)
                    slot = Instantiate(_iconSlots[0], _wheelRect);
                else
                    slot = new GameObject("MaskPopup Icon", typeof(RectTransform), typeof(CanvasGroup),
                        typeof(Image), typeof(MaskItemView)).GetComponent<MaskItemView>();
                slot.transform.SetParent(_wheelRect, false);
                _iconSlots.Add(slot);
            }
            for (int i = 0; i < _iconSlots.Count; i++)
            {
                bool active = i < _masks.Count;
                _iconSlots[i].gameObject.SetActive(active);
                if (active)
                {
                    _iconSlots[i].name = $"MaskPopup Icon {i}";
                    _iconSlots[i].SetData(_masks[i].MaskSprite);
                }
            }
            SetMaterialFloat(_segmentCountId, Mathf.Max(1, _masks.Count));
            int nextIndex = previous == null ? -1 : _masks.IndexOf(previous);
            _hoveredIndex = nextIndex;
            LayoutIcons();
            _hoveredIndex = -2; // Force refresh when the data at an index changes.
            SetHoveredIndex(nextIndex);
            if (_masks.Count == 0)
                Close();
        }

        private void Update()
        {
            if (!IsOpen || _closing)
                return;
            if (_layoutSize != _wheelRect.rect.size)
                LayoutIcons();
            if (!_controllerSelection)
                UpdateMouseHover();
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && _hoveredIndex >= 0)
                MaskSelected?.Invoke(_masks[_hoveredIndex]);
        }

        private void UpdateMouseHover()
        {
            if (Mouse.current == null || _masks.Count == 0)
            {
                SetHoveredIndex(-1);
                return;
            }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_wheelRect,
                    Mouse.current.position.ReadValue(), _canvasCamera, out Vector2 point))
                return;
            Rect rect = _wheelRect.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;
            // Match shader UVs, including non-square RectTransforms and custom pivots.
            point -= rect.center;
            point = new Vector2(point.x * 2f / rect.width, point.y * 2f / rect.height);
            if (point.sqrMagnitude <= _mouseDeadZone * _mouseDeadZone)
            {
                if (!_keepSelectionInDeadZone)
                    SetHoveredIndex(-1);
                return;
            }
            // atan2(x, y): top=0, right=90, bottom=180, left=270; clockwise.
            float angle = Mathf.Repeat(Mathf.Atan2(point.x, point.y) * Mathf.Rad2Deg, 360f);
            int index = Mathf.FloorToInt(angle / (360f / _masks.Count));
            SetHoveredIndex(Mathf.Clamp(index, 0, _masks.Count - 1));
        }

        private void SetHoveredIndex(int index)
        {
            if (_hoveredIndex == index)
                return;
            if (_hoveredIndex >= 0 && _hoveredIndex < _iconSlots.Count)
                _iconSlots[_hoveredIndex].SetHovered(false, _hoverScale);
            _hoveredIndex = index;
            SetMaterialFloat(_selectedIndexId, index);
            if (index >= 0)
                _iconSlots[index].SetHovered(true, _hoverScale);
            _maskName.text = index >= 0 ? _masks[index].MaskName : "CHOOSE A MASK";
            _maskDescription.text = index >= 0 ? _masks[index].MaskDescription : "Move the mouse toward a mask.";
        }

        private void SetMaterialFloat(int property, float value)
        {
            _material.SetFloat(property, value);
            // UGUI creates a stencil material if this popup is inside a parent UI mask.
            var renderedMaterial = _wheelImage.materialForRendering;
            if (renderedMaterial != _material)
                renderedMaterial.SetFloat(property, value);
        }

        private void LayoutIcons()
        {
            _layoutSize = _wheelRect.rect.size;
            if (_masks.Count == 0)
                return;
            float radius = Mathf.Min(_layoutSize.x, _layoutSize.y) * 0.5f * _iconRadius;
            float size = Mathf.Min(_iconSize, 2f * Mathf.PI * radius / _masks.Count * 0.65f);
            for (int i = 0; i < _masks.Count; i++)
            {
                float angle = (i + 0.5f) * (2f * Mathf.PI / _masks.Count);
                Vector2 position = new Vector2(Mathf.Sin(angle) * _layoutSize.x,
                    Mathf.Cos(angle) * _layoutSize.y) * (0.5f * _iconRadius);
                _iconSlots[i].SetLayout(position, size);
                _iconSlots[i].SetHovered(i == _hoveredIndex, _hoverScale);
            }
        }

        private TMP_Text CreateLabel(string labelName, Vector2 position, Vector2 size, float fontSize)
        {
            var label = new GameObject(labelName, typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(_wheelRect, false);
            label.rectTransform.anchoredPosition = position;
            label.rectTransform.sizeDelta = size;
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = fontSize * 0.7f;
            label.fontSizeMax = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private void OnDisable()
        {
            _animation?.Kill();
            _animation = null;
            _closing = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            if (!IsOpen)
                return;
            IsOpen = false;
            MaskPopupOpened?.Invoke(false);
        }

        private void OnDestroy()
        {
            _animation?.Kill();
            if (_material != null)
                Destroy(_material);
        }
    }
}
