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
            Edge,           // 边缘检测 with 后处理卷积
            Grey,           // 用亮度 ,填充颜色为灰色
            Mosaic,         // 马赛克
            Filter,         // 滤镜 with LUT 
            Sketch,         // 素描
            Caustics,       // 焦散
        }
        
        public PostEffectType _curPostEffectType = PostEffectType.None;
        //public Dictionary<PostEffectType, Material> _materialMap = new Dictionary<PostEffectType, Material>();
        private Dictionary<PostEffectType, PPTechnique> _techMap = new Dictionary<PostEffectType, PPTechnique>();

        public Material _matNone;
        public Material _matUVTest;
        public Material _matDistortion;
        public Material _matEdge;
        public Material _matGrey;
        public Material _matMosaic;
        public Material _matFilter;
        public Material _matSketch;
        public Material _matCaustics;
        
        void Start()
        {
            _techMap.Add(PostEffectType.None,null);
            _techMap.Add(PostEffectType.UVTest,new PPUVTest(_matUVTest));
            _techMap.Add(PostEffectType.Distortion,new PPDistortion(_matDistortion));
            _techMap.Add(PostEffectType.Edge,new PPDistortion(_matEdge));
            _techMap.Add(PostEffectType.Grey,new PPDistortion(_matGrey));
            _techMap.Add(PostEffectType.Mosaic,null);
            _techMap.Add(PostEffectType.Filter,null);
            _techMap.Add(PostEffectType.Sketch,null);
            _techMap.Add(PostEffectType.Caustics,null);
        }

        void Update()
        {
            
        }
        
        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            PPTechnique tech = _techMap[_curPostEffectType];
            if (tech != null && tech.GetMaterial() != null)
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
