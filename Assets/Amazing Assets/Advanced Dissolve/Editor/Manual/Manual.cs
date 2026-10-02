// Advanced Dissolve <https://u3d.as/16cX>
// Copyright (c) Amazing Assets <https://amazingassets.world>

using System;

using UnityEngine;


namespace AmazingAssets.AdvancedDissolve.Editor.Manual
{
    public class Manual : ScriptableObject
    {
        public enum URLType { OpenPage, MailTo }


        public Texture2D icon;
        public Section[] sections;

        [Serializable]
        public class Section
        {
            public string heading, text, linkText, url;
            public URLType urlType;

            public Section()
            {

            }

            public Section(string heading, string text, string linkText, string url, URLType urlType = URLType.OpenPage)
            {
                this.heading = heading;
                this.text = text;
                this.linkText = linkText;
                this.url = url;
                this.urlType = urlType;
            }
        }
    }
}
 