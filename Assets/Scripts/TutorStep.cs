using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public sealed class TutorStep : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private List<GameObject> objectsToActivate;
        [SerializeField] private TutorStep _nextTutorStep;
        
        private bool _activated;

        private void OnTriggerStay(Collider other)
        {
            if (_activated)
                return;

            if (!other.CompareTag(playerTag))
                return;

            Activate();
        }

        private void OnTriggerExit(Collider other)
        {
            
            if (!other.CompareTag(playerTag))
                return;

            Deactivate();
        }

        private void Activate()
        {
            _activated = true;

            for (int i = 0; i < objectsToActivate.Count; i++)
            {
                if (objectsToActivate[i] != null)
                    objectsToActivate[i].SetActive(true);
            }
        }

        private void Deactivate()
        {
            for (int i = 0; i < objectsToActivate.Count; i++)
            {
                if (objectsToActivate[i] != null)
                    objectsToActivate[i].SetActive(false);
            }
            
            _nextTutorStep?.gameObject.SetActive(true);
        }
    }
}