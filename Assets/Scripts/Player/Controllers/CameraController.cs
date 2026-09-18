using GameUI;
using Mask;
using Mask.Controllers;
using Objectives;
using Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Player.Controllers
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [Header("Framing")]
        [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1.1f, 0f);
        [SerializeField, Min(0.1f)] private float _distance = 5f;
        [SerializeField, Min(0.1f)] private float _minimumDistance = 2.5f;
        [SerializeField, Min(0.1f)] private float _maximumDistance = 8f;
        [SerializeField, Range(30f, 90f)] private float _fieldOfView = 60f;
        [SerializeField] private float _defaultPitch = 15f;
        [SerializeField] private Vector2 _pitchLimits = new Vector2(-35f, 70f);

        [Header("Look and follow")]
        [Tooltip("Degrees per mouse pixel; mouse delta must not be multiplied by delta time.")]
        [SerializeField, Min(0.001f)] private float _mouseSensitivity = 0.12f;
        [SerializeField, Min(1f)] private float _stickSpeed = 140f;
        [SerializeField] private bool _invertY;
        [SerializeField, Min(0f)] private float _lookSmoothTime = 0.045f;
        [SerializeField, Min(0f)] private float _followSmoothTime = 0.08f;
        [SerializeField, Min(0.01f)] private float _recenterSmoothTime = 0.18f;
        [SerializeField, Min(0f)] private float _zoomSensitivity = 0.005f;
        [SerializeField, Min(1f)] private float _teleportDistance = 8f;

        [Header("Obstruction")]
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField, Min(0.05f)] private float _collisionRadius = 0.22f;
        [SerializeField, Min(0.01f)] private float _collisionPadding = 0.05f;
        [SerializeField, Min(0.01f)] private float _collisionRecoveryTime = 0.2f;

        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private readonly Collider[] _overlaps = new Collider[64];
        private Player _player;
        private MaskPopup _popup;
        private MaskManager _masks;
        private LevelManager _levels;
        private PhysicsDragController _drag;
        private PhysicsMoveController _mover;
        private Camera _camera;
        private Transform _target;
        private PlayerInputActions _input;
        private InputAction _lookStick, _recenter, _zoom, _releaseCursor;
        private Vector3 _pivot, _pivotVelocity, _lastTargetPosition;
        private float _yaw, _pitch, _desiredYaw, _desiredPitch;
        private float _yawVelocity, _pitchVelocity, _currentDistance, _distanceVelocity;
        private bool _snapPending = true, _recentering, _cursorReleased, _focused = true;
        private bool _wasBlocked, _wasMouseLocked;
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;

        [Inject]
        private void Construct(Player player, MaskPopup popup, MaskManager masks, LevelManager levels)
        {
            _player = player;
            _popup = popup;
            _masks = masks;
            _levels = levels;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _target = _player.transform;
            _drag = FindFirstObjectByType<PhysicsDragController>();
            _mover = FindFirstObjectByType<PhysicsMoveController>();
            _camera.orthographic = false;
            _camera.fieldOfView = _fieldOfView;
            _camera.nearClipPlane = 0.1f;
            _input = new PlayerInputActions();
            _lookStick = _input.asset.FindAction("CameraLookStick", true);
            _recenter = _input.asset.FindAction("CameraRecenter", true);
            _zoom = _input.asset.FindAction("CameraZoom", true);
            _releaseCursor = _input.asset.FindAction("CameraReleaseCursor", true);
        }

        private void OnEnable()
        {
            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _input.Enable();
            _levels.LevelChanged += SnapToTarget;
            _levels.LevelRestarted += SnapToTarget;
            SnapToTarget();
        }

        private void OnDisable()
        {
            _levels.LevelChanged -= SnapToTarget;
            _levels.LevelRestarted -= SnapToTarget;
            _input.Disable();
            Cursor.lockState = _previousCursorLock;
            Cursor.visible = _previousCursorVisible;
        }

        private void OnDestroy() => _input?.Dispose();

        private void OnApplicationFocus(bool focused)
        {
            _focused = focused;
            if (!focused)
            {
                _cursorReleased = true;
                _wasMouseLocked = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void SnapToTarget() => _snapPending = true;

        public void Recenter()
        {
            _desiredYaw = _target.eulerAngles.y;
            _desiredPitch = _defaultPitch;
            _recentering = true;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            float dt = Time.unscaledDeltaTime;
            Vector3 targetPosition = _target.position + _targetOffset;
            if (_snapPending || Vector3.Distance(_lastTargetPosition, _target.position) > _teleportDistance)
            {
                _pivot = targetPosition;
                _pivotVelocity = Vector3.zero;
                _yaw = _desiredYaw = _target.eulerAngles.y;
                _pitch = _desiredPitch = _defaultPitch;
                _yawVelocity = _pitchVelocity = _distanceVelocity = 0f;
                _currentDistance = _distance;
                _recentering = false;
                _snapPending = false;
            }
            _lastTargetPosition = _target.position;

            bool held = (_drag != null && _drag.IsHoldingObject) || (_mover != null && _mover.IsHoldingObject);
            bool blocked = _popup.IsOpen || !_focused || Time.timeScale <= 0f || held;
            if (blocked && !_wasBlocked)
            {
                _desiredYaw = _yaw;
                _desiredPitch = _pitch;
                _yawVelocity = _pitchVelocity = 0f;
                _recentering = false;
            }
            UpdateCursor(blocked);
            // Ignore the first delta after a cursor warp or a modal interaction ends.
            if (!blocked && !_wasBlocked && !_cursorReleased)
                UpdateLook(dt);
            _wasBlocked = blocked;
            _wasMouseLocked = Cursor.lockState == CursorLockMode.Locked;

            float smoothTime = _recentering ? _recenterSmoothTime : _lookSmoothTime;
            _yaw = Mathf.SmoothDampAngle(_yaw, _desiredYaw, ref _yawVelocity, smoothTime, Mathf.Infinity, dt);
            _pitch = Mathf.SmoothDampAngle(_pitch, _desiredPitch, ref _pitchVelocity, smoothTime, Mathf.Infinity, dt);
            if (Mathf.Abs(Mathf.DeltaAngle(_yaw, _desiredYaw)) < 0.1f && Mathf.Abs(_pitch - _desiredPitch) < 0.1f)
                _recentering = false;
            _pivot = Vector3.SmoothDamp(_pivot, targetPosition, ref _pivotVelocity, _followSmoothTime, Mathf.Infinity, dt);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 backward = rotation * Vector3.back;
            // Keep the smoothed pivot on the player's side of intervening walls.
            Vector3 pivotOffset = _pivot - targetPosition;
            float radius = Mathf.Max(_collisionRadius, NearPlaneRadius());
            if (pivotOffset.sqrMagnitude > 0.0001f)
                _pivot = targetPosition + pivotOffset.normalized * ClearDistance(targetPosition, pivotOffset.normalized, pivotOffset.magnitude, radius);
            float clearDistance = ClearDistance(_pivot, backward, _distance, radius);
            if (clearDistance < _currentDistance)
            {
                _currentDistance = clearDistance;
                _distanceVelocity = 0f;
            }
            else
                _currentDistance = Mathf.SmoothDamp(_currentDistance, clearDistance, ref _distanceVelocity,
                    _collisionRecoveryTime, Mathf.Infinity, dt);
            transform.SetPositionAndRotation(_pivot + backward * _currentDistance, rotation);
        }

        private void UpdateCursor(bool blocked)
        {
            // MaskPopupPresenter owns the cursor for the entire wheel interaction.
            if (_popup.IsOpen || !_focused) return;
            if (_releaseCursor.WasPressedThisFrame()) _cursorReleased = true;
            if (_input.Gameplay.RightClick.WasPressedThisFrame()) _cursorReleased = false;
            bool pointerMask = _masks.CurrentMask == Enums.MaskType.Strength || _masks.CurrentMask == Enums.MaskType.Mover;
            bool locked = !blocked && !_cursorReleased && (!pointerMask || _input.Gameplay.RightClick.IsPressed());
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void UpdateLook(float dt)
        {
            Vector2 mouse = Cursor.lockState == CursorLockMode.Locked && _wasMouseLocked
                ? _input.Gameplay.LookDelta.ReadValue<Vector2>() * _mouseSensitivity : Vector2.zero;
            Vector2 stick = _lookStick.ReadValue<Vector2>() * (_stickSpeed * dt);
            Vector2 look = mouse + stick;
            if (look.sqrMagnitude > 0.000001f)
            {
                _desiredYaw += look.x;
                _desiredYaw = _yaw + Mathf.DeltaAngle(_yaw, _desiredYaw);
                _desiredPitch = Mathf.Clamp(_desiredPitch + look.y * (_invertY ? 1f : -1f), _pitchLimits.x, _pitchLimits.y);
                _recentering = false;
            }
            if (_recenter.WasPressedThisFrame()) Recenter();
            _distance = Mathf.Clamp(_distance - _zoom.ReadValue<float>() * _zoomSensitivity, _minimumDistance, _maximumDistance);
        }

        private float NearPlaneRadius()
        {
            float halfHeight = _camera.nearClipPlane * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return halfHeight * Mathf.Sqrt(1f + _camera.aspect * _camera.aspect);
        }

        private float ClearDistance(Vector3 origin, Vector3 direction, float distance, float radius)
        {
            int count = Physics.SphereCastNonAlloc(origin, radius, direction, _hits, distance,
                _collisionMask, QueryTriggerInteraction.Ignore);
            // A saturated buffer is conservatively blocked instead of missing a nearer wall.
            float result = count == _hits.Length ? 0f : distance;
            for (int i = 0; i < count; i++)
            {
                if (IsObstacle(_hits[i].collider))
                    result = Mathf.Min(result, Mathf.Max(0f, _hits[i].distance - _collisionPadding));
            }
            // Sphere casts miss initial overlaps; also check moving walls at the destination.
            while (result > 0f && IsOverlapping(origin + direction * result, radius))
                result = Mathf.Max(0f, result - radius * 0.5f);
            return result;
        }

        private bool IsOverlapping(Vector3 position, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, _overlaps, _collisionMask, QueryTriggerInteraction.Ignore);
            if (count == _overlaps.Length) return true;
            for (int i = 0; i < count; i++)
                if (IsObstacle(_overlaps[i])) return true;
            return false;
        }

        private bool IsObstacle(Collider obstacle) => obstacle != null && !obstacle.transform.IsChildOf(_target);
    }
}
