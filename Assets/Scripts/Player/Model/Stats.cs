using System;
using UnityEngine;

namespace Player.Model
{
    [Serializable]
    public class Stats
    {
        [SerializeField] private float speed;
        [SerializeField] private float gravity;
        [SerializeField] private float jumpForce;
        [SerializeField] private int jumpCount = 1;

        [HideInInspector] public float SpeedModifier = 1;
        [HideInInspector] public float GravityModifier = 1;
        [HideInInspector] public float JumpForceModifier = 1;
        [HideInInspector] public int JumpCountModifier = 1;
        
        public float GetSpeed()
        {
            return speed * SpeedModifier;
        }

        public float GetGravity()
        {
            return gravity * GravityModifier;
        }

        public float GetJumpForce()
        {
            return jumpForce * JumpForceModifier;
        }

        public int GetJumpCount()
        {
            return jumpCount *  JumpCountModifier;
        }
    }
}