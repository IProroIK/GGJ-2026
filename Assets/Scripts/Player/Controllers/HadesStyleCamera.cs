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

    private void Start()
    {
        _currentDistance = _baseDistance;
        _targetDistance = _baseDistance;
        
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
        UpdatePosition();
        UpdateShake();
    }

    private void UpdateZoom()
    {
        // Smoothly interpolate to target distance
        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, _zoomSpeed * Time.deltaTime);
    }

    private void UpdatePosition()
    {
        // Calculate offset based on angle and distance
        float radians = _cameraAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(0, _height, -_currentDistance * Mathf.Cos(radians));

        // Calculate desired position
        Vector3 desiredPosition = _target.position + offset;

        // Apply boundaries if enabled
        if (_useBoundaries)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, _minBounds.x, _maxBounds.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, _minBounds.y, _maxBounds.y);
        }

        // Add shake offset
        desiredPosition += _shakeOffset;

        // Smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            _smoothSpeed * Time.deltaTime
        );
        
        transform.position = smoothedPosition;

        // Smooth rotation toward target
        Quaternion targetRotation = Quaternion.Euler(_cameraAngle, 0, 0);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            _rotationSmoothSpeed * Time.deltaTime
        );
    }

    private void UpdateShake()
    {
        if (_shakeIntensity > 0)
        {
            _shakeOffset = Random.insideUnitSphere * _shakeIntensity;
            _shakeIntensity -= _shakeDecay * Time.deltaTime;
            
            if (_shakeIntensity < 0)
            {
                _shakeIntensity = 0;
                _shakeOffset = Vector3.zero;
            }
        }
    }

    #region Public Methods

    /// <summary>
    /// Zoom in for boss fights or special encounters
    /// </summary>
    public void ZoomForBossFight()
    {
        if (_enableDynamicZoom)
        {
            _targetDistance = _bossZoomDistance;
        }
    }

    /// <summary>
    /// Return to normal zoom
    /// </summary>
    public void ZoomToNormal()
    {
        _targetDistance = _baseDistance;
    }

    /// <summary>
    /// Gradually zoom to a specific distance
    /// </summary>
    public void ZoomTo(float distance)
    {
        _targetDistance = distance;
    }

    /// <summary>
    /// Instantly set zoom distance
    /// </summary>
    public void SetZoomImmediate(float distance)
    {
        _currentDistance = distance;
        _targetDistance = distance;
    }

    /// <summary>
    /// Trigger camera shake (for impacts, explosions, etc.)
    /// </summary>
    public void Shake(float intensity)
    {
        _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
    }

    /// <summary>
    /// Set new follow target
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (_target == null) return;

        // Draw camera bounds
        if (_useBoundaries)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (_minBounds.x + _maxBounds.x) / 2f,
                0,
                (_minBounds.y + _maxBounds.y) / 2f
            );
            Vector3 size = new Vector3(
                _maxBounds.x - _minBounds.x,
                0.1f,
                _maxBounds.y - _minBounds.y
            );
            Gizmos.DrawWireCube(center, size);
        }

        // Draw camera position
        Gizmos.color = Color.cyan;
        float radians = _cameraAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(0, _height, -_currentDistance * Mathf.Cos(radians));
        Vector3 cameraPos = _target.position + offset;
        
        Gizmos.DrawLine(_target.position, cameraPos);
        Gizmos.DrawWireSphere(cameraPos, 0.5f);

        // Draw FOV cone
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Vector3 forward = (_target.position - cameraPos).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        
        float fov = Camera.main != null ? Camera.main.fieldOfView : 60f;
        float halfFov = fov * 0.5f * Mathf.Deg2Rad;
        float distance = Vector3.Distance(_target.position, cameraPos);
        
        Gizmos.DrawLine(cameraPos, cameraPos + Quaternion.Euler(0, halfFov * Mathf.Rad2Deg, 0) * forward * distance);
        Gizmos.DrawLine(cameraPos, cameraPos + Quaternion.Euler(0, -halfFov * Mathf.Rad2Deg, 0) * forward * distance);
    }
}
}