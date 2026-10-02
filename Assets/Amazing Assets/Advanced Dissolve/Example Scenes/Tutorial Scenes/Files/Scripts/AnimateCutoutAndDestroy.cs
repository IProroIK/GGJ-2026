// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.AdvancedDissolve.Examples
{
    public class AnimateCutoutAndDestroy : MonoBehaviour
    {
        public Material material;

        float clipValue = 0;
        float dissolveSpeed = 0.1f;


        private void Start()
        {
            clipValue = 0;
            dissolveSpeed = Random.Range(0.15f, 0.35f);


            //Enable dissolve 'State' keyword for this material
            AdvancedDissolveKeywords.SetKeyword(material, AdvancedDissolveKeywords.State.Enabled, true);

            //Make sure initial clip value is 0
            AdvancedDissolveProperties.Cutout.Standard.UpdateLocalProperty(material, AdvancedDissolveProperties.Cutout.Standard.Property.Clip, 0);

            //Assign random edge color
            AdvancedDissolveProperties.Edge.Base.UpdateLocalProperty(material, AdvancedDissolveProperties.Edge.Base.Property.Color, new Color(Random.value, Random.value, Random.value));

            //Randomize intensity
            AdvancedDissolveProperties.Edge.Base.UpdateLocalProperty(material, AdvancedDissolveProperties.Edge.Base.Property.ColorIntensity, Random.Range(5f, 8f));

            //Set edge shape
            AdvancedDissolveProperties.Edge.Base.UpdateLocalProperty(material, AdvancedDissolveProperties.Edge.Base.Property.Shape, AdvancedDissolveProperties.Edge.Base.Shape.Smoother);

        }

        void Update()
        {
            //Update 'Clip' property inside material
            AdvancedDissolveProperties.Cutout.Standard.UpdateLocalProperty(material, AdvancedDissolveProperties.Cutout.Standard.Property.Clip, clipValue);


            //Animate clip value
            clipValue += Time.deltaTime * dissolveSpeed;
            

            //Distroy after full dissolve
            if(clipValue >= 1)
                DestroyImmediate(this.gameObject);           
        }
    }
}
