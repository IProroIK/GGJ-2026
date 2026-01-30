using DG.Tweening;
using Settings;
using UnityEngine;
using Zenject;

namespace Mask
{
    public class Mask : MonoBehaviour
    {
        [SerializeField] private Transform _maskTransform;
        [SerializeField] private Enums.MaskType _maskType;
        [SerializeField] private float moveDistance;
        private MaskManager _maskManager;

        private Tween _rotationTween;
        private const float MoveTime = 2f;
        
        [Inject]
        private void Constract(MaskManager maskManager)
        {
            _maskManager = maskManager;
        }

        private void OnEnable()
        {
            StartAnimation();
        }

        private void OnDisable()
        {
            _rotationTween?.Kill();
        }
        
        private void StartAnimation()
        {
            _rotationTween = _maskTransform
                .DORotate(
                    new Vector3(0f, 360f, 0f),
                    2f,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.Linear)
                .SetLoops(-1);

            Vector3 startPos = _maskTransform.position;

            _maskTransform.DOMoveY(startPos.y + 0.5f, 1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Player.Player player))
            {
                _maskManager.AddMask(_maskType);
            }
            _rotationTween?.Kill();
            Destroy(gameObject);
        }
        
    }
}