using UnityEngine;
using Zenject;

namespace Objectives
{
    public class DeathZone : MonoBehaviour
    {
        private LevelManager _levelManager;

        [Inject]
        private void Construct(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }
        
        public void OnTriggerEnter(Collider other)
        {
            _levelManager.RestartLevel();
        }
    }
}