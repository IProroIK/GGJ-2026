// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>
 
using UnityEngine;


namespace AmazingAssets.AdvancedDissolve.Examples
{
    public class Viper : MonoBehaviour
    {
        static Vector3 moveDirection = new Vector3(0, 0, -1);
        static float deathZone = -200;



        Material material;

        float moveSpeed;
        

        void Start()
        {
            moveSpeed = Random.Range(3f, 10f);


            //Instantiate material
            material = GetComponent<Renderer>().material;



            //Enable 'State' keyword
            AdvancedDissolveKeywords.SetKeyword(material, AdvancedDissolveKeywords.State.Enabled, true);

            //Enable 'Plane' Geometric cutout
            AdvancedDissolveKeywords.SetKeyword(material, AdvancedDissolveKeywords.CutoutGeometricType.Plane, true);
            AdvancedDissolveKeywords.SetKeyword(material, AdvancedDissolveKeywords.CutoutGeometricCount.Two, true);

            //Enable 'Edge' rendering
            AdvancedDissolveKeywords.SetKeyword(material, AdvancedDissolveKeywords.EdgeBaseSource.CutoutGeometric, true);

            //Set 'Edge' width
            AdvancedDissolveProperties.Edge.Base.UpdateLocalProperty(material, AdvancedDissolveProperties.Edge.Base.Property.WidthGeometric, 0.2f);

            //Set 'Edge' color and its intensity
            AdvancedDissolveProperties.Edge.Base.UpdateLocalProperty(material, AdvancedDissolveProperties.Edge.Base.Property.Color, Random.ColorHSV(0f, 1f, 1, 1, 1, 1));
            AdvancedDissolveProperties.Edge.Base.UpdateLocalProperty(material, AdvancedDissolveProperties.Edge.Base.Property.ColorIntensity, Random.Range(4f, 6f));


            //Add current material to the controller's materials array
            Spawner.active.controller.AddMaterial(material);

        }

        void Update()
        {
            transform.Translate(moveDirection * Time.deltaTime * moveSpeed, Space.World);
        }

        private void FixedUpdate()
        {
            if (transform.position.z < deathZone)
                DestroyImmediate(this.gameObject);
        }
    }
}
