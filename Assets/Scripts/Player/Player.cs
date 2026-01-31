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
        private Vector2 _lookInput;
        private float _yaw;
        private Vector2 _mouseScreenPos;

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
                _playerStats
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
            _input.Gameplay.Move.canceled  += OnMove;
            _input.Gameplay.Jump.performed += OnJump;
            _input.Gameplay.Run.started += OnRun;
            _input.Gameplay.Run.canceled += OnRunCanceled;
            
            _input.Gameplay.LookPosition.performed += OnLook;
            _input.Gameplay.LookPosition.canceled  += OnLook;
        }

        private void OnDisable()
        {
            _input.Gameplay.Move.performed -= OnMove;
            _input.Gameplay.Move.canceled  -= OnMove;
            _input.Gameplay.Jump.performed -= OnJump;
            _input.Gameplay.Run.started -= OnRun;
            _input.Gameplay.Run.canceled -= OnRunCanceled;
            
            _input.Gameplay.LookPosition.performed -= OnLook;
            _input.Gameplay.LookPosition.canceled  -= OnLook;

            _input.Disable();
        }

        private void Update()
        {
            RotateTowardsMouse();
            
            _animation.Tick(
                _controller.velocity,
                _controller.isGrounded,
                _jumpPressed
            );
            
            _movement.Tick(_moveInput, _jumpPressed, Time.deltaTime, _runPressed);

            _jumpPressed = false;
        }

        public void SwitchInputAsses(bool isEnabled)
        {
            if(isEnabled)
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

        private void RotateTowardsMouse()
        {
            Ray ray = _camera.ScreenPointToRay(_mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundMask))
            {
                Vector3 direction = hit.point - transform.position;
                direction.y = 0f; 
                
                if (direction.sqrMagnitude < 0.001f)
                    return;

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = targetRotation;
            }
        }

        private void OnLook(InputAction.CallbackContext ctx)
        {
            _mouseScreenPos = ctx.ReadValue<Vector2>();
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