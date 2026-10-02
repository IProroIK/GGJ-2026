using UnityEngine;

public interface IPlayerMotor
{
    bool IsGrounded { get; }
    Vector3 Forward { get; }
    Vector3 Right { get; }
    bool IsLowProfile { get; }
    CollisionFlags LastCollisionFlags { get; }
    void Move(Vector3 motion);
    void Rotate(float yawDegrees);
    bool TrySetLowProfile(bool lowProfile, float heightRatio = 0.55f);
    void RestoreStandingHeight();
    bool TryFindWall(Vector3 travelDirection, float distance, LayerMask layers, out Vector3 normal);
    bool TryFindJumpWall(float distance, LayerMask layers, out Vector3 normal);
}
