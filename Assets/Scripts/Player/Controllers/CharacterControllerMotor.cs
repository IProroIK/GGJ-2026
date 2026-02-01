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
        public Vector3 Forward => _transform.forward;
        public Vector3 Right => _transform.right;

        public void Move(Vector3 motion)
        {
            _controller.Move(motion);
        }

        public void Rotate(float yawDegrees)
        {
            if (yawDegrees == 0f) return;
            
            _transform.Rotate(0f, yawDegrees, 0f, Space.World);
        }
    }
}
