using System.Collections;
using System.Collections.Generic;
using Core.DataLoader;
using Core.DataReader.Mv3;
using Core.Services;
using Pal3.Data;
using Pal3.Renderer;
using Unity.VisualScripting;
using UnityEngine;


namespace Pal3.playground
{
    public class Playground : MonoBehaviour
    {
        void Start()
        {
            LoadMv3();
        }
        
        void Update()
        {
        
        }
        
        void LoadMv3()
        {
            GameResourceProvider grp = ServiceLocator.Instance.Get<GameResourceProvider>();
            (Mv3File mv3File, ITextureResourceProvider textureProvider) = grp.GetMv3("basedata.cpk\\ROLE\\101\\C01.MV3");
            

            GameObject go = new GameObject();
            var mv3Renderer = go.GetOrAddComponent<Mv3ModelRenderer>();
            mv3Renderer.Init(mv3File,grp.GetMaterialFactory(),textureProvider,Color.white);
        }
    }
    
}
