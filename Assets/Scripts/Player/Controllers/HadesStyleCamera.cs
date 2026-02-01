using UnityEngine;

namespace Player.Controllers
{
    public class HadesStyleCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Hades-Style Settings")]
        [Tooltip("60° for Hades-style view")]
        [SerializeField] private float _cameraAngle = 60f;

        [Tooltip("Base distance from player (15-20 for Hades style)")]
        [SerializeField] private float _baseDistance = 17f;

        [Tooltip("Height above player")]
        [SerializeField] private float _height = 20f;

        [Header("Smooth Follow")]
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private float _rotationSmoothSpeed = 8f;

        [Header("Y Rotation (Yaw)")]
        [SerializeField] private bool _enableYawRotation = true;

        [Tooltip("Initial Y rotation in degrees")]
        [SerializeField] private float _initialYaw = 45f;

        [Tooltip("Yaw smoothing speed")]
        [SerializeField] private float _yawSmoothSpeed = 8f;

        [Tooltip("If true: camera orbits around target (position rotates too). If false: only camera view rotates.")]
        [SerializeField] private bool _orbitAroundTarget = true;

        [Tooltip("Optional input axis for yaw (e.g. 'Mouse X' or 'RightStickX'). Leave empty to keep fixed yaw.")]
        [SerializeField] private string _yawInputAxis = "";

        [Tooltip("Degrees per second at input=1")]
        [SerializeField] private float _yawInputSensitivity = 120f;

        [Tooltip("Clamp yaw angles (optional)")]
        [SerializeField] private bool _clampYaw = false;

        [SerializeField] private float _minYaw = -180f;
        [SerializeField] private float _maxYaw = 180f;

        [Header("Dynamic Zoom (Boss Fights)")]
        [SerializeField] private bool _enableDynamicZoom = true;
        [SerializeField] private float _bossZoomDistance = 22f;
        [SerializeField] private float _zoomSpeed = 2f;

        [Header("Camera Shake")]
        [SerializeField] private float _shakeDecay = 5f;

        [Header("Boundaries (Optional)")]
        [SerializeField] private bool _useBoundaries = false;
        [SerializeField] private Vector2 _minBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 _maxBounds = new Vector2(50, 50);

        private float _currentDistance;
        private float _targetDistance;

        private Vector3 _shakeOffset;
        private float _shakeIntensity;

        private float _yaw;
        private float _targetYaw;

        private void Start()
        {
            _currentDistance = _baseDistance;
            _targetDistance = _baseDistance;

            _yaw = _initialYaw;
            _targetYaw = _initialYaw;

            if (_target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                Debug.LogWarning("HadesStyleCamera: No target assigned!");
                return;
            }

            UpdateZoom();
            UpdateYaw();
            UpdateShake();
            UpdatePositionAndRotation();
        }

        private void UpdateZoom()
        {
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, _zoomSpeed * Time.deltaTime);
        }

        private void UpdateYaw()
        {
            if (!_enableYawRotation)
                return;

            if (!string.IsNullOrEmpty(_yawInputAxis))
            {
                float input = Input.GetAxis(_yawInputAxis);
                _targetYaw += input * _yawInputSensitivity * Time.deltaTime;
            }

            if (_clampYaw)
                _targetYaw = Mathf.Clamp(_targetYaw, _minYaw, _maxYaw);

            _yaw = Mathf.LerpAngle(_yaw, _targetYaw, _yawSmoothSpeed * Time.deltaTime);
        }

        private void UpdatePositionAndRotation()
        {
            float radians = _cameraAngle * Mathf.Deg2Rad;

            Vector3 localOffset = new Vector3(
                0f,
                _height,
                -_currentDistance * Mathf.Cos(radians)
            );

            Quaternion yawRot = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 worldOffset = _orbitAroundTarget ? (yawRot * localOffset) : localOffset;

            Vector3 desiredPosition = _target.position + worldOffset + _shakeOffset;

            if (_useBoundaries)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, _minBounds.x, _maxBounds.x);
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, _minBounds.y, _maxBounds.y);
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.Euler(_cameraAngle, _yaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSmoothSpeed * Time.deltaTime);
        }

        private void UpdateShake()
        {
            if (_shakeIntensity > 0f)
            {
                _shakeOffset = Random.insideUnitSphere * _shakeIntensity;
                _shakeIntensity -= _shakeDecay * Time.deltaTime;

                if (_shakeIntensity <= 0f)
                {
                    _shakeIntensity = 0f;
                    _shakeOffset = Vector3.zero;
                }
            }
        }

        #region Public Methods

        public void ZoomForBossFight()
        {
            if (_enableDynamicZoom)
                _targetDistance = _bossZoomDistance;
        }

        public void ZoomToNormal()
        {
            _targetDistance = _baseDistance;
        }

        public void ZoomTo(float distance)
        {
            _targetDistance = distance;
        }

        public void SetZoomImmediate(float distance)
        {
            _currentDistance = distance;
            _targetDistance = distance;
        }

        public void Shake(float intensity)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        }

        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
        }

        public void SetYaw(float yaw, bool immediate = false)
        {
            _targetYaw = yaw;

            if (_clampYaw)
                _targetYaw = Mathf.Clamp(_targetYaw, _minYaw, _maxYaw);

            if (immediate)
                _yaw = _targetYaw;
        }

        public void AddYaw(float delta)
        {
            _targetYaw += delta;

            if (_clampYaw)
                _targetYaw = Mathf.Clamp(_targetYaw, _minYaw, _maxYaw);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;

            if (_useBoundaries)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = new Vector3(
                    (_minBounds.x + _maxBounds.x) / 2f,
                    0f,
                    (_minBounds.y + _maxBounds.y) / 2f
                );
                Vector3 size = new Vector3(
                    _maxBounds.x - _minBounds.x,
                    0.1f,
                    _maxBounds.y - _minBounds.y
                );
                Gizmos.DrawWireCube(center, size);
            }

            Gizmos.color = Color.cyan;

            float radians = _cameraAngle * Mathf.Deg2Rad;
            Vector3 localOffset = new Vector3(0f, _height, -_currentDistance * Mathf.Cos(radians));
            Quaternion yawRot = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 worldOffset = _orbitAroundTarget ? (yawRot * localOffset) : localOffset;

            Vector3 cameraPos = _target.position + worldOffset;

            Gizmos.DrawLine(_target.position, cameraPos);
            Gizmos.DrawWireSphere(cameraPos, 0.5f);

            var cam = Camera.main;
            float fov = cam != null ? cam.fieldOfView : 60f;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Vector3 forward = (_target.position - cameraPos).normalized;
            float halfFov = fov * 0.5f * Mathf.Deg2Rad;
            float dist = Vector3.Distance(_target.position, cameraPos);

            Gizmos.DrawLine(cameraPos, cameraPos + Quaternion.Euler(0f, halfFov * Mathf.Rad2Deg, 0f) * forward * dist);
            Gizmos.DrawLine(cameraPos, cameraPos + Quaternion.Euler(0f, -halfFov * Mathf.Rad2Deg, 0f) * forward * dist);
        }
    }
}
