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
        private bool _wasManipulating;

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
            float deltaTime,
            bool isManipulating = false,
            Vector3 objectDirection = default,
            float grabSmoothTime = 0.15f,
            float grabMaxSpeed = 540f,
            float grabMinimumDistance = 0.35f)
        {
            if (deltaTime <= 0f) return;
            if (_wasManipulating != isManipulating)
            {
                _rotationSmoothVelocity = 0f;
                _wasManipulating = isManipulating;
            }
            Move(moveInput, jumpPressed, isRunning, deltaTime, isManipulating,
                objectDirection, grabSmoothTime, grabMaxSpeed, grabMinimumDistance);
        }

        private void Move(
            Vector2 moveInput,
            bool jumpPressed,
            bool isRunning,
            float deltaTime, bool isManipulating, Vector3 objectDirection,
            float grabSmoothTime, float grabMaxSpeed, float grabMinimumDistance)
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
            }

            objectDirection.y = 0f;
            Vector3 facingDirection = isManipulating ? objectDirection : moveDirection;
            float minimumDistance = isManipulating ? grabMinimumDistance : 0.01f;
            if (facingDirection.sqrMagnitude >= minimumDistance * minimumDistance)
            {
                float targetAngle = Mathf.Atan2(facingDirection.x, facingDirection.z) * Mathf.Rad2Deg;

                Vector3 forward = _motor.Forward;
                forward.y = 0f;

                float currentAngle = (forward.sqrMagnitude > 0.0001f)
                    ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg
                    : targetAngle;

                float smoothAngle = Mathf.SmoothDampAngle(
                    currentAngle,
                    targetAngle,
                    ref _rotationSmoothVelocity,
                    isManipulating ? grabSmoothTime : RotationSmoothTime,
                    isManipulating ? grabMaxSpeed : Mathf.Infinity,
                    deltaTime
                );

                float yawDelta = Mathf.DeltaAngle(currentAngle, smoothAngle);
                _motor.Rotate(yawDelta);
            }
            else _rotationSmoothVelocity = 0f;

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
