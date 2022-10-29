using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pal3.postprocess
{
    class PPDistortion : PPTechnique
    {
        private Material _material = null;
        public override void Init()
        {
            _material = new Material(Shader.Find("Pal3/postprocess/Distortion"));
        }

        public override void Blit(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src,dest,_material);
        }
    }
    
}
