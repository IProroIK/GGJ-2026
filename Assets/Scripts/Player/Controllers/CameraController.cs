using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Player.Controllers
{
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private float sensitivity = 2f;
        [SerializeField] private float resetSpeed = 5f;
        private PlayerInputActions _input;
        private bool _isRightPressed;
        private float _yRotation; 
        private readonly float _defaultYRotation = -45f;
        private Player _player;
        private const float DefaultXRotation = 30f;

        [Inject]
        private void Construct(Player player)
        {
            _player = player;
        }
        
        private void Awake()
        {
            _input = new PlayerInputActions();
            _input.Gameplay.RightClick.performed += OnRightClickOnperformed;
            _input.Gameplay.RightClick.canceled  += OnRightClickOncanceled;
        
            _yRotation = transform.localRotation.eulerAngles.y;
        }
    
        private void OnRightClickOnperformed(InputAction.CallbackContext _)
        {
            _isRightPressed = true;
            if(_player.IsInputLocked)
                _player.SwitchInputAsses(false);
        }

        private void OnRightClickOncanceled(InputAction.CallbackContext _)
        {
            _isRightPressed = false;
            if(!_player.IsInputLocked)
                _player.SwitchInputAsses(true);
        }

        private void OnEnable() => _input.Enable();
        private void OnDisable() => _input.Disable();
    
        private void LateUpdate()
        {
                Vector2 delta = _input.Gameplay.LookDelta.ReadValue<Vector2>();
                _yRotation += delta.x * sensitivity;
                transform.localRotation = Quaternion.Euler(DefaultXRotation, _yRotation, 0f);
                
            // else if(!_isRightPressed)
            // {
            //     _yRotation = Mathf.LerpAngle(_yRotation, _defaultYRotation, Time.deltaTime * resetSpeed);
            //     transform.localRotation = Quaternion.Euler(DefaultXRotation, _yRotation, 0f);
            // }
        }
    }
}