using UnityEngine;

namespace Mask.Controllers
{
    // Owns every temporary Rigidbody change made during a Strength interaction.
    internal sealed class StrengthHeldBody
    {
        private readonly Rigidbody _body;
        private readonly Collider[] _colliders;
        private readonly Collider[] _rotationContacts = new Collider[64];
        private readonly bool _gravity;
        private readonly float _damping;
        private readonly RigidbodyInterpolation _interpolation;
        private readonly float _skin;

        public StrengthHeldBody(Rigidbody body, float skin)
        {
            _body = body;
            _skin = skin;
            _colliders = body.GetComponentsInChildren<Collider>();
            _gravity = body.useGravity;
            _damping = body.linearDamping;
            _interpolation = body.interpolation;
            body.useGravity = false;
            body.linearDamping = 0f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        public void Tick(Vector3 desiredPosition, Quaternion desiredRotation,
            float smoothing, float moveSpeed, float rotationSpeed, float fixedDeltaTime)
        {
            if (_body == null) return;
            Vector3 delta = desiredPosition - _body.position;
            float fraction = 1f - Mathf.Exp(-smoothing * fixedDeltaTime);
            Vector3 step = Vector3.ClampMagnitude(delta * fraction, moveSpeed * fixedDeltaTime);
            if (step.sqrMagnitude > 0.000001f &&
                _body.SweepTest(step.normalized, out RaycastHit hit, step.magnitude + _skin))
                step = step.normalized * Mathf.Max(0f, hit.distance - _skin);
            _body.linearVelocity = step / fixedDeltaTime;
            _body.angularVelocity = Vector3.zero;
            Quaternion nextRotation = Quaternion.RotateTowards(_body.rotation, desiredRotation,
                rotationSpeed * fixedDeltaTime);
            if (CanRotateTo(nextRotation))
                _body.MoveRotation(nextRotation);
        }

        private bool CanRotateTo(Quaternion candidate)
        {
            Quaternion fromBody = Quaternion.Inverse(_body.rotation);
            for (int i = 0; i < _colliders.Length; i++)
            {
                Collider own = _colliders[i];
                if (own == null || !own.enabled || own.isTrigger) continue;
                Vector3 localOffset = fromBody * (own.transform.position - _body.position);
                Quaternion localRotation = fromBody * own.transform.rotation;
                Vector3 candidatePosition = _body.position + candidate * localOffset;
                Quaternion candidateRotation = candidate * localRotation;
                int count = Physics.OverlapSphereNonAlloc(candidatePosition,
                    own.bounds.extents.magnitude + _skin, _rotationContacts, ~0,
                    QueryTriggerInteraction.Ignore);
                if (count == _rotationContacts.Length) return false;
                for (int j = 0; j < count; j++)
                {
                    Collider other = _rotationContacts[j];
                    if (other == null || other.attachedRigidbody == _body) continue;
                    if (Physics.ComputePenetration(own, candidatePosition, candidateRotation,
                            other, other.transform.position, other.transform.rotation,
                            out _, out float penetration) && penetration > _skin)
                        return false;
                }
            }
            return true;
        }

        public void Release()
        {
            if (_body == null) return;
            _body.useGravity = _gravity;
            _body.linearDamping = _damping;
            _body.interpolation = _interpolation;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }
    }
}
