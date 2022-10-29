using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pal3.postprocess
{
    public class PostProcess : MonoBehaviour
    {
        public enum PostEffectType
        {
            None,           
            UVTest,         // UV测试
            Distortion,     // 扭曲
            Mosaic,         // 马赛克
            Filter,         // 滤镜 with LUT 
            Sketch,         // 素描
            Caustics,       // 焦散
        }
        
        public PostEffectType _curPostEffectType = PostEffectType.None;
        private Dictionary<PostEffectType, PPTechnique> _techMap = new Dictionary<PostEffectType, PPTechnique>();
        void Start()
        {
            _techMap.Add(PostEffectType.None,null);
            _techMap.Add(PostEffectType.UVTest,new PPUVTest());
            _techMap.Add(PostEffectType.Distortion,new PPDistortion());
            _techMap.Add(PostEffectType.Mosaic,null);
            _techMap.Add(PostEffectType.Filter,null);
            _techMap.Add(PostEffectType.Sketch,null);
            _techMap.Add(PostEffectType.Caustics,null);

            foreach (var tech in _techMap)
            {
                tech.Value?.Init();
            }
        }

        void Update()
        {
            
        }
        
        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            PPTechnique tech = _techMap[_curPostEffectType];
            if (tech != null)
            {
                tech.Blit(src,dest);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
    }
    
}
