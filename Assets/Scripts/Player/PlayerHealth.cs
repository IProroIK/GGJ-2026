using System;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maximumHealth = 100f;
        [SerializeField, Min(0f)] private float _hitInvulnerability = 0.65f;
        [SerializeField, Min(0f)] private float _respawnProtection = 0.75f;

        public event Action<float, float> HealthChanged;
        public event Action Died;
        public float MaximumHealth => Mathf.Max(1f, _maximumHealth);
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        private float _nextImpactTime;
        private float _nextContinuousTime;
        private float _protectedUntil;

        private void Awake() => ResetHealth();

        public void ResetHealth()
        {
            CurrentHealth = MaximumHealth;
            _protectedUntil = Time.time + Mathf.Max(0f, _respawnProtection);
            _nextImpactTime = _nextContinuousTime = _protectedUntil;
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
        }

        public bool TryDamage(float amount, bool continuous = false, float interval = 0f)
        {
            if (!isActiveAndEnabled || IsDead || amount <= 0f || float.IsNaN(amount)
                || float.IsInfinity(amount) || Time.time < _protectedUntil)
                return false;

            // Separate timers: taking a hit cannot make lava harmless, and adjacent
            // lava colliders cannot multiply the number of ticks received.
            if (Time.time < (continuous ? _nextContinuousTime : _nextImpactTime)) return false;
            float delay = Mathf.Max(0.02f, Mathf.Max(interval, continuous ? 0f : _hitInvulnerability));
            if (continuous) _nextContinuousTime = Time.time + delay;
            else _nextImpactTime = Time.time + delay;
            SetHealth(CurrentHealth - amount);
            return true;
        }

        public void Kill()
        {
            if (isActiveAndEnabled && !IsDead) SetHealth(0f);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            SetHealth(CurrentHealth + amount);
        }

        private void SetHealth(float value)
        {
            CurrentHealth = Mathf.Clamp(value, 0f, MaximumHealth);
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
            if (IsDead) Died?.Invoke();
        }
    }
}
