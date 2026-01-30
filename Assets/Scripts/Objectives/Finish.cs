using UnityEngine;
using Zenject;

namespace Objectives
{
    public class Finish : MonoBehaviour
    {
        private LevelManager _levelManager;

        [Inject]
        private void Construct(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        private void OnTriggerEnter(Collider other)
        {
            _levelManager.LoadNextLevel();
        }
        
    }
}