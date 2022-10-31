using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pal3.postprocess
{
    class PPDistortion : PPTechnique
    {

        public PPDistortion(Material material) : base(material)
        {
        }

        public override void Blit(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest, _material);
        }

    }
}
