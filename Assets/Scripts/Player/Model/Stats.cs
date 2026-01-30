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

        public float speedModifier = 1;
        public float gravityModifier = 1;
        public float jumpForceModifier = 1;
        
        public float GetSpeed()
        {
            return speed * speedModifier;
        }

        public float GetGravity()
        {
            return gravity * gravityModifier;
        }

        public float GetJumpForce()
        {
            return jumpForce * jumpForceModifier;
        }
    }
}