using System;
using Mask;
using Objectives;
using Player.Controllers;
using Player.Model;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Player
{
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class Player : MonoBehaviour
    {
        public bool IsInputLocked { get; private set; }

        [Header("Movement")]
        [SerializeField] private Stats _playerStats;
        [SerializeField] private LayerMask _groundMask;

        private Camera _camera;
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

        [Inject]
        private void Construct(MaskManager maskManager, LevelManager levelManager)
        {
            _levelManager = levelManager;
            _maskManager = maskManager;
        }

        private void Awake()
        {
            _camera = Camera.main;
            _controller = GetComponent<CharacterController>();

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
        }

        private void OnDestroy()
        {
            _levelManager.LevelChanged -= LevelChangedEventHandler;
            _levelManager.LevelRestarted -= LevelChangedEventHandler;
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
        }

        private void Update()
        {
            _animation.Tick(
                _controller.velocity,
                _controller.isGrounded,
                _jumpPressed
            );

            _movement.Tick(
                _moveInput,
                _jumpPressed,
                _runPressed,
                Time.deltaTime
            );

            _jumpPressed = false;
        }

        public void SwitchInputAsses(bool isEnabled)
        {
            IsInputLocked = isEnabled;
            if (isEnabled)
                _moveAction.Enable();
            else
                _moveAction.Disable();
        }

        public void SetPosition(Vector3 position)
        {
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
            if (ctx.performed)
                _jumpPressed = true;
        }

        private void OnRun(InputAction.CallbackContext ctx)
        {
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

        private void LevelChangedEventHandler()
        {
            _playerStatsController.ResetToDefault();
        }
    }
}
