using UnityEngine;

namespace Player.Controllers
{
    public sealed class CharacterControllerMotor : IPlayerMotor
    {
        private readonly CharacterController _controller;
        private readonly Transform _transform;

        public CharacterControllerMotor(CharacterController controller)
        {
            _controller = controller;
            _transform = controller.transform;
        }

        public Vector3 Position => _transform.position;
        public bool IsGrounded => _controller.isGrounded;

        public void Move(Vector3 delta)
        {
            _controller.Move(delta);
        }
    }
}