using Player.Model;
using UnityEngine;

namespace Player.Controllers
{
    public sealed class PlayerMovementController
    {
        private readonly IPlayerMotor _motor;
        private readonly Stats _playerStats;
        private readonly Transform _cameraTransform;
        private readonly MovementSettings _settings;
        private Vector3 _horizontalVelocity;
        private Vector3 _abilityDirection;
        private Vector3 _wallNormal;
        private float _verticalVelocity;
        private float _rotationSmoothVelocity;
        private float _coyoteRemaining;
        private float _jumpBufferRemaining;
        private float _dashRemaining;
        private float _dashCooldownRemaining;
        private float _slideRemaining;
        private float _slideCooldownRemaining;
        private float _slideSpeed;
        private bool _slideQueued;
        private bool _preserveAirMomentum;
        private Vector3 _wallContactNormal;
        private Vector3 _lastWallJumpNormal;
        private float _wallJumpGraceRemaining;
        private float _wallJumpControlRemaining;
        private bool _hasWallJumped;
        private float _wallRunRemaining;
        private float _wallReattachRemaining;
        private bool _airJumpUsed;
        private bool _airDashUsed;
        private bool _wasManipulating;
        private bool _agilityEnabled;
        private Vector3 _knockbackVelocity;
        private float _knockbackRecoveryRemaining;

        public bool IsDashing => _dashRemaining > 0f;
        public bool IsSliding => _slideRemaining > 0f;
        public bool IsRunning { get; private set; }
        public bool IsWallRunning { get; private set; }
        // -1: wall on the character's left; +1: wall on the right.
        public int WallSide => !IsWallRunning ? 0 : Vector3.Dot(_motor.Right, _wallNormal) > 0f ? -1 : 1;
        public bool IsDoubleJumping { get; private set; }
        public bool JumpedThisFrame { get; private set; }
        public bool WallJumpedThisFrame { get; private set; }
        public bool IsLowProfile => _motor.IsLowProfile;

        public PlayerMovementController(IPlayerMotor motor, Stats playerStats, Transform cameraTransform,
            MovementSettings settings = null)
        {
            _motor = motor;
            _playerStats = playerStats;
            _cameraTransform = cameraTransform;
            _settings = settings ?? new MovementSettings();
            _wallRunRemaining = _settings.WallRunDuration;
        }

        public void SetAgilityEnabled(bool enabled)
        {
            _agilityEnabled = enabled;
            if (!enabled) CancelAbilities();
            // Equipping in the air must not replenish consumed jumps, dash or wall-run time.
        }

        public void CancelAbilities()
        {
            IsRunning = false;
            _slideQueued = false;
            _preserveAirMomentum = false;
            _wallJumpGraceRemaining = 0f;
            _wallJumpControlRemaining = 0f;
            if (IsDashing || IsSliding || IsWallRunning) _horizontalVelocity = Vector3.zero;
            EndDash();
            EndSlide();
            EndWallRun();
            IsDoubleJumping = false;
            _jumpBufferRemaining = 0f;
            _motor.TrySetLowProfile(false);
        }

        public void ResetMotion(bool restoreStandingHeight = false)
        {
            _knockbackVelocity = Vector3.zero;
            _knockbackRecoveryRemaining = 0f;
            CancelAbilities();
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _rotationSmoothVelocity = 0f;
            _coyoteRemaining = 0f;
            _dashCooldownRemaining = 0f;
            _slideCooldownRemaining = 0f;
            _wallReattachRemaining = 0f;
            _wallRunRemaining = _settings.WallRunDuration;
            _airJumpUsed = false;
            _airDashUsed = false;
            _hasWallJumped = false;
            JumpedThisFrame = false;
            WallJumpedThisFrame = false;
            if (restoreStandingHeight) _motor.RestoreStandingHeight();
        }

        public void ApplyKnockback(Vector3 impulse)
        {
            CancelAbilities();
            _horizontalVelocity = Vector3.zero;
            _knockbackVelocity = Vector3.ProjectOnPlane(impulse, Vector3.up);
            _verticalVelocity = Mathf.Max(_verticalVelocity, impulse.y);
            _coyoteRemaining = 0f;
            _knockbackRecoveryRemaining = _settings.KnockbackRecoveryTime;
        }

