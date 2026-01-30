using System;
using Settings;
using UnityEngine;

namespace Mask
{
    public class MaskController : MonoBehaviour
    {
        public event Action<Enums.MaskType> OnMaskEquip;
        public event Action<Enums.MaskType> OnMaskUnequip;

        public Enums.MaskType CurrentMask;
        
        
    }
}