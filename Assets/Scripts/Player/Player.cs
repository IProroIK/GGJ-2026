using Player.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class Player : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _gravity = 20f;
        [SerializeField] private float _jumpForce = 7f;

        private PlayerMovementController _movement;
        private PlayerAnimationController _animation;
        private PlayerInputActions _input;

        private Vector2 _moveInput;
        private bool _jumpPressed;

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            var motor = new CharacterControllerMotor(_controller);

            _movement = new PlayerMovementController(
                motor,
                _speed,
                _gravity,
                _jumpForce
            );

            _animation = new PlayerAnimationController(
                GetComponent<Animator>()
            );

            _input = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _input.Enable();

            _input.Gameplay.Move.performed += OnMove;
            _input.Gameplay.Move.canceled  += OnMove;
            _input.Gameplay.Jump.performed += OnJump;
        }

        private void OnDisable()
        {
            _input.Gameplay.Move.performed -= OnMove;
            _input.Gameplay.Move.canceled  -= OnMove;
            _input.Gameplay.Jump.performed -= OnJump;

            _input.Disable();
        }

        private void Update()
        {
            _animation.Tick(
                _controller.velocity,
                _controller.isGrounded,
                _jumpPressed
            );
            
            _movement.Tick(_moveInput, _jumpPressed, Time.deltaTime);
            
            _jumpPressed = false; // consume jump
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
    }

}