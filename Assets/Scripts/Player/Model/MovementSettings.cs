using System;
using UnityEngine;

namespace Player.Model
{
    [Serializable]
    public sealed class MovementSettings
    {
        [Header("Locomotion (all masks)")]
        [SerializeField, Min(1f)] private float _runMultiplier = 1.4f;
        [SerializeField, Min(0.1f)] private float _groundAcceleration = 35f;
        [SerializeField, Min(0.1f)] private float _airAcceleration = 15f;
        [SerializeField, Min(0f)] private float _coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float _jumpBufferTime = 0.12f;

        [Header("Agility: dash")]
        [SerializeField, Min(0.1f)] private float _dashSpeed = 12f;
        [SerializeField, Min(0.01f)] private float _dashDuration = 0.2f;
        [SerializeField, Min(0f)] private float _dashCooldown = 0.65f;

        [Header("Agility: slide")]
        [SerializeField, Min(0.1f)] private float _slideSpeed = 7f;
        [SerializeField, Min(0.01f)] private float _slideDuration = 0.8f;
        [SerializeField, Range(0.1f, 1f)] private float _slideHeightRatio = 0.55f;
        [SerializeField, Min(0f)] private float _slideCooldown = 0.25f;
        [SerializeField, Min(0.1f)] private float _slideMinimumSpeed = 3.4f;
        [SerializeField, Min(0.1f)] private float _slideMaxSpeed = 10f;
        [SerializeField, Min(0.1f)] private float _slideFriction = 4.5f;
        [SerializeField, Min(0f)] private float _slideAirDrag = 0.75f;
        [SerializeField, Min(0f)] private float _slideAirTurnSpeed = 90f;

        [Header("Agility: wall jump")]
        [Tooltip("Solid wall layers. Triggers, player colliders and ignored collision layers are excluded.")]
        [SerializeField] private LayerMask _wallLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.01f)] private float _wallProbeDistance = 0.35f;
        [SerializeField, Min(0f)] private float _wallJumpGraceTime = 0.12f;
        [SerializeField, Min(0f)] private float _wallJumpControlLock = 0.18f;
        [SerializeField, Min(0.1f)] private float _wallJumpHeightMultiplier = 1.1f;
        [SerializeField, Min(0f)] private float _wallJumpPush = 4f;

        [Header("Agility: wall run (temporarily disabled)")]
        [SerializeField] private bool _wallRunEnabled;
        [SerializeField, Min(0.1f)] private float _wallRunSpeed = 5.5f;
        [SerializeField, Min(0.01f)] private float _wallRunDuration = 1.4f;
        [SerializeField, Range(0f, 1f)] private float _wallGravityMultiplier = 0.2f;
        [SerializeField, Min(0f)] private float _wallMaxFallSpeed = 1.5f;
        [SerializeField, Min(0f)] private float _wallReattachDelay = 0.3f;

        [Header("Damage knockback")]
        [SerializeField, Min(0f)] private float _knockbackRecoveryTime = 0.2f;
        [SerializeField, Min(0.1f)] private float _knockbackDeceleration = 18f;

        public float KnockbackRecoveryTime => Mathf.Max(0f, _knockbackRecoveryTime);
        public float KnockbackDeceleration => Mathf.Max(0.1f, _knockbackDeceleration);
        public float RunMultiplier => Mathf.Max(1f, _runMultiplier);
        public float GroundAcceleration => Mathf.Max(0.1f, _groundAcceleration);
        public float AirAcceleration => Mathf.Max(0.1f, _airAcceleration);
        public float CoyoteTime => Mathf.Max(0f, _coyoteTime);
        public float JumpBufferTime => Mathf.Max(0f, _jumpBufferTime);
        public float DashSpeed => Mathf.Max(0.1f, _dashSpeed);
        public float DashDuration => Mathf.Max(0.01f, _dashDuration);
        public float DashCooldown => Mathf.Max(0f, _dashCooldown);
        public float SlideSpeed => Mathf.Max(0.1f, _slideSpeed);
        public float SlideDuration => Mathf.Max(0.01f, _slideDuration);
        public float SlideHeightRatio => Mathf.Clamp(_slideHeightRatio, 0.1f, 1f);
        public float SlideCooldown => Mathf.Max(0f, _slideCooldown);
        public float SlideMinimumSpeed => Mathf.Max(0.1f, _slideMinimumSpeed);
        public float SlideMaxSpeed => Mathf.Max(SlideSpeed, _slideMaxSpeed);
        public float SlideFriction => Mathf.Max(0.1f, _slideFriction);
        public float SlideAirDrag => Mathf.Max(0f, _slideAirDrag);
        public float SlideAirTurnSpeed => Mathf.Max(0f, _slideAirTurnSpeed);
        public bool WallRunEnabled => _wallRunEnabled;
        public float WallJumpGraceTime => Mathf.Max(0f, _wallJumpGraceTime);
        public float WallJumpControlLock => Mathf.Max(0f, _wallJumpControlLock);
        public float WallJumpHeightMultiplier => Mathf.Max(0.1f, _wallJumpHeightMultiplier);
        public LayerMask WallLayers => _wallLayers;
        public float WallProbeDistance => Mathf.Max(0.01f, _wallProbeDistance);
        public float WallRunSpeed => Mathf.Max(0.1f, _wallRunSpeed);
        public float WallRunDuration => Mathf.Max(0.01f, _wallRunDuration);
        public float WallGravityMultiplier => Mathf.Clamp01(_wallGravityMultiplier);
        public float WallMaxFallSpeed => Mathf.Max(0f, _wallMaxFallSpeed);
        public float WallJumpPush => Mathf.Max(0f, _wallJumpPush);
        public float WallReattachDelay => Mathf.Max(0f, _wallReattachDelay);
    }
}