        public void Tick(Vector2 moveInput, bool jumpPressed, bool isRunning, float deltaTime,
            bool isManipulating = false, Vector3 objectDirection = default,
            float grabSmoothTime = 0.15f, float grabMaxSpeed = 540f, float grabMinimumDistance = 0.35f,
            bool dashPressed = false, bool slidePressed = false, bool slideHeld = false,
            bool inputEnabled = true)
        {
            JumpedThisFrame = false;
            WallJumpedThisFrame = false;
            IsRunning = false;
            if (deltaTime <= 0f) return;
            if (_knockbackRecoveryRemaining > 0f)
            {
                inputEnabled = false;
                _knockbackRecoveryRemaining = Mathf.Max(0f, _knockbackRecoveryRemaining - deltaTime);
            }
            if (_wasManipulating != isManipulating)
            {
                _rotationSmoothVelocity = 0f;
                _wasManipulating = isManipulating;
            }

            _dashCooldownRemaining = Mathf.Max(0f, _dashCooldownRemaining - deltaTime);
            _slideCooldownRemaining = Mathf.Max(0f, _slideCooldownRemaining - deltaTime);
            _wallReattachRemaining = Mathf.Max(0f, _wallReattachRemaining - deltaTime);
            _jumpBufferRemaining = Mathf.Max(0f, _jumpBufferRemaining - deltaTime);
            _wallJumpGraceRemaining = Mathf.Max(0f, _wallJumpGraceRemaining - deltaTime);
            _wallJumpControlRemaining = Mathf.Max(0f, _wallJumpControlRemaining - deltaTime);
            bool grounded = _motor.IsGrounded && _verticalVelocity <= 0f;
            if (grounded)
            {
                _coyoteRemaining = _settings.CoyoteTime;
                _airJumpUsed = false;
                _airDashUsed = false;
                _hasWallJumped = false;
                _preserveAirMomentum = false;
                _wallJumpGraceRemaining = 0f;
                _wallJumpControlRemaining = 0f;
                _wallRunRemaining = _settings.WallRunDuration;
                IsDoubleJumping = false;
                _verticalVelocity = -2f;
            }
            else _coyoteRemaining = Mathf.Max(0f, _coyoteRemaining - deltaTime);

            if (!inputEnabled)
            {
                CancelAbilities();
                _horizontalVelocity = Vector3.zero;
                moveInput = Vector2.zero;
                jumpPressed = dashPressed = slidePressed = slideHeld = isRunning = false;
            }
            bool canUseAgility = _agilityEnabled && inputEnabled && !isManipulating;
            if (!canUseAgility && (IsDashing || IsSliding || IsWallRunning || _preserveAirMomentum
                || _wallJumpControlRemaining > 0f)) CancelAbilities();
            // While trapped below a ceiling, retain the short capsule until there is standing room.
            if (!IsSliding) _motor.TrySetLowProfile(false);

            Vector3 direction = GetMoveDirection(moveInput);
            float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
            if (jumpPressed) _jumpBufferRemaining = Mathf.Max(_settings.JumpBufferTime, deltaTime);
            if (!canUseAgility || !slideHeld) _slideQueued = false;
            else if (slidePressed || !grounded) _slideQueued = true;
            if (IsSliding && (!grounded || !slideHeld))
            {
                _preserveAirMomentum = !grounded || _jumpBufferRemaining > 0f;
                EndSlide();
            }

            UpdateWallRun(direction, grounded, canUseAgility);
            if (canUseAgility && !grounded && !IsDashing && !IsSliding && !_motor.IsLowProfile
                && _motor.TryFindJumpWall(_settings.WallProbeDistance, _settings.WallLayers, out Vector3 wallNormal))
            {
                _wallContactNormal = wallNormal;
                _wallJumpGraceRemaining = Mathf.Max(deltaTime, _settings.WallJumpGraceTime);
            }

            if (!IsDashing && _jumpBufferRemaining > 0f && _motor.TrySetLowProfile(false))
            {
                if (grounded || _coyoteRemaining > 0f)
                    Jump(false);
                else if (canUseAgility && _wallJumpGraceRemaining > 0f && _wallJumpControlRemaining <= 0f
                    && (!_hasWallJumped || Vector3.Dot(_lastWallJumpNormal, _wallContactNormal) < 0.5f))
                {
                    _horizontalVelocity = _wallContactNormal * _settings.WallJumpPush
                        + Vector3.ProjectOnPlane(direction, _wallContactNormal) * _playerStats.GetSpeed();
                    _lastWallJumpNormal = _wallContactNormal;
                    _hasWallJumped = true;
                    _wallJumpGraceRemaining = 0f;
                    _wallJumpControlRemaining = _settings.WallJumpControlLock;
                    _preserveAirMomentum = false;
                    EndWallRun();
                    Jump(false);
                    _verticalVelocity *= _settings.WallJumpHeightMultiplier;
                    WallJumpedThisFrame = true;
                }
                else if (canUseAgility && !_airJumpUsed)
                {
                    _airJumpUsed = true;
                    Jump(true);
                }
            }

            if (canUseAgility && dashPressed && _dashCooldownRemaining <= 0f
                && !IsDashing && (grounded || !_airDashUsed) && _motor.TrySetLowProfile(false))
            {
                EndSlide();
                EndWallRun();
                _preserveAirMomentum = false;
                _wallJumpControlRemaining = 0f;
                _slideQueued = false;
                _abilityDirection = direction.sqrMagnitude > 0.01f ? direction : Vector3.ProjectOnPlane(_motor.Forward, Vector3.up).normalized;
                _dashRemaining = _settings.DashDuration;
                _dashCooldownRemaining = _settings.DashDuration + _settings.DashCooldown;
                _verticalVelocity = 0f;
                if (!grounded || JumpedThisFrame) _airDashUsed = true;
            }
            else if (canUseAgility && _slideQueued && grounded && !JumpedThisFrame && !IsDashing
                && !IsSliding && _slideCooldownRemaining <= 0f
                && _horizontalVelocity.magnitude >= _settings.SlideMinimumSpeed)
            {
                EndWallRun();
                _slideQueued = false;
                _abilityDirection = _horizontalVelocity.normalized;
                _slideSpeed = Mathf.Clamp(_horizontalVelocity.magnitude, _settings.SlideSpeed, _settings.SlideMaxSpeed);
                _slideRemaining = _settings.SlideDuration;
                _motor.TrySetLowProfile(true, _settings.SlideHeightRatio);
            }

            float speed = _playerStats.GetSpeed() * (isRunning ? _settings.RunMultiplier : 1f);
            float movementTime = deltaTime;
            if (IsDashing)
            {
                _horizontalVelocity = _abilityDirection * _settings.DashSpeed;
                // Limit the final dash step so distance stays consistent at low frame rates.
                movementTime = Mathf.Min(deltaTime, _dashRemaining);
            }
            else if (IsSliding)
            {
                _horizontalVelocity = _abilityDirection * _slideSpeed;
                _slideSpeed = Mathf.Max(0f, _slideSpeed - _settings.SlideFriction * deltaTime);
            }
            else if (IsWallRunning)
            {
                _horizontalVelocity = Vector3.ProjectOnPlane(direction, _wallNormal).normalized * _settings.WallRunSpeed;
                // A small inward movement keeps contact without snapping to the wall.
                _horizontalVelocity -= _wallNormal * 1.5f;
            }
            else
            {
                IsRunning = isRunning && grounded && !JumpedThisFrame && !isManipulating
                    && !_motor.IsLowProfile && inputMagnitude > 0.1f;
                if (_motor.IsLowProfile) speed = Mathf.Min(speed, _playerStats.GetSpeed());
                // Preserve the push-off briefly even while the stick still points into the wall.
                if (_wallJumpControlRemaining <= 0f && _preserveAirMomentum && (!grounded || JumpedThisFrame))
                {
                    float momentumSpeed = Mathf.Max(speed * inputMagnitude,
                        _horizontalVelocity.magnitude - _settings.SlideAirDrag * deltaTime);
                    Vector3 momentumDirection = _horizontalVelocity.normalized;
                    if (inputMagnitude > 0.1f)
                        momentumDirection = Vector3.RotateTowards(momentumDirection, direction,
                            _settings.SlideAirTurnSpeed * Mathf.Deg2Rad * deltaTime, 0f);
                    _horizontalVelocity = momentumDirection * momentumSpeed;
                }
                else if (_wallJumpControlRemaining <= 0f)
                    _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, direction * (speed * inputMagnitude),
                        (grounded && !JumpedThisFrame ? _settings.GroundAcceleration : _settings.AirAcceleration) * deltaTime);
            }

