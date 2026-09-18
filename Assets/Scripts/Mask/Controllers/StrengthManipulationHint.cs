using TMPro;
using UnityEngine;

namespace Mask.Controllers
{
    internal sealed class StrengthManipulationHint
    {
        private readonly TMP_Text _text;

        public StrengthManipulationHint(GameUI.MaskPopup popup)
        {
            Canvas canvas = popup.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var hintObject = new GameObject("Strength Controls Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintObject.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)hintObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(750f, 110f);
            _text = hintObject.GetComponent<TextMeshProUGUI>();
            _text.alignment = TextAlignmentOptions.Bottom;
            _text.fontSize = 22f;
            _text.color = Color.white;
            _text.raycastTarget = false;
            _text.enabled = false;
        }

        public void ShowTarget()
        {
            if (_text == null) return;
            _text.text = "Click / E / A / Cross: Grab and move object";
            _text.enabled = true;
        }

        public void ShowHeld()
        {
            if (_text == null) return;
            _text.text = "Click / E / A / Cross: Release   |   Mouse / Right Stick: Move\n" +
                         "Wheel / D-Pad Up Down: Distance   |   Hold R / RB / R1 + Mouse / D-Pad: Rotate";
            _text.enabled = true;
        }

        public void Hide()
        {
            if (_text != null) _text.enabled = false;
        }

        public void Dispose()
        {
            if (_text != null) Object.Destroy(_text.gameObject);
        }
    }
}
