using System.Collections.Generic;
using Mask;
using Mask.Controllers;
using Objectives;
using Player.Controllers;
using Player.Model;
using Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Player
{
    public enum LocomotionMode { Normal, ObjectManipulation }

    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class Player : MonoBehaviour
    {
        public bool IsInputLocked { get; private set; }
        public Transform ManipulationTarget { get; private set; }
        public LocomotionMode Mode => ManipulationTarget != null
            ? LocomotionMode.ObjectManipulation : LocomotionMode.Normal;
        public Vector3 PlanarVelocity { get; private set; }
        public bool IsDashing => _movement != null && _movement.IsDashing;
        public bool IsSliding => _movement != null && _movement.IsSliding;
        public bool IsWallRunning => _movement != null && _movement.IsWallRunning;
        public PlayerHealth Health { get; private set; }

        [Header("Movement")]
        [SerializeField] private Stats _playerStats;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private MovementSettings _movementSettings = new MovementSettings();
        [Header("Movement effects")]
        [SerializeField] private GameObject _smokeWhiteTrail;
        [Header("Grab facing")]
        [SerializeField, Min(0.01f)] private float _grabFacingSmoothTime = 0.15f;
        [SerializeField, Min(1f)] private float _grabFacingMaxSpeed = 540f;
        [SerializeField, Min(0.01f)] private float _grabFacingMinimumDistance = 0.35f;

        private PhysicsDragController _drag;
        private PlayerMovementController _movement;
        private PlayerAnimationController _animation;
        private PlayerStatsController _playerStatsController;
        private PlayerInputActions _input;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _dashAction;
        private InputAction _slideAction;
        private CharacterController _controller;
        private MaskManager _maskManager;
        private LevelManager _levelManager;
        private GameUI.MaskPopup _maskPopup;
        private bool _maskPopupOpen;
        private bool _inputEnabled = true;
        private bool _restartPending;
        private Vector3 _pendingKnockback;
        private readonly Dictionary<Collider, DamageZone> _damageZones = new Dictionary<Collider, DamageZone>();

        [Inject]
        private void Construct(MaskManager maskManager, LevelManager levelManager, GameUI.MaskPopup maskPopup)
        {
            _maskPopup = maskPopup;
            _levelManager = levelManager;
            _maskManager = maskManager;
        }

        private void Awake()
        {
            SetSmokeTrailActive(false);
            Health = GetComponent<PlayerHealth>();
            Health.Died += OnDied;
            Camera camera = Camera.main;
            _controller = GetComponent<CharacterController>();
            _movement = new PlayerMovementController(new CharacterControllerMotor(_controller),
                _playerStats, camera != null ? camera.transform : null, _movementSettings);
            // The animation controller caches all parameter hashes here, during Awake.
            _animation = new PlayerAnimationController(GetComponent<Animator>());
            _input = new PlayerInputActions();
            _moveAction = _input.asset.FindAction("Move", true);
            _jumpAction = _input.asset.FindAction("Jump", true);
            _runAction = _input.asset.FindAction("Run", true);
            _dashAction = _input.asset.FindAction("Dash", true);
            _slideAction = _input.asset.FindAction("Slide", true);
            _playerStatsController = new PlayerStatsController(_maskManager, _playerStats);
            _movement.SetAgilityEnabled(_maskManager.CurrentMask == Enums.MaskType.Agility);
            _maskManager.OnMaskEquip += OnMaskEquipped;
            _maskManager.OnMaskUnequip += OnMaskUnequipped;
            _levelManager.LevelChanged += LevelChangedEventHandler;
            _levelManager.LevelRestarted += LevelChangedEventHandler;
            _maskPopup.MaskPopupOpened += OnMaskPopupOpened;

            _drag = FindFirstObjectByType<PhysicsDragController>();
            if (_drag != null)
            {
                _drag.ObjectGrabbedEvent += OnObjectGrabbed;
                OnObjectGrabbed(_drag.IsHoldingObject);
            }
        }

        private void OnDestroy()
        {
            if (Health != null) Health.Died -= OnDied;
            _levelManager.LevelChanged -= LevelChangedEventHandler;
            _levelManager.LevelRestarted -= LevelChangedEventHandler;
            _maskPopup.MaskPopupOpened -= OnMaskPopupOpened;
            _maskManager.OnMaskEquip -= OnMaskEquipped;
            _maskManager.OnMaskUnequip -= OnMaskUnequipped;
            if (_drag != null) _drag.ObjectGrabbedEvent -= OnObjectGrabbed;
            _playerStatsController.Dispose();
            _input.Dispose();
        }

        private void OnEnable()
        {
            _input.Enable();
            ApplyInputAccess();
        }

        private void OnDisable()
        {
            _pendingKnockback = Vector3.zero;
            SetSmokeTrailActive(false);
            _input.Disable();
            _movement.ResetMotion();
            _animation.UpdateAgilityBools(_movement);
            PlanarVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (_restartPending) return;
            if (_pendingKnockback.sqrMagnitude > 0f)
            {
                _movement.ApplyKnockback(_pendingKnockback);
                _pendingKnockback = Vector3.zero;
            }
            Vector3 positionBeforeMove = transform.position;
            bool inputEnabled = !IsInputLocked;
            _movement.Tick(
                inputEnabled ? _moveAction.ReadValue<Vector2>() : Vector2.zero,
                inputEnabled && _jumpAction.WasPressedThisFrame(),
                inputEnabled && _runAction.IsPressed(),
                Time.deltaTime,
                Mode == LocomotionMode.ObjectManipulation,
                ManipulationTarget != null ? ManipulationTarget.position - transform.position : Vector3.zero,
                _grabFacingSmoothTime, _grabFacingMaxSpeed, _grabFacingMinimumDistance,
                inputEnabled && _dashAction.WasPressedThisFrame(),
                inputEnabled && _slideAction.WasPressedThisFrame(),
                inputEnabled && _slideAction.IsPressed(),
                inputEnabled);

            PlanarVelocity = Time.deltaTime > 0f
                ? Vector3.ProjectOnPlane(transform.position - positionBeforeMove, Vector3.up) / Time.deltaTime
                : Vector3.zero;
            _animation.UpdateAgilityBools(_movement);
            _animation.Tick(_controller.velocity, _controller.isGrounded, _movement.JumpedThisFrame);
        }

        private void OnObjectGrabbed(bool grabbed)
        {
            ManipulationTarget = grabbed ? _drag.HeldTransform : null;
            if (grabbed) _movement.CancelAbilities();
        }

        public void SwitchInputAsses(bool isEnabled)
        {
            _inputEnabled = isEnabled;
            ApplyInputAccess();
        }

        private void ApplyInputAccess()
        {
            IsInputLocked = !_inputEnabled || _maskPopupOpen || _restartPending;
            if (IsInputLocked)
            {
                _moveAction.Disable();
                _movement.CancelAbilities();
                _animation.UpdateAgilityBools(_movement);
            }
            else if (isActiveAndEnabled) _moveAction.Enable();
        }

        public void SetPosition(Vector3 position)
        {
            _pendingKnockback = Vector3.zero;
            PlanarVelocity = Vector3.zero;
            _controller.enabled = false;
            transform.position = position;
            _movement.ResetMotion(restoreStandingHeight: true);
            _animation.UpdateAgilityBools(_movement);
            _controller.enabled = true;
        }

        private void OnMaskEquipped(Enums.MaskType mask)
        {
            _movement.SetAgilityEnabled(mask == Enums.MaskType.Agility);
            _animation.UpdateAgilityBools(_movement);
        }

        private void OnMaskUnequipped(Enums.MaskType mask)
        {
            _movement.SetAgilityEnabled(false);
            _animation.UpdateAgilityBools(_movement);
        }

        private void OnMaskPopupOpened(bool open)
        {
            _maskPopupOpen = open;
            ApplyInputAccess();
        }

        private void LevelChangedEventHandler()
        {
            _restartPending = false;
            _pendingKnockback = Vector3.zero;
            Health.ResetHealth();
            ApplyInputAccess();
            _playerStatsController.ResetToDefault();
            _movement.SetAgilityEnabled(false);
            _movement.ResetMotion(restoreStandingHeight: true);
            _animation.UpdateAgilityBools(_movement);
            PlanarVelocity = Vector3.zero;
        }

        public void RegisterDamageZone(Collider collider, DamageZone zone)
        {
            if (collider != null) _damageZones[collider] = zone;
        }

        public void UnregisterDamageZone(Collider collider, DamageZone zone)
        {
            if (collider != null && _damageZones.TryGetValue(collider, out DamageZone registered) && registered == zone)
                _damageZones.Remove(collider);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_damageZones.TryGetValue(hit.collider, out DamageZone zone)) zone.ApplyContact(hit.normal);
        }

        public bool TryTakeDamage(float amount, Vector3 impulse, bool continuous = false, float interval = 0f)
        {
            if (_restartPending || !Health.TryDamage(amount, continuous, interval)) return false;
            if (!Health.IsDead) _pendingKnockback += impulse;
            return true;
        }

        private void OnDied()
        {
            _restartPending = true;
            _pendingKnockback = Vector3.zero;
        }

        private void LateUpdate()
        {
            // Never teleport or destroy a level from inside CharacterController.Move
            // or a physics callback. Further hits are ignored until the restart.
            if (_restartPending) _levelManager.RestartLevel();
            SetSmokeTrailActive(!IsInputLocked && Mode == LocomotionMode.Normal
                && (_movement.IsRunning || IsSliding) && PlanarVelocity.sqrMagnitude > 0.01f);
        }

        private void SetSmokeTrailActive(bool active)
        {
            if (_smokeWhiteTrail != null && _smokeWhiteTrail.activeSelf != active)
                _smokeWhiteTrail.SetActive(active);
        }
    }
}
