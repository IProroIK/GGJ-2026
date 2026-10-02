// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.AdvancedDissolve
{
    [ExecuteAlways]
    public class AdvancedDissolveTransformScaleToRadius : MonoBehaviour
    {
        public AdvancedDissolveGeometricCutoutController geometricCutoutController;
        public AdvancedDissolveKeywords.CutoutGeometricCount countID;

      
       
        void Update()
        {
            if (geometricCutoutController == null)
                return;


            float radius = transform.lossyScale.x * .5f;

            geometricCutoutController.SetTargetStartPointTransform(countID, transform);
            geometricCutoutController.SetTargetRadius(countID, radius);
        }
    }
}
