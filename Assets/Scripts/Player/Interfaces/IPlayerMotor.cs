using UnityEngine;

public interface IPlayerMotor
{
        Vector3 Position { get; }
        void Move(Vector3 delta);
        bool IsGrounded { get; }
}
