using UnityEngine;

namespace Player.Controllers
{
    public sealed class PlayerMovementController
    {
        private readonly IPlayerMotor _motor;

        private readonly float _speed;
        private readonly float _gravity;
        private readonly float _jumpForce;

        private float _verticalVelocity;

        public PlayerMovementController(
            IPlayerMotor motor,
            float speed,
            float gravity,
            float jumpForce)
        {
            _motor = motor;
            _speed = speed;
            _gravity = gravity;
            _jumpForce = jumpForce;
        }

        public void Tick(Vector2 moveInput, bool jumpPressed, float deltaTime)
        {
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y) * _speed;

            if (_motor.IsGrounded)
            {
                if (_verticalVelocity < 0)
                    _verticalVelocity = -2f;

                if (jumpPressed)
                    _verticalVelocity = _jumpForce;
            }
            else
            {
                _verticalVelocity -= _gravity * deltaTime;
            }

            move.y = _verticalVelocity;

            _motor.Move(move * deltaTime);
        }
    }
}