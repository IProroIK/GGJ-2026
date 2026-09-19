using System;
using Mask;
using Mask.Controllers;
using Objectives;
using Player.Controllers;
using Player.Model;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Player
{
    public enum LocomotionMode { Normal, ObjectManipulation }

    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class Player : MonoBehaviour
    {
        public bool IsInputLocked { get; private set; }
        public Transform ManipulationTarget { get; private set; }
        public LocomotionMode Mode => ManipulationTarget != null
            ? LocomotionMode.ObjectManipulation : LocomotionMode.Normal;
        public Vector3 PlanarVelocity { get; private set; }

        [Header("Movement")]
        [SerializeField] private Stats _playerStats;
        [SerializeField] private LayerMask _groundMask;
        [Header("Grab facing")]
        [SerializeField, Min(0.01f)] private float _grabFacingSmoothTime = 0.15f;
        [SerializeField, Min(1f)] private float _grabFacingMaxSpeed = 540f;
        [SerializeField, Min(0.01f)] private float _grabFacingMinimumDistance = 0.35f;

        private Camera _camera;
        private PhysicsDragController _drag;
        private PlayerMovementController _movement;
        private PlayerAnimationController _animation;
        private PlayerStatsController _playerStatsController;
        
        private PlayerInputActions _input;
        private InputAction _moveAction;
        
        private Vector2 _moveInput;
        private bool _jumpPressed;
        private bool _runPressed;

        private CharacterController _controller;
        private MaskManager _maskManager;
        private LevelManager _levelManager;
        private GameUI.MaskPopup _maskPopup;
        private bool _maskPopupOpen;
        private bool _movementEnabledBeforeMaskPopup;

        [Inject]
        private void Construct(MaskManager maskManager, LevelManager levelManager, GameUI.MaskPopup maskPopup)
        {
            _maskPopup = maskPopup;
            _levelManager = levelManager;
            _maskManager = maskManager;
        }

        private void Awake()
        {
            _camera = Camera.main;
            _controller = GetComponent<CharacterController>();
            _drag = FindFirstObjectByType<PhysicsDragController>();
            if (_drag != null)
            {
                _drag.ObjectGrabbedEvent += OnObjectGrabbed;
                OnObjectGrabbed(_drag.IsHoldingObject);
            }

            var motor = new CharacterControllerMotor(_controller);
            
            _movement = new PlayerMovementController(
                motor,
                _playerStats,
                _camera.transform  // Pass camera transform for camera-relative movement
            );

            _animation = new PlayerAnimationController(
                GetComponent<Animator>()
            );

            _input = new PlayerInputActions();
            _moveAction = _input.FindAction("Move");

            _playerStatsController = new PlayerStatsController(_maskManager, _playerStats);

            _levelManager.LevelChanged += LevelChangedEventHandler;
            _levelManager.LevelRestarted += LevelChangedEventHandler;
            _maskPopup.MaskPopupOpened += OnMaskPopupOpened;
        }

        private void OnDestroy()
        {
            _levelManager.LevelChanged -= LevelChangedEventHandler;
            _levelManager.LevelRestarted -= LevelChangedEventHandler;
            _maskPopup.MaskPopupOpened -= OnMaskPopupOpened;
            if (_drag != null) _drag.ObjectGrabbedEvent -= OnObjectGrabbed;
            _input.Dispose();
        }

        private void OnEnable()
        {
            _input.Enable();
            _input.Gameplay.Move.performed += OnMove;
            _input.Gameplay.Move.canceled += OnMove;
            _input.Gameplay.Jump.performed += OnJump;
            _input.Gameplay.Run.started += OnRun;
            _input.Gameplay.Run.canceled += OnRunCanceled;
        }

        private void OnDisable()
        {
            _input.Gameplay.Move.performed -= OnMove;
            _input.Gameplay.Move.canceled -= OnMove;
            _input.Gameplay.Jump.performed -= OnJump;
            _input.Gameplay.Run.started -= OnRun;
            _input.Gameplay.Run.canceled -= OnRunCanceled;
            _input.Disable();
            PlanarVelocity = Vector3.zero;
        }

        private void Update()
        {
            Vector3 positionBeforeMove = transform.position;
            _animation.Tick(
                _controller.velocity,
                _controller.isGrounded,
                _jumpPressed
            );

            _movement.Tick(
                _moveInput,
                _jumpPressed,
                _runPressed,
                Time.deltaTime,
                Mode == LocomotionMode.ObjectManipulation,
                ManipulationTarget != null ? ManipulationTarget.position - transform.position : Vector3.zero,
                _grabFacingSmoothTime,
                _grabFacingMaxSpeed,
                _grabFacingMinimumDistance
            );

            PlanarVelocity = Time.deltaTime > 0f
                ? Vector3.ProjectOnPlane(transform.position - positionBeforeMove, Vector3.up) / Time.deltaTime
                : Vector3.zero;

            _jumpPressed = false;
        }

        private void OnObjectGrabbed(bool grabbed)
        {
            ManipulationTarget = grabbed ? _drag.HeldTransform : null;
        }

        public void SwitchInputAsses(bool isEnabled)
        {
            if (_maskPopupOpen && isEnabled)
                return;
            IsInputLocked = isEnabled;
            if (isEnabled)
                _moveAction.Enable();
            else
                _moveAction.Disable();
        }

        public void SetPosition(Vector3 position)
        {
            PlanarVelocity = Vector3.zero;
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (_maskPopupOpen)
                return;
            if (ctx.performed)
                _jumpPressed = true;
        }

        private void OnRun(InputAction.CallbackContext ctx)
        {
            if (_maskPopupOpen)
                return;
            if (ctx.started)
            {
                _runPressed = true;
            }
        }

        private void OnRunCanceled(InputAction.CallbackContext ctx)
        {
            if (ctx.canceled)
            {
                _runPressed = false;
            }
        }

        private void OnMaskPopupOpened(bool open)
        {
            if (open)
            {
                _movementEnabledBeforeMaskPopup = _moveAction.enabled;
                _maskPopupOpen = true;
                SwitchInputAsses(false);
                _moveInput = Vector2.zero;
                _jumpPressed = false;
                _runPressed = false;
            }
            else
            {
                _maskPopupOpen = false;
                SwitchInputAsses(_movementEnabledBeforeMaskPopup);
            }
        }

        private void LevelChangedEventHandler()
        {
            _playerStatsController.ResetToDefault();
        }
    }
}
