using UnityEngine;

namespace Player.Controllers
{
    public sealed class PlayerAnimationController
    {
        private readonly Animator _animator;

        private static readonly int SpeedHash      = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int JumpHash       = Animator.StringToHash("Jump");
        private static readonly int LandHash       = Animator.StringToHash("Land");

        private bool _wasGrounded;

        public PlayerAnimationController(Animator animator)
        {
            _animator = animator;
            _wasGrounded = true;
        }

        public void Tick(Vector3 velocity, bool isGrounded, bool jumpPressed)
        {
            float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

            _animator.SetFloat(SpeedHash, speed);
            _animator.SetBool(IsGroundedHash, isGrounded);

            if (jumpPressed && isGrounded)
            {
                _animator.ResetTrigger(LandHash);
                _animator.SetTrigger(JumpHash);
            }

            if (!_wasGrounded && isGrounded)
            {
                _animator.ResetTrigger(JumpHash);
                _animator.SetTrigger(LandHash);
            }

            _wasGrounded = isGrounded;
        }
    }
}