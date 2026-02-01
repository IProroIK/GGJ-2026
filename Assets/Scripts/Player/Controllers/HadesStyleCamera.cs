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

        [Tooltip("Base distance from player (15–20 for Hades style)")]
        [SerializeField] private float _baseDistance = 17f;

        [Tooltip("Height above player")]
        [SerializeField] private float _height = 20f;

        [Header("Initial Y Rotation")]
        [Tooltip("Yaw applied once on start")]
        [SerializeField] private float _initialYaw = -45f;

        [Header("Smooth Follow")]
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private float _rotationSmoothSpeed = 8f;

        [Header("Dynamic Zoom")]
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

        private Quaternion _yawRotation;

        private void Start()
        {
            _currentDistance = _baseDistance;
            _targetDistance = _baseDistance;

            _yawRotation = Quaternion.Euler(0f, _initialYaw, 0f);

            if (_target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player)
                    _target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            UpdateZoom();
            UpdateShake();
            UpdatePositionAndRotation();
        }

        private void UpdateZoom()
        {
            _currentDistance = Mathf.Lerp(
                _currentDistance,
                _targetDistance,
                _zoomSpeed * Time.deltaTime
            );
        }

        private void UpdatePositionAndRotation()
        {
            float radians = _cameraAngle * Mathf.Deg2Rad;

            Vector3 localOffset = new Vector3(
                0f,
                _height,
                -_currentDistance * Mathf.Cos(radians)
            );

            Vector3 worldOffset = _yawRotation * localOffset;
            Vector3 desiredPosition = _target.position + worldOffset + _shakeOffset;

            if (_useBoundaries)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, _minBounds.x, _maxBounds.x);
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, _minBounds.y, _maxBounds.y);
            }

            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                _smoothSpeed * Time.deltaTime
            );

            Quaternion targetRotation = Quaternion.Euler(_cameraAngle, _initialYaw, 0f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSmoothSpeed * Time.deltaTime
            );
        }

        private void UpdateShake()
        {
            if (_shakeIntensity <= 0f)
                return;

            _shakeOffset = Random.insideUnitSphere * _shakeIntensity;
            _shakeIntensity -= _shakeDecay * Time.deltaTime;

            if (_shakeIntensity <= 0f)
            {
                _shakeIntensity = 0f;
                _shakeOffset = Vector3.zero;
            }
        }

        #region Public API

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

        #endregion
    }
}
