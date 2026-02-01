using Player.Model;
using UnityEngine;

namespace Player.Controllers
{
    public sealed class PlayerMovementController
    {
        private readonly IPlayerMotor _motor;
        private readonly Stats _playerStats;
        private readonly Transform _cameraTransform;

        private float _verticalVelocity;
        private int _jumpCounter;
        private bool _wasGrounded;

        private const float RunSpeedModifier = 1.4f;
        private const float RotationSmoothTime = 0.12f;
        private float _rotationSmoothVelocity;

        public PlayerMovementController(
            IPlayerMotor motor,
            Stats playerStats,
            Transform cameraTransform)
        {
            _motor = motor;
            _playerStats = playerStats;
            _cameraTransform = cameraTransform;
        }

        public void Tick(
            Vector2 moveInput,
            bool jumpPressed,
            bool isRunning,
            float deltaTime)
        {
            Move(moveInput, jumpPressed, isRunning, deltaTime);
        }

        private void Move(
            Vector2 moveInput,
            bool jumpPressed,
            bool isRunning,
            float deltaTime)
        {
            // Movement direction relative to camera
            Vector3 moveDirection = Vector3.zero;

            if (moveInput.sqrMagnitude >= 0.01f)
            {
                Vector3 cameraForward = _cameraTransform.forward;
                Vector3 cameraRight = _cameraTransform.right;

                cameraForward.y = 0f;
                cameraRight.y = 0f;

                cameraForward.Normalize();
                cameraRight.Normalize();

                moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

                Vector3 forward = _motor.Forward;
                forward.y = 0f;

                float currentAngle = (forward.sqrMagnitude > 0.0001f)
                    ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg
                    : targetAngle;

                float smoothAngle = Mathf.SmoothDampAngle(
                    currentAngle,
                    targetAngle,
                    ref _rotationSmoothVelocity,
                    RotationSmoothTime
                );

                float yawDelta = Mathf.DeltaAngle(currentAngle, smoothAngle);
                _motor.Rotate(yawDelta);
            }

            float speed = isRunning
                ? _playerStats.GetSpeed() * RunSpeedModifier
                : _playerStats.GetSpeed();

            Vector3 horizontalMove = moveDirection * speed;

            bool isGrounded = _motor.IsGrounded;

            // === LANDING ===
            if (isGrounded && !_wasGrounded)
            {
                _jumpCounter = 0;
            }

            // === JUMP (multi-jump) ===
            int maxJumps = Mathf.Max(1, _playerStats.GetJumpCount());

            if (jumpPressed && _jumpCounter < maxJumps)
            {
                if (isGrounded)
                    _jumpCounter = 0;

                _verticalVelocity = _playerStats.GetJumpForce();
                _jumpCounter++;
            }

            if (isGrounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity -= _playerStats.GetGravity() * deltaTime;
            }

            Vector3 motion = horizontalMove;
            motion.y = _verticalVelocity;

            _motor.Move(motion * deltaTime);
            _wasGrounded = isGrounded;
        }
    }
}
