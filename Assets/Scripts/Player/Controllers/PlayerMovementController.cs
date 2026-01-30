using Player.Model;
using UnityEngine;

namespace Player.Controllers
{
    public sealed class PlayerMovementController
    {
        private readonly IPlayerMotor _motor;
        
        private Stats _playerStats;
        
        private float _verticalVelocity;
        private const float RunSpeedModifier = 1.4f;
        
        public PlayerMovementController(
            IPlayerMotor motor,
            Stats playerStats)
        {
            _playerStats = playerStats;
            _motor = motor;
        }

        public void Tick(Vector2 moveInput, bool jumpPressed, float deltaTime, bool isRunning)
        {
            var speed = isRunning ? _playerStats.GetSpeed() * RunSpeedModifier : _playerStats.GetSpeed();
            
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y) * speed;

            if (_motor.IsGrounded)
            {
                if (_verticalVelocity < 0)
                    _verticalVelocity = -2f;

                if (jumpPressed)
                    _verticalVelocity = _playerStats.GetJumpForce();
            }
            else
            {
                _verticalVelocity -= _playerStats.GetGravity() * deltaTime;
            }

            move.y = _verticalVelocity;

            _motor.Move(move * deltaTime);
        }
    }
}