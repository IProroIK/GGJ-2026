using System;
using System.Collections.Generic;
using Settings;
using UnityEngine;

namespace Mask
{
    public class MaskController : MonoBehaviour
    {
        public event Action<Enums.MaskType> OnMaskEquip;
        public event Action<Enums.MaskType> OnMaskUnequip;
        public event Action<List<Enums.MaskType>> OnMaskUpdated;

        public static MaskController Instance { get; private set; }
        
        public Enums.MaskType CurrentMask { get; private set; }

        private List<Enums.MaskType> _availableMasks;

        private void Awake()
        {
            _availableMasks = new List<Enums.MaskType>();
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        
        public void SetMask(Enums.MaskType mask)
        {
            OnMaskUnequip?.Invoke(CurrentMask);
            CurrentMask = mask;
            OnMaskEquip?.Invoke(CurrentMask);
        }

        public void AddMask(Enums.MaskType mask)
        {
            if(!_availableMasks.Contains(mask))
                _availableMasks.Add(mask);
            else
            {
                Debug.LogError($"Mask {mask} is already in use");
            }
            
            OnMaskUpdated?.Invoke(_availableMasks);
        }

        public void RemoveMask(Enums.MaskType mask)
        {
            _availableMasks.Remove(mask);
            
            OnMaskUpdated?.Invoke(_availableMasks);
        }
    }
}