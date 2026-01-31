using System;
using System.Collections.Generic;
using System.Linq;
using Settings;
using Sirenix.OdinInspector;
using TrailsFX;
using UnityEngine;
using Zenject;

namespace Mask.Controllers
{
    public class WorldHighlightController : MonoBehaviour
    {
        [SerializeField] private List<QuickOutline> _quickOutlines;
        [SerializeField] private List<TrailEffect> _trailEffects;
        [SerializeField] private Enums.MaskType[] _maskTypes = { Enums.MaskType.Intelligence };
        private MaskManager _maskManager;

        [Button]
        public void FindInScene()
        {
            _quickOutlines = FindObjectsByType<QuickOutline>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).ToList();
            _trailEffects =  FindObjectsByType<TrailEffect>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).ToList();
        }

        [Inject]
        private void Construct(MaskManager maskManager)
        {
            _maskManager = maskManager;
        }

        private void Awake()
        {
            SetEffects(false);
        }

        private void OnEnable()
        {
            _maskManager.OnMaskEquip += EnableEffects;
            _maskManager.OnMaskUnequip += DisableEffects;
        }

        private void OnDisable()
        {
            _maskManager.OnMaskEquip -= EnableEffects;
            _maskManager.OnMaskUnequip -= DisableEffects;
        }
        
        private void EnableEffects(Enums.MaskType maskType)
        {
            if (!IsAllowed(maskType))
                return;

            SetEffects(true);
        }

        private void DisableEffects(Enums.MaskType maskType)
        {
            if (!IsAllowed(maskType))
                return;

            SetEffects(false);
        }

        private void SetEffects(bool enable)
        {
            foreach (var q in _quickOutlines)
                if (q != null) q.enabled = enable;

            foreach (var t in _trailEffects)
                if (t != null) t.enabled = enable;
        }
        
        private bool IsAllowed(Enums.MaskType maskType) => _maskTypes != null && _maskTypes.Contains(maskType);
    }
}