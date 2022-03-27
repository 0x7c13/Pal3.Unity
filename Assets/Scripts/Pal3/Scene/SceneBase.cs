// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene
{
    using System.Collections.Generic;
    using Actor;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
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
            InitLgtData();
            InitSceneObjectData();
            InitActorData();
        }

        private void InitMeshData()
        {
            var separator = CpkConstants.CpkDirectorySeparatorChar;

            var meshFileRelativePath = $"{ScnFile.SceneInfo.CityName}.cpk{separator}" +
                                   $"{ScnFile.SceneInfo.Model}{separator}";

            // Switch to night version of the mesh model when LightMap flag is set to 1
            if (ScnFile.SceneInfo.LightMap == 1)
            {
                meshFileRelativePath += $"1{separator}";
            }

            ScenePolyMesh = _resourceProvider.GetPol(meshFileRelativePath + $"{ScnFile.SceneInfo.Model}.pol");

            // Only few of the scenes use CVD models, so we need to check first
            if (_resourceProvider.FileExists(meshFileRelativePath + $"{ScnFile.SceneInfo.Model}.cvd"))
            {
                SceneCvdMesh = _resourceProvider.GetCvd(meshFileRelativePath + $"{ScnFile.SceneInfo.Model}.cvd");
            }
        }

        private void InitNavData()
        {
            NavFile = _resourceProvider.GetNav(ScnFile.SceneInfo.CityName, ScnFile.SceneInfo.Model);
            Tilemap = new Tilemap(NavFile);
        }

        private void InitLgtData()
        {
            // TODO: Impl
        }

        private void InitSceneObjectData()
        {
            foreach (var objectInfo in ScnFile.ObjectInfos)
            {
                if (SceneObjectFactory.Create(objectInfo, ScnFile.SceneInfo) is { } sceneObject)
                {
                    SceneObjects[objectInfo.Id] = sceneObject;
                }
            }
        }

        private void InitActorData()
        {
            foreach (var npcInfo in ScnFile.NpcInfos)
            {
                Actors[npcInfo.Id] = new Actor(_resourceProvider, npcInfo);
            }

            foreach (var playerInfo in PlayerActorNpcInfo.GetAll())
            {
                if (!Actors.ContainsKey(playerInfo.Id))
                {
                    Actors[playerInfo.Id] = new Actor(_resourceProvider, playerInfo);
                }
            }
        }
    }
}