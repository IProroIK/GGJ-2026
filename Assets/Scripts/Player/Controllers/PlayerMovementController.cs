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
            // Calculate movement direction relative to camera
            Vector3 moveDirection = Vector3.zero;
            
            if (moveInput.sqrMagnitude >= 0.01f)
            {
                // Get camera forward and right, but flatten them on Y axis
                Vector3 cameraForward = _cameraTransform.forward;
                Vector3 cameraRight = _cameraTransform.right;
                
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                
                cameraForward.Normalize();
                cameraRight.Normalize();
                
                // Calculate desired move direction
                moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
                
                // Rotate character to face movement direction
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float currentAngle = _motor.Forward.y == 0 ? 
                    Mathf.Atan2(_motor.Forward.x, _motor.Forward.z) * Mathf.Rad2Deg : 
                    0f;
                
                float smoothAngle = Mathf.SmoothDampAngle(
                    currentAngle, 
                    targetAngle, 
                    ref _rotationSmoothVelocity, 
                    RotationSmoothTime
                );
                
                float yawDelta = Mathf.DeltaAngle(currentAngle, smoothAngle);
                _motor.Rotate(yawDelta);
            }

            // Calculate speed
            float speed = isRunning ? 
                _playerStats.GetSpeed() * RunSpeedModifier : 
                _playerStats.GetSpeed();

            Vector3 horizontalMove = moveDirection * speed;
            bool isGrounded = _motor.IsGrounded;

            // === JUMP ===
            if (jumpPressed && isGrounded && _jumpCounter < _playerStats.GetJumpCount())
            {
                _verticalVelocity = _playerStats.GetJumpForce();
                _jumpCounter++;
            }

            // === LANDING ===
            if (isGrounded && !_wasGrounded)
            {
                _jumpCounter = 0;
            }

            // === GRAVITY ===
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
