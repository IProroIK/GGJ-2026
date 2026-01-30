using Player.Model;
using UnityEngine;

namespace Player.Controllers
{
    public sealed class PlayerMovementController
    {
        private readonly IPlayerMotor _motor;
        
        private Stats _playerStats;
        
        private float _verticalVelocity;
        private int _jumpCounter = 0;
        private const float RunSpeedModifier = 1.4f;
        private bool _wasGrounded;

        public PlayerMovementController(
            IPlayerMotor motor,
            Stats playerStats)
        {
            _playerStats = playerStats;
            _motor = motor;
        }

        public void Tick(Vector2 moveInput, bool jumpPressed, float deltaTime, bool isRunning)
        {
            var speed = isRunning
                ? _playerStats.GetSpeed() * RunSpeedModifier
                : _playerStats.GetSpeed();

            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y) * speed;

            bool isGrounded = _motor.IsGrounded;

            // === JUMP ===
            if (jumpPressed && _jumpCounter < _playerStats.GetJumpCount())
            {
                _verticalVelocity = _playerStats.GetJumpForce();
                _jumpCounter++;
            }

            // === LANDING DETECTION ===
            if (isGrounded && !_wasGrounded)
            {
                // just landed
                _jumpCounter = 0;
            }

            // === GRAVITY ===
            if (isGrounded)
            {
                if (_verticalVelocity < 0)
                    _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity -= _playerStats.GetGravity() * deltaTime;
            }

            move.y = _verticalVelocity;

            _motor.Move(move * deltaTime);

            _wasGrounded = isGrounded;
        }

    }
}