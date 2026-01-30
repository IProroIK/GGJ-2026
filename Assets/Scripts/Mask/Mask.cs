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

        private Sequence _animationSequenceMove;
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
            _animationSequenceMove?.Kill();
        }
        
        private void StartAnimation()
        {
            _animationSequenceMove?.Kill();
            _animationSequenceMove =  DOTween.Sequence();
                        
            _animationSequenceMove.Append(_maskTransform.DOLocalMoveY(moveDistance, MoveTime))
                .Join(_maskTransform.DOLocalRotate(new Vector3(359, 0, 0), MoveTime * 2, RotateMode.FastBeyond360))
                .Append(_maskTransform.DOLocalMoveY(-moveDistance, MoveTime))
                .SetEase(Ease.InOutCubic).SetLoops(-1, LoopType.Yoyo);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Player.Player player))
            {
                _maskManager.AddMask(_maskType);
            }
            
            Destroy(gameObject);
        }
        
    }
}