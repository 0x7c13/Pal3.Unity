using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pal3.postprocess
{
    abstract class PPTechnique
    {
        protected Material _material = null;
        public PPTechnique(Material material)
        {
            _material = material;
        }
        
        public abstract void Blit(RenderTexture src, RenderTexture dest);
    }
    
}
