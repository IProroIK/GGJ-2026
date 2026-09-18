using Settings;
using UnityEngine;
using UnityEngine.VFX;

namespace Player
{
    public class PlayerInteractionParticles
    {
        public ParticleSystem ParticleSystem;
        public VisualEffect  VisualEffect;

        [SerializeField] private Enums.MaskType _maskType;

        public Enums.MaskType GetMaskType()
        {
            return _maskType;
        }

        private void Awake()
        {
            
        }
            
        public void Play()
        {
            ParticleSystem?.Play();
            VisualEffect?.Play();
        }

        public void Stop()
        {
            ParticleSystem?.Stop();
            VisualEffect?.Stop();
        }
    }
}