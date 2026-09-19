using Mask.Controllers;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Effects
{
    [DisallowMultipleComponent]
    public class StylizedBeamController : MonoBehaviour
    {
        [SerializeField] private VisualEffect _visualEffect;
        [SerializeField] private PhysicsDragController _physicsDragController;
        [SerializeField] private Transform _beamEndPoint;

        private bool _isStopped = true;
        private ParticleSystem[] _particleSystems = new ParticleSystem[0];
        private VFXBinderBase[] _bindings = new VFXBinderBase[0];
        
        private void Awake()
        {
            if (_visualEffect == null)
                _visualEffect = GetComponentInChildren<VisualEffect>(true);

            if (_visualEffect == null) return;

            // The beam prefab's light particles are siblings of its VisualEffect.
            Transform effectRoot = _visualEffect.transform.parent != null
                ? _visualEffect.transform.parent
                : _visualEffect.transform;
            _particleSystems = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
            _bindings = _visualEffect.GetComponents<VFXBinderBase>();
        }

        private void OnEnable()
        {
            if (_physicsDragController == null) return;

            _physicsDragController.HeldObjectUpdatedPositionEvent += HeldObjectUpdatedPositionEventHandler;
            _physicsDragController.ObjectGrabbedEvent += ObjectGrabbedEventHandler;
        }

        private void OnDisable()
        {
            if (_physicsDragController != null)
            {
                _physicsDragController.HeldObjectUpdatedPositionEvent -= HeldObjectUpdatedPositionEventHandler;
                _physicsDragController.ObjectGrabbedEvent -= ObjectGrabbedEventHandler;
            }

            Stop();
        }

        private void ObjectGrabbedEventHandler(bool isGrabbed)
        {
            if (isGrabbed) Play();
            else Stop();
        }

        public void Play()
        {
            if (_visualEffect == null || !_isStopped) return;

            // Apply the target before spawning the beam's first frame.
            foreach (VFXBinderBase binding in _bindings)
            {
                if (binding != null && binding.IsValid(_visualEffect))
                    binding.UpdateBinding(_visualEffect);
            }

            _visualEffect.Reinit();
            _visualEffect.Play();
            foreach (ParticleSystem particleSystem in _particleSystems)
                particleSystem.Play(false);

            _isStopped = false;
        }

        public void Stop()
        {
            if (_isStopped) return;

            _isStopped = true;
            foreach (ParticleSystem particleSystem in _particleSystems)
                particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_visualEffect != null)
            {
                _visualEffect.Stop();
                // The persistent core must be cleared, not left waiting for a lifetime.
                // This prefab's initial event is BeamManualStart, so Reinit stays idle.
                _visualEffect.Reinit();
            }
        }

        private void HeldObjectUpdatedPositionEventHandler(Vector3 position)
        {
            if (_beamEndPoint != null)
                _beamEndPoint.position = position;
        }
    }
}
