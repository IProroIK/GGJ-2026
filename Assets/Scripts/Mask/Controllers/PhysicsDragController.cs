using Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Mask.Controllers
{
public class PhysicsDragController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference lookPositionAction;
    [SerializeField] private InputActionReference dragAction;
    
    [Header("Physics Settings")]
    [SerializeField] private float _dragForce = 50f;
    [SerializeField] private float _dragDamping = 5f;
    [SerializeField] private float _maxDragDistance = 10f;
    
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask _draggableLayer;
    [SerializeField] private float _raycastDistance = 100f;
    [SerializeField] private Camera _mainCamera;

    private Rigidbody _currentDraggedObject;
    private Vector3 _targetPosition;
    private float _dragDepth;
    private MaskManager _maskManager;

    [Inject]
    private void Construct(MaskManager maskManager)
    {
        _maskManager = maskManager;
    }
    
    private void Awake()
    {
        _maskManager.OnMaskUnequip += Reset;
        _maskManager.OnMaskEquip += Reset;
    }

    private void OnDestroy()
    {
        _maskManager.OnMaskUnequip -= Reset;
        _maskManager.OnMaskEquip -= Reset;
    }
    
    private void OnEnable()
    {
        dragAction.action.Enable();
        lookPositionAction.action.Enable();
        
        dragAction.action.performed += OnDragPerformed;
        dragAction.action.canceled += OnDragCanceled;
    }

    private void OnDisable()
    {
        dragAction.action.performed -= OnDragPerformed;
        dragAction.action.canceled -= OnDragCanceled;
        
        dragAction.action.Disable();
        lookPositionAction.action.Disable();
        if (_currentDraggedObject == null ) return;
        _currentDraggedObject.linearDamping = 0.5f; 
        _currentDraggedObject = null;
    }

    private void OnDragPerformed(InputAction.CallbackContext context)
    {
        if (_currentDraggedObject != null && _maskManager.CurrentMask == Enums.MaskType.Strength) return;
        
        Vector2 mousePosition = lookPositionAction.action.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _draggableLayer))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                _currentDraggedObject = rb;
                _dragDepth = Vector3.Distance(hit.point, _mainCamera.transform.position);
                
                _currentDraggedObject.linearDamping = _dragDamping;
            }
        }
    }

    private void OnDragCanceled(InputAction.CallbackContext context)
    {
        if (_currentDraggedObject != null && _maskManager.CurrentMask == Enums.MaskType.Strength)
        {
            _currentDraggedObject.linearDamping = 0.5f; 
            _currentDraggedObject = null;
        }
    }

    private void FixedUpdate()
    {
        if (_currentDraggedObject != null && _maskManager.CurrentMask == Enums.MaskType.Strength)
        {
            Vector2 mousePosition = lookPositionAction.action.ReadValue<Vector2>();
            
            Vector3 screenPoint = new Vector3(mousePosition.x, mousePosition.y, _dragDepth);
            _targetPosition = _mainCamera.ScreenToWorldPoint(screenPoint);
            
            Vector3 direction = _targetPosition - _currentDraggedObject.position;
            float distance = direction.magnitude;
            
            if (distance > _maxDragDistance)
            {
                direction = direction.normalized * _maxDragDistance;
                distance = _maxDragDistance;
            }
            
            Vector3 force = direction * _dragForce;
            _currentDraggedObject.AddForce(force, ForceMode.Force);
            
            Vector3 dampingForce = -_currentDraggedObject.linearVelocity * _dragDamping;
            _currentDraggedObject.AddForce(dampingForce, ForceMode.Force);
        }
    }

    private void OnDrawGizmos()
    {
        if (_currentDraggedObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_currentDraggedObject.position, _targetPosition);
            Gizmos.DrawWireSphere(_targetPosition, 0.2f);
        }
    }
    
    private void Reset(Enums.MaskType obj)
    {
        if (_currentDraggedObject == null ) return;
        _currentDraggedObject.linearDamping = 0.5f; 
        _currentDraggedObject = null;
    }
}

}