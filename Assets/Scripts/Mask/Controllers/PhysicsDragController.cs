using System;
using System.Collections.Generic;
using Objectives;
using Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Mask.Controllers
{
    public class PhysicsDragController : MonoBehaviour
    {
        public bool IsHoldingObject => _held != null;
        public float MaxDragDistance => _maxDragDistance;
        public Transform HeldTransform => _held != null ? _held.transform : null;
        public bool IsCameraOrbitRequested => IsHoldingObject &&
            (_orbitMouse.IsPressed() || _orbitModifier.IsPressed());
        public event Action<Vector3> HeldObjectUpdatedPositionEvent;
        public event Action<bool> ObjectGrabbedEvent;
        
        [Header("Input")]
        [SerializeField] private InputActionReference lookPositionAction;
        [SerializeField] private InputActionReference dragAction;
        [Header("Physics Settings")]
        [SerializeField] private float _minHoldDistance = 1.5f;
        [SerializeField] private float _maxDragDistance = 10f;
        [SerializeField] private float _movementSpeed = 5f;
        [SerializeField] private float _depthMovementSpeed = 4f;
        [SerializeField] private float _movementSmoothing = 12f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _rotationStep = 45f;
        [SerializeField] private float _mousePixelsPerStep = 35f;
        [SerializeField] private float _mouseMovementScale = 0.012f;
        [SerializeField] private float _mouseWheelScale = 0.01f;
        [SerializeField] private float _collisionSkin = 0.025f;
        [Header("Raycast Settings")]
        [SerializeField] private LayerMask _draggableLayer;
        [SerializeField, Min(0.1f)] private float _raycastDistance = 100f;
        [SerializeField] private Camera _mainCamera;

        private Rigidbody _held;
        private StrengthHeldBody _motion;
        private bool _holding;
        private Vector3 _desiredPosition;
        private Quaternion _desiredRotation;
        private float _depth;
        private float _screenX;
        private float _screenY;
        private Vector2 _rotationPixels;
        private Vector3 _holdForward, _holdRight, _holdUp;
        private InputAction _orbitMouse, _orbitModifier;
        private InputAction[] _heldActions;
        private bool _wasOrbiting;
        private bool _gamepadTargeting;
        private bool _popupOpen;
        private MaskManager _masks;
        private Player.Player _player;
        private LevelManager _levels;
        private GameUI.MaskPopup _popup;
        private StrengthManipulationHint _hint;
        private InputAction _stick, _pointer, _dpad, _wheel, _rotateMode, _cancel, _reset;

        [Inject]
        private void Construct(MaskManager masks, Player.Player player, LevelManager levels, GameUI.MaskPopup popup)
        {
            _masks = masks;
            _player = player;
            _levels = levels;
            _popup = popup;
        }

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            var map = dragAction.action.actionMap;
            _stick = map.FindAction("ManipulateStick", true);
            _pointer = map.FindAction("ManipulatePointer", true);
            _dpad = map.FindAction("ManipulateDpad", true);
            _wheel = map.FindAction("ManipulateWheel", true);
            _rotateMode = map.FindAction("ManipulateRotationMode", true);
            _cancel = map.FindAction("ManipulateCancel", true);
            _reset = map.FindAction("ManipulateReset", true);
            _orbitMouse = map.FindAction("RightClick", true);
            _orbitModifier = map.FindAction("CameraRecenter", true);
            _heldActions = new[] { _stick, _pointer, _dpad, _wheel, _rotateMode, _cancel, _reset, _orbitMouse, _orbitModifier };
            _masks.OnMaskUnequip += OnMaskChanged;
            _masks.OnMaskEquip += OnMaskChanged;
            _masks.OnMaskUpdated += OnMasksUpdated;
            _popup.MaskPopupOpened += OnPopup;
            _levels.LevelChanged += Release;
            _levels.LevelRestarted += Release;
            _hint = new StrengthManipulationHint(_popup);
        }

        private void OnEnable()
        {
            dragAction.action.Enable();
            lookPositionAction.action.Enable();
            dragAction.action.performed += OnGrab;
            lookPositionAction.action.performed += OnPointerPositionChanged;
            _dpad.performed += OnDpad;
            _cancel.performed += OnCancel;
            _reset.performed += OnReset;
        }

        private void OnDisable()
        {
            Release();
            _hint?.Hide();
            dragAction.action.performed -= OnGrab;
            lookPositionAction.action.performed -= OnPointerPositionChanged;
            _dpad.performed -= OnDpad;
            _cancel.performed -= OnCancel;
            _reset.performed -= OnReset;
            dragAction.action.Disable();
            lookPositionAction.action.Disable();
        }

        private void OnDestroy()
        {
            Release();
            _masks.OnMaskUnequip -= OnMaskChanged;
            _masks.OnMaskEquip -= OnMaskChanged;
            _masks.OnMaskUpdated -= OnMasksUpdated;
            _popup.MaskPopupOpened -= OnPopup;
            _levels.LevelChanged -= Release;
            _levels.LevelRestarted -= Release;
            _hint?.Dispose();
        }

        private bool CanManipulate => !_popupOpen && _masks.CurrentMask == Enums.MaskType.Strength;

        private void OnPointerPositionChanged(InputAction.CallbackContext context)
        {
            _gamepadTargeting = false;
        }

        private void OnGrab(InputAction.CallbackContext context)
        {
            if (!CanManipulate) return;
            if (_held != null) { Release(); return; }
            _gamepadTargeting = context.control.device is Gamepad;
            if (!TryTarget(out Rigidbody body) || body.transform.IsChildOf(_player.transform)) return;

            Vector3 relative = body.position - _player.transform.position;
            Vector3 right = _mainCamera.transform.right;
            Vector3 up = _mainCamera.transform.up;
            float x = Vector3.Dot(relative, right);
            float y = relative.y / Mathf.Max(0.1f, up.y);
            float depth = Vector3.Dot(relative - right * x - up * y, FlatForward());
            if (depth < _minHoldDistance || depth > _maxDragDistance) return;

            _held = body;
            _holding = true;
            _screenX = x;
            _screenY = y;
            _depth = depth;
            CacheHoldBasis();
            _wasOrbiting = false;
            _desiredPosition = body.position;
            _desiredRotation = body.rotation;
            _rotationPixels = Vector2.zero;
            _motion = new StrengthHeldBody(body, _collisionSkin);
            SetHeldActions(true);
            UpdateHint();
            
            HeldObjectUpdatedPositionEvent?.Invoke(body.position);
            ObjectGrabbedEvent?.Invoke(true);
        }

        private bool TryTarget(out Rigidbody body)
        {
            body = null;
            if (_mainCamera == null) return false;
            Ray ray = _gamepadTargeting
                ? _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f))
                : _mainCamera.ScreenPointToRay(lookPositionAction.action.ReadValue<Vector2>());
            if (!Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _draggableLayer,
                    QueryTriggerInteraction.Ignore)) return false;
            body = hit.rigidbody;
            return body != null && !body.isKinematic &&
                   Vector3.Distance(body.position, _player.transform.position) <= _maxDragDistance + 2f;
        }

        private Vector3 FlatForward()
        {
            Vector3 forward = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up);
            return forward.sqrMagnitude > 0.001f ? forward.normalized : _player.transform.forward;
        }

        private void CacheHoldBasis()
        {
            _holdForward = FlatForward();
            _holdRight = _mainCamera.transform.right;
            _holdUp = _mainCamera.transform.up;
        }

        private void RebaseHoldOffset()
        {
            // Preserve the world offset when the camera or its framing rotates.
            Vector3 relative = _holdForward * _depth + _holdRight * _screenX + _holdUp * _screenY;
            CacheHoldBasis();
            _screenX = Vector3.Dot(relative, _holdRight);
            _screenY = relative.y / Mathf.Max(0.1f, _holdUp.y);
            _depth = Vector3.Dot(relative - _holdRight * _screenX - _holdUp * _screenY, _holdForward);
        }

        private void Update()
        {
            if (_held == null)
            {
                if (_holding) Release();
                else UpdateHint();
                return;
            }
            if (!CanManipulate || !_held.gameObject.activeInHierarchy) { Release(); return; }
            RebaseHoldOffset();
            bool orbiting = IsCameraOrbitRequested;
            bool suppressPointer = orbiting || _wasOrbiting;
            _wasOrbiting = orbiting;
            Vector2 pointer = suppressPointer ? Vector2.zero : _pointer.ReadValue<Vector2>();
            if (orbiting) _rotationPixels = Vector2.zero;
            else if (_rotateMode.IsPressed())
            {
                _rotationPixels += pointer;
                while (Mathf.Abs(_rotationPixels.x) >= _mousePixelsPerStep)
                {
                    float sign = Mathf.Sign(_rotationPixels.x);
                    Rotate(sign, 0f);
                    _rotationPixels.x -= sign * _mousePixelsPerStep;
                }
                while (Mathf.Abs(_rotationPixels.y) >= _mousePixelsPerStep)
                {
                    float sign = Mathf.Sign(_rotationPixels.y);
                    Rotate(0f, sign);
                    _rotationPixels.y -= sign * _mousePixelsPerStep;
                }
            }
            else
            {
                _rotationPixels = Vector2.zero;
                Vector2 move = _stick.ReadValue<Vector2>() * (_movementSpeed * Time.deltaTime) +
                               pointer * _mouseMovementScale;
                if (move.sqrMagnitude > 0.000001f)
                {
                    Vector2 screenOffset = Vector2.ClampMagnitude(new Vector2(_screenX + move.x, _screenY + move.y), _maxDragDistance);
                    _screenX = screenOffset.x;
                    _screenY = screenOffset.y;
                }
                float depthChange = _dpad.ReadValue<Vector2>().y * _depthMovementSpeed * Time.deltaTime +
                    _wheel.ReadValue<Vector2>().y * _mouseWheelScale;
                if (Mathf.Abs(depthChange) > 0.0001f)
                    _depth = Mathf.Clamp(_depth + depthChange, Mathf.Min(_minHoldDistance, _depth),
                        Mathf.Max(_maxDragDistance, _depth));
            }
            _desiredPosition = _player.transform.position + FlatForward() * _depth +
                               _mainCamera.transform.right * _screenX + _mainCamera.transform.up * _screenY;
            Vector3 relativeTarget = _desiredPosition - _player.transform.position;
            _desiredPosition = _player.transform.position +
                               Vector3.ClampMagnitude(relativeTarget, _maxDragDistance);
        }

        private void FixedUpdate()
        {
            if (_held == null) return;
            _motion.Tick(_desiredPosition, _desiredRotation, _movementSmoothing,
                _movementSpeed, _rotationSpeed, Time.fixedDeltaTime);
            
            HeldObjectUpdatedPositionEvent?.Invoke(_motion.GetPosition());
        }

        private void OnDpad(InputAction.CallbackContext context)
        {
            if (_held == null || IsCameraOrbitRequested || !_rotateMode.IsPressed()) return;
            Vector2 direction = context.ReadValue<Vector2>();
            if (Mathf.Abs(direction.x) > 0.5f) Rotate(Mathf.Sign(direction.x), 0f);
            if (Mathf.Abs(direction.y) > 0.5f) Rotate(0f, Mathf.Sign(direction.y));
        }

        private void Rotate(float horizontal, float vertical)
        {
            if (horizontal != 0f)
                _desiredRotation = Quaternion.AngleAxis(horizontal * _rotationStep,
                    _mainCamera.transform.up) * _desiredRotation;
            if (vertical != 0f)
                _desiredRotation = Quaternion.AngleAxis(vertical * _rotationStep,
                    _mainCamera.transform.right) * _desiredRotation;
            _desiredRotation.Normalize();
        }

        private void OnReset(InputAction.CallbackContext context)
        {
            if (_held != null) _desiredRotation = Quaternion.identity;
        }
        private void OnCancel(InputAction.CallbackContext context) => Release();
        private void OnMaskChanged(Enums.MaskType mask) => Release();
        private void OnMasksUpdated(List<Enums.MaskType> masks)
        {
            if (!masks.Contains(Enums.MaskType.Strength)) Release();
        }
        private void OnPopup(bool open)
        {
            _popupOpen = open;
            if (open) Release();
            else UpdateHint();
        }

        private void SetHeldActions(bool enabled)
        {
            foreach (InputAction action in _heldActions)
            {
                if (enabled) action.Enable();
                else action.Disable();
            }
        }

        private void Release()
        {
            _motion?.Release();
            _motion = null;
            _held = null;
            _holding = false;
            _rotationPixels = Vector2.zero;
            SetHeldActions(false);
            UpdateHint();
            
            ObjectGrabbedEvent?.Invoke(false);
        }

        private void UpdateHint()
        {
            if (_hint == null) return;
            if (!CanManipulate) { _hint.Hide(); return; }
            if (_held != null)
            {
                _hint.ShowHeld();
                return;
            }
            if (TryTarget(out _)) _hint.ShowTarget();
            else _hint.Hide();
        }
    }
}
