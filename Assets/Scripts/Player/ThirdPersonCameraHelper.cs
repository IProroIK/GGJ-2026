using UnityEngine;

namespace Player
{
    /// <summary>
    /// Optional helper component to manage cursor and camera settings
    /// Add this to your Player GameObject
    /// </summary>
    public class ThirdPersonCameraHelper : MonoBehaviour
    {
        [Header("Cursor Settings")]
        [SerializeField] private bool _lockCursor = true;
        [SerializeField] private KeyCode _toggleCursorKey = KeyCode.Escape;

        [Header("Camera Look Target")]
        [Tooltip("Optional: Create an empty child object for better camera targeting")]
        [SerializeField] private Transform _cameraLookTarget;
        [SerializeField] private float _lookTargetHeight = 1.5f;

        private bool _isCursorLocked;

        private void Start()
        {
            // Create look target if it doesn't exist
            if (_cameraLookTarget == null)
            {
                GameObject lookTarget = new GameObject("CameraLookTarget");
                lookTarget.transform.SetParent(transform);
                lookTarget.transform.localPosition = new Vector3(0, _lookTargetHeight, 0);
                _cameraLookTarget = lookTarget.transform;
            }

            // Lock cursor on start if enabled
            if (_lockCursor)
            {
                LockCursor();
            }
        }

        private void Update()
        {
            // Toggle cursor lock with Escape key
            if (Input.GetKeyDown(_toggleCursorKey))
            {
                if (_isCursorLocked)
                    UnlockCursor();
                else
                    LockCursor();
            }
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _isCursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _isCursorLocked = false;
        }

        // Public getter for the look target transform
        public Transform GetLookTarget() => _cameraLookTarget;

        private void OnDrawGizmos()
        {
            // Draw the camera look target position in editor
            if (_cameraLookTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_cameraLookTarget.position, 0.2f);
            }
        }
    }
}