            if (!IsDashing)
            {
                if (IsWallRunning)
                    _verticalVelocity = Mathf.Max(-_settings.WallMaxFallSpeed,
                        _verticalVelocity - _playerStats.GetGravity() * _settings.WallGravityMultiplier * deltaTime);
                else if (!grounded || JumpedThisFrame)
                    _verticalVelocity -= _playerStats.GetGravity() * deltaTime;
            }

            Vector3 facing = IsDashing || IsSliding ? _abilityDirection : direction;
            if (_wallJumpControlRemaining > 0f) facing = _horizontalVelocity;
            Rotate(isManipulating ? objectDirection : facing, isManipulating,
                grabSmoothTime, grabMaxSpeed, grabMinimumDistance, deltaTime);
            _motor.Move(_horizontalVelocity * movementTime
                + (_knockbackVelocity + Vector3.up * _verticalVelocity) * deltaTime);
            _knockbackVelocity = Vector3.MoveTowards(_knockbackVelocity, Vector3.zero,
                _settings.KnockbackDeceleration * deltaTime);
            if ((_motor.LastCollisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;

            if (IsDashing)
            {
                if (!_motor.IsGrounded) _airDashUsed = true;
                bool finished = _dashRemaining <= deltaTime;
                if (finished || (_motor.LastCollisionFlags & CollisionFlags.Sides) != 0) EndDash();
                else _dashRemaining -= deltaTime;
            }
            if (IsSliding)
            {
                if (_slideRemaining <= deltaTime || _slideSpeed < _playerStats.GetSpeed()
                    || (_motor.LastCollisionFlags & CollisionFlags.Sides) != 0) EndSlide();
                else _slideRemaining -= deltaTime;
            }
            if (IsWallRunning)
            {
                _wallRunRemaining = Mathf.Max(0f, _wallRunRemaining - deltaTime);
                if (_wallRunRemaining <= 0f || _motor.IsGrounded) EndWallRun();
            }
        }

        private void Jump(bool doubleJump)
        {
            if (IsSliding) _preserveAirMomentum = true;
            EndSlide();
            _verticalVelocity = _playerStats.GetJumpForce();
            _coyoteRemaining = 0f;
            _jumpBufferRemaining = 0f;
            JumpedThisFrame = true;
            IsDoubleJumping = doubleJump;
        }

        private void UpdateWallRun(Vector3 direction, bool grounded, bool canUseAgility)
        {
            if (!_settings.WallRunEnabled || !canUseAgility || grounded || IsDashing || IsSliding || _motor.IsLowProfile
                || direction.sqrMagnitude < 0.01f || _wallRunRemaining <= 0f || _wallReattachRemaining > 0f
                || !_motor.TryFindWall(direction, _settings.WallProbeDistance, _settings.WallLayers, out Vector3 normal)
                || Mathf.Abs(Vector3.Dot(direction, normal)) > 0.65f)
            {
                EndWallRun();
                return;
            }
            _wallNormal = normal;
            IsWallRunning = true;
        }

        private void EndDash()
        {
            if (IsDashing) _horizontalVelocity = Vector3.ClampMagnitude(_horizontalVelocity, _playerStats.GetSpeed() * _settings.RunMultiplier);
            _dashRemaining = 0f;
        }

        private void EndSlide()
        {
            if (IsSliding) _slideCooldownRemaining = _settings.SlideCooldown;
            _slideRemaining = 0f;
            _motor.TrySetLowProfile(false);
        }

        private void EndWallRun()
        {
            if (IsWallRunning) _wallReattachRemaining = _settings.WallReattachDelay;
            IsWallRunning = false;
        }

        private Vector3 GetMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 forward = _cameraTransform != null ? _cameraTransform.forward : Vector3.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            return (forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x).normalized;
        }

        private void Rotate(Vector3 direction, bool manipulating, float smoothTime, float maxSpeed,
            float minimumDistance, float deltaTime)
        {
            direction.y = 0f;
            float threshold = manipulating ? minimumDistance : 0.01f;
            if (direction.sqrMagnitude < threshold * threshold)
            {
                _rotationSmoothVelocity = 0f;
                return;
            }
            float target = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float current = Mathf.Atan2(_motor.Forward.x, _motor.Forward.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(current, target, ref _rotationSmoothVelocity,
                manipulating ? smoothTime : 0.12f, manipulating ? maxSpeed : Mathf.Infinity, deltaTime);
            _motor.Rotate(Mathf.DeltaAngle(current, angle));
        }
    }
}
