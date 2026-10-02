using System.Collections.Generic;
using Settings;
using UnityEngine;
using Zenject;

namespace Mask.Controllers
{
    public sealed class StrengthRangeIndicator : MonoBehaviour
    {
        [SerializeField] private Transform _circle;
        [Tooltip("Measured world-space diameter at the circle's original prefab scale.")]
        [SerializeField] private float _referenceDiameter = 6.8f;

        private MaskManager _masks;
        private PhysicsDragController _drag;
        private ParticleSystem[] _particles;
        private Vector3 _referenceWorldScale;
        private Player.Player _player;

        [Inject]
        private void Construct(MaskManager masks, Player.Player player)
        {
            _player = player;
            _masks = masks;
        }

        private void Awake()
        {
            _drag = FindFirstObjectByType<PhysicsDragController>();
            if (_circle == null) return;

            _referenceWorldScale = _circle.lossyScale;
            _particles = _circle.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particle in _particles)
            {
                var main = particle.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
            SetVisible(false);
        }

        private void OnEnable()
        {
            _masks.OnMaskEquip += OnMaskEquipped;
            _masks.OnMaskUnequip += OnMaskUnequipped;
            _masks.OnMaskUpdated += OnMasksUpdated;
            OnMaskEquipped(_masks.CurrentMask);
        }

        private void OnDisable()
        {
            _masks.OnMaskEquip -= OnMaskEquipped;
            _masks.OnMaskUnequip -= OnMaskUnequipped;
            _masks.OnMaskUpdated -= OnMasksUpdated;
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (_circle != null && _circle.gameObject.activeSelf)
            {
                _circle.position = _player.transform.position;
                UpdateScale();
            }
        }

        private void OnMaskEquipped(Enums.MaskType mask)
        {
            SetVisible(mask == Enums.MaskType.Strength);
        }

        private void OnMaskUnequipped(Enums.MaskType mask)
        {
            SetVisible(false);
        }

        private void OnMasksUpdated(List<Enums.MaskType> masks)
        {
            SetVisible(_masks.CurrentMask == Enums.MaskType.Strength &&
                       masks.Contains(Enums.MaskType.Strength));
        }

        private void SetVisible(bool visible)
        {
            if (_circle == null) return;
            visible &= _drag != null;
            if (visible) UpdateScale();
            if (_circle.gameObject.activeSelf == visible) return;

            if (!visible)
            {
                foreach (ParticleSystem particle in _particles)
                    particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            _circle.gameObject.SetActive(visible);
            if (visible)
            {
                foreach (ParticleSystem particle in _particles)
                    particle.Play(false);
            }
        }

        private void UpdateScale()
        {
            // Compensate for the player's scale so the circle radius is in world units.
            float radiusScale =  Mathf.Max(0f, _drag.MaxDragDistance) / _referenceDiameter;
            Vector3 parentScale = _circle.parent != null ? _circle.parent.lossyScale : Vector3.one;
            Vector3 scale = _circle.localScale;
            scale.x = _referenceWorldScale.x * radiusScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x));
            scale.z = _referenceWorldScale.z * radiusScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z));
            _circle.localScale = scale;
        }
    }
}
