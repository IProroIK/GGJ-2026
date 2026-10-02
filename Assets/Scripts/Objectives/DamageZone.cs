using UnityEngine;
using Zenject;

namespace Objectives
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DamageZone : MonoBehaviour
    {
        public enum DamageMode { Impact, Continuous }

        [SerializeField] private DamageMode _mode = DamageMode.Impact;
        [SerializeField, Min(0.01f)] private float _damage = 25f;
        [Tooltip("Seconds between hits while touching this hazard. Continuous damage shares a player timer to prevent stacking at tile seams.")]
        [SerializeField, Min(0.02f)] private float _damageInterval = 0.65f;
        [SerializeField, Min(0f)] private float _knockbackSpeed = 4f;
        [SerializeField, Min(0f)] private float _upwardSpeed = 2.5f;

        private Player.Player _player;
        private Collider _collider;

        [Inject]
        private void Construct(Player.Player player) => _player = player;

        private void Awake() => _collider = GetComponent<Collider>();

        private void OnEnable()
        {
            if (_player != null) _player.RegisterDamageZone(_collider, this);
        }

        private void OnDisable()
        {
            if (_player != null) _player.UnregisterDamageZone(_collider, this);
        }

        private void OnTriggerEnter(Collider other) => TryTriggerDamage(other);
        private void OnTriggerStay(Collider other) => TryTriggerDamage(other);

        private void TryTriggerDamage(Collider other)
        {
            if (_player == null || !other.transform.IsChildOf(_player.transform)) return;
            ApplyContact(_player.transform.position - _collider.bounds.center);
        }

        // Solid hazards are dispatched from the player's CharacterController callback.
        public void ApplyContact(Vector3 awayFromSurface)
        {
            if (!isActiveAndEnabled || _collider == null || !_collider.enabled || _player == null) return;
            Vector3 direction = Vector3.ProjectOnPlane(awayFromSurface, Vector3.up);
            if (direction.sqrMagnitude < 0.001f) direction = -_player.transform.forward;
            Vector3 impulse = direction.normalized * Mathf.Max(0f, _knockbackSpeed)
                + Vector3.up * Mathf.Max(0f, _upwardSpeed);
            _player.TryTakeDamage(_damage, impulse, _mode == DamageMode.Continuous, _damageInterval);
        }
    }
}
