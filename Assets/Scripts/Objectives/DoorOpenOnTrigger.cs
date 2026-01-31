using DG.Tweening;
using UnityEngine;

namespace Objectives
{
    public sealed class DoorOpenOnTrigger : MonoBehaviour
    {
        [Header("Door Parts")]
        [SerializeField] private Transform doorLeft;
        [SerializeField] private Transform doorRight;

        [Header("End Rotations (Local)")]
        [SerializeField] private Vector3 leftDoorEndRotation;
        [SerializeField] private Vector3 rightDoorEndRotation;

        [Header("Animation")]
        [SerializeField] private float openDuration = 1f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Header("Trigger")]
        [SerializeField] private string triggerTag = "Player";

        private bool _isOpened;

        private void OnTriggerEnter(Collider other)
        {
            if (_isOpened)
                return;

            if (!other.CompareTag(triggerTag))
                return;

            OpenDoor();
        }

        private void OpenDoor()
        {
            _isOpened = true;

            if (doorLeft != null)
            {
                doorLeft
                    .DOLocalRotate(leftDoorEndRotation, openDuration)
                    .SetEase(ease);
            }

            if (doorRight != null)
            {
                doorRight
                    .DOLocalRotate(rightDoorEndRotation, openDuration)
                    .SetEase(ease);
            }
        }
    }
}