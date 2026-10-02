using UnityEngine;

namespace Player.Controllers
{
    public sealed class CharacterControllerMotor : IPlayerMotor
    {
        private readonly CharacterController _controller;
        private readonly Transform _transform;
        private readonly float _standingHeight;
        private readonly Vector3 _standingCenter;
        private readonly float _standingStepOffset;
        private readonly Collider[] _overlaps = new Collider[32];
        private readonly RaycastHit[] _wallHits = new RaycastHit[16];

        public CharacterControllerMotor(CharacterController controller)
        {
            _controller = controller;
            _transform = controller.transform;
            _standingHeight = controller.height;
            _standingCenter = controller.center;
            _standingStepOffset = controller.stepOffset;
        }

        public Vector3 Position => _transform.position;
        public bool IsGrounded => _controller.isGrounded;
        public Vector3 Forward => _transform.forward;
        public Vector3 Right => _transform.right;
        public bool IsLowProfile { get; private set; }
        public CollisionFlags LastCollisionFlags { get; private set; }

        public void Move(Vector3 motion)
        {
            LastCollisionFlags = _controller.Move(motion);
        }

        public void Rotate(float yawDegrees)
        {
            if (yawDegrees == 0f) return;
            
            _transform.Rotate(0f, yawDegrees, 0f, Space.World);
        }

        public bool TrySetLowProfile(bool lowProfile, float heightRatio = 0.55f)
        {
            if (lowProfile == IsLowProfile) return true;
            if (!lowProfile)
            {
                if (!HasStandingRoom()) return false;
                RestoreStandingHeight();
                return true;
            }

            float height = Mathf.Clamp(_standingHeight * heightRatio, _controller.radius * 2f, _standingHeight);
            _controller.stepOffset = Mathf.Min(_standingStepOffset, height * 0.5f);
            _controller.height = height;
            // Keep the feet in place when the collision capsule shrinks.
            _controller.center = _standingCenter - Vector3.up * ((_standingHeight - height) * 0.5f);
            IsLowProfile = true;
            return true;
        }

        public void RestoreStandingHeight()
        {
            _controller.height = _standingHeight;
            _controller.center = _standingCenter;
            _controller.stepOffset = _standingStepOffset;
            IsLowProfile = false;
        }

        private bool HasStandingRoom()
        {
            Vector3 scale = _transform.lossyScale;
            float radius = _controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(radius, _standingHeight * Mathf.Abs(scale.y) * 0.5f);
            Vector3 center = _transform.TransformPoint(_standingCenter);
            float inset = Mathf.Max(0.01f, _controller.skinWidth * 0.5f);
            int count = Physics.OverlapCapsuleNonAlloc(
                center - _transform.up * (halfHeight - radius),
                center + _transform.up * (halfHeight - radius),
                Mathf.Max(0.01f, radius - inset), _overlaps, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            // A full buffer is ambiguous: stay low rather than expand into an unseen collider.
            if (count == _overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (IsSolidObstacle(_overlaps[i])) return false;
            return true;
        }

        public bool TryFindWall(Vector3 travelDirection, float distance, LayerMask layers, out Vector3 normal)
        {
            Vector3 side = Vector3.Cross(Vector3.up, travelDirection).normalized;
            float closest = float.PositiveInfinity;
            normal = Vector3.zero;
            ProbeWall(side, distance, layers, ref closest, ref normal);
            ProbeWall(-side, distance, layers, ref closest, ref normal);
            return closest < float.PositiveInfinity;
        }

        public bool TryFindJumpWall(float distance, LayerMask layers, out Vector3 normal)
        {
            float closest = float.PositiveInfinity;
            normal = Vector3.zero;
            // Include frontal contact and diagonals; wall jumping does not require running alongside it.
            for (int i = 0; i < 8; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(i * 45f, Vector3.up) * Vector3.forward;
                ProbeWall(direction, distance, layers, ref closest, ref normal);
            }
            return closest < float.PositiveInfinity;
        }

        private void ProbeWall(Vector3 direction, float distance, LayerMask layers, ref float closest, ref Vector3 normal)
        {
            Vector3 scale = _transform.lossyScale;
            float radius = _controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            Vector3 origin = _transform.TransformPoint(_controller.center);
            int count = Physics.RaycastNonAlloc(origin, direction, _wallHits,
                radius + distance, layers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _wallHits[i];
                if (!IsSolidObstacle(hit.collider) || Mathf.Abs(hit.normal.y) > 0.2f || hit.distance >= closest)
                    continue;
                closest = hit.distance;
                normal = hit.normal;
            }
        }

        private bool IsSolidObstacle(Collider other)
        {
            return other != null && other != _controller && !other.transform.IsChildOf(_transform)
                && !Physics.GetIgnoreLayerCollision(_controller.gameObject.layer, other.gameObject.layer)
                && !Physics.GetIgnoreCollision(_controller, other);
        }
    }
}
