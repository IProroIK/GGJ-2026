// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using UnityEngine;


namespace AmazingAssets.AdvancedDissolve.Examples
{
    public class AnimateGI : MonoBehaviour
    {
        Renderer mRenderer;


        private void Start()
        {
            mRenderer = GetComponent<Renderer>();
        }

        // Update is called once per frame
        void Update()
        {
            //We need to update Unity GI every time we change material properties effecting GI
            RendererExtensions.UpdateGIMaterials(mRenderer);
        }
    }
}
