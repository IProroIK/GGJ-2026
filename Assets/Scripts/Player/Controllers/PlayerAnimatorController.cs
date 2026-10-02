using UnityEngine;

namespace Player.Controllers
{
    public sealed class PlayerAnimationController
    {
        private readonly Animator _animator;

        private readonly int _speedHash;
        private readonly int _isGroundedHash;
        private readonly int _jumpHash;
        private readonly int _landHash;

        private readonly int _isDashingHash, _isSlidingHash, _isWallRunningHash;
        private readonly int _isDoubleJumpingHash, _isLowProfileHash, _wallSideHash, _doubleJumpHash;

        private bool _wasGrounded;
        private bool _wasDoubleJumping;
        private bool _abilityOverridesJump;

        public PlayerAnimationController(Animator animator)
        {
            _animator = animator;
            _wasGrounded = true;
            _speedHash = Animator.StringToHash("Speed");
            _isGroundedHash = Animator.StringToHash("IsGrounded");
            _jumpHash = Animator.StringToHash("Jump");
            _landHash = Animator.StringToHash("Land");

            _isDashingHash = Animator.StringToHash("IsDashing");
            _isSlidingHash = Animator.StringToHash("IsSliding");
            _isWallRunningHash = Animator.StringToHash("IsWallRunning");
            _isDoubleJumpingHash = Animator.StringToHash("IsDoubleJumping");
            _isLowProfileHash = Animator.StringToHash("IsLowProfile");
            _wallSideHash = Animator.StringToHash("WallSide");
            _doubleJumpHash = Animator.StringToHash("DoubleJump");
        }

        public void Tick(Vector3 velocity, bool isGrounded, bool jumpPressed)
        {
            float speed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

            _animator.SetFloat(_speedHash, speed);
            _animator.SetBool(_isGroundedHash, isGrounded);

            if (jumpPressed && !_wasDoubleJumping && !_abilityOverridesJump)
            {
                _animator.ResetTrigger(_landHash);
                _animator.SetTrigger(_jumpHash);
            }

            if (!_wasGrounded && isGrounded)
            {
                _animator.ResetTrigger(_jumpHash);
                _animator.SetTrigger(_landHash);
            }

            _wasGrounded = isGrounded;
        }

        public void UpdateAgilityBools(PlayerMovementController movement)
        {
            _abilityOverridesJump = movement.IsDashing || movement.IsSliding || movement.IsWallRunning || movement.IsLowProfile;
            _animator.SetBool(_isDashingHash, movement.IsDashing);
            _animator.SetBool(_isSlidingHash, movement.IsSliding);
            _animator.SetBool(_isWallRunningHash, movement.IsWallRunning);
            _animator.SetBool(_isDoubleJumpingHash, movement.IsDoubleJumping);
            _animator.SetBool(_isLowProfileHash, movement.IsLowProfile);
            _animator.SetInteger(_wallSideHash, movement.WallSide);

            // The airborne flag persists until landing; trigger the flip only once.
            if (movement.IsDoubleJumping && !_wasDoubleJumping)
            {
                _animator.ResetTrigger(_jumpHash);
                _animator.ResetTrigger(_landHash);
                _animator.SetTrigger(_doubleJumpHash);
            }
            if (!movement.IsDoubleJumping || movement.IsDashing || movement.IsWallRunning)
                _animator.ResetTrigger(_doubleJumpHash);
            if (movement.IsDashing || movement.IsSliding || movement.IsWallRunning)
                _animator.ResetTrigger(_jumpHash);
            _wasDoubleJumping = movement.IsDoubleJumping;
        }
    }
}
