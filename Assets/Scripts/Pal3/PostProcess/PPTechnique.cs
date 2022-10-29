using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pal3.postprocess
{
    abstract class PPTechnique
    {
        public abstract void Init();
        public abstract void Blit(RenderTexture src, RenderTexture dest);
    }
    
}
