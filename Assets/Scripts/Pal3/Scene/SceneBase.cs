// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System;
    using System.Collections.Generic;
    using Actor;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Lgt;
    using Core.DataReader.Nav;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Data;
    using Player;
    using SceneObjects;
    using UnityEngine;

    /// <summary>
    /// Scene base class used to init and hold scene data models.
    /// </summary>
    public abstract class SceneBase : MonoBehaviour
    {
        protected Tilemap Tilemap;
        protected ScnFile ScnFile;
        protected NavFile NavFile;
        //protected LgtFile LgtFile;

        protected (PolFile PolFile, ITextureResourceProvider TextureProvider) ScenePolyMesh;
        protected (CvdFile CvdFile, ITextureResourceProvider TextureProvider)? SceneCvdMesh;

        protected readonly Dictionary<byte, SceneObject> SceneObjects = new ();
        protected readonly Dictionary<byte, Actor> Actors = new ();

        private GameResourceProvider _resourceProvider;

        protected void Init(GameResourceProvider resourceProvider, ScnFile scnFile)
        {
            _resourceProvider = resourceProvider;

            ScnFile = scnFile;

            InitMeshData();
            InitNavData();
            InitSceneObjectData();
            InitActorData();
        }

        protected bool IsNightScene()
        {
            #if PAL3
            if (ScnFile.SceneInfo.LightMap == 1)
            #elif PAL3A
            if (ScnFile.SceneInfo.LightMap == 1 ||
                ScnFile.SceneInfo.Name.EndsWith("y", StringComparison.OrdinalIgnoreCase) ||
                ScnFile.SceneInfo.Name.Equals("q11", StringComparison.OrdinalIgnoreCase)) // 蜀山深夜场景
            #endif
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private void InitMeshData()
        {
            var separator = CpkConstants.DirectorySeparator;

            var meshFileRelativePath = $"{ScnFile.SceneInfo.CityName}{CpkConstants.FileExtension}{separator}" +
                                   $"{ScnFile.SceneInfo.Model}{separator}";

            // Switch to night version of the mesh model when LightMap flag is set to 1
            if (ScnFile.SceneInfo.LightMap == 1)
            {
                meshFileRelativePath += $"1{separator}";
            }

            var sceneMetadataFilePrefix = meshFileRelativePath + ScnFile.SceneInfo.Model;
            
            ScenePolyMesh = _resourceProvider.GetPol(sceneMetadataFilePrefix + ".pol");

            // Only few of the scenes use CVD models, so we need to check first
            if (_resourceProvider.FileExists(sceneMetadataFilePrefix + ".cvd"))
            {
                SceneCvdMesh = _resourceProvider.GetCvd(sceneMetadataFilePrefix + ".cvd");
            }
            
            #if RTX_ON
            // Check if light file exists
            // if (_resourceProvider.FileExists(sceneMetadataFilePrefix + ".lgt"))
            // {
            //     LgtFile = _resourceProvider.GetLgt(sceneMetadataFilePrefix + ".lgt");
            // }
            #endif
        }

        private void InitNavData()
        {
            NavFile = _resourceProvider.GetNav(ScnFile.SceneInfo.CityName, ScnFile.SceneInfo.Model);
            Tilemap = new Tilemap(NavFile);
        }
        
        private void InitSceneObjectData()
        {
            foreach (ScnObjectInfo objectInfo in ScnFile.ObjectInfos)
            {
                if (SceneObjectFactory.Create(objectInfo, ScnFile.SceneInfo) is { } sceneObject)
                {
                    SceneObjects[objectInfo.Id] = sceneObject;
                }
            }
        }

        private void InitActorData()
        {
            foreach (ScnNpcInfo npcInfo in ScnFile.NpcInfos)
            {
                Actors[npcInfo.Id] = new Actor(_resourceProvider, npcInfo);
            }

            foreach (ScnNpcInfo playerInfo in PlayerActorNpcInfoFactory.CreateAll())
            {
                if (!Actors.ContainsKey(playerInfo.Id))
                {
                    Actors[playerInfo.Id] = new Actor(_resourceProvider, playerInfo);
                }
            }

            #if PAL3A
            foreach (ScnNpcInfo fengYaSongInfo in FengYaSongActorNpcInfoFactory.CreateAll())
            {
                if (!Actors.ContainsKey(fengYaSongInfo.Id))
                {
                    Actors[fengYaSongInfo.Id] = new Actor(_resourceProvider, fengYaSongInfo);
                }
                else
                {
                    Debug.LogError("FengYsSong actor id is already used by another actor!");
                }
            }
            #endif
        }
    }
}