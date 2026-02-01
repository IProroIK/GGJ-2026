using UnityEngine;

public interface IPlayerMotor
{
    bool IsGrounded { get; }
    Vector3 Forward { get; }
    Vector3 Right { get; }
    void Move(Vector3 motion);
    void Rotate(float yawDegrees);
}
