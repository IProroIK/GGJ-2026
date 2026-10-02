using Player;
using TMPro;
using UnityEngine;
using Zenject;

namespace GameUI
{
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [SerializeField] private UnityEngine.UI.Image _fillImage;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Color _healthyColor = new Color(0.86f, 0.2f, 0.26f);
        [SerializeField] private Color _lowHealthColor = new Color(1f, 0.48f, 0.18f);

        private Player.Player _player;
        private PlayerHealth _health;

        [Inject]
        private void Construct(Player.Player player) => _player = player;

        private void Start()
        {
            _health = _player.Health;
            Subscribe();
        }

        private void OnEnable()
        {
            if (_health != null) Subscribe();
        }

        private void OnDisable()
        {
            if (_health != null) _health.HealthChanged -= Refresh;
        }

        private void Subscribe()
        {
            _health.HealthChanged += Refresh;
            Refresh(_health.CurrentHealth, _health.MaximumHealth);
        }

        private void Refresh(float current, float maximum)
        {
            float ratio = Mathf.Clamp01(current / maximum);
            _fill.anchorMax = new Vector2(ratio, 1f);
            _fillImage.color = ratio <= 0.3f ? _lowHealthColor : _healthyColor;
            _label.SetText("{0:0} / {1:0}", Mathf.Ceil(current), Mathf.Ceil(maximum));
        }
    }
}
