// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene
{
    using System;
    using System.Collections.Generic;
    using Actor;
    using Core.Contract.Constants;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Lgt;
    using Core.DataReader.Nav;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.Utilities;
    using Data;
    using Engine.Abstraction;
    using Engine.DataLoader;
    using SceneObjects;
    using State;

    /// <summary>
    /// Scene base class used to init and hold scene data models.
    /// </summary>
    public abstract class SceneBase : GameEntityScript
    {
        private const float ACTOR_MOVEMENT_MAX_Y_DIFFERENTIAL_DEFAULT = 2.2f;
        private const float ACTOR_MOVEMENT_MAX_Y_DIFFERENTIAL_CROSS_LAYER_DEFAULT = 2f;
        private const float ACTOR_MOVEMENT_MAX_Y_DIFFERENTIAL_CROSS_PLATFORM_DEFAULT = 2f;

        protected float ActorMovementMaxYDifferential;
        protected float ActorMovementMaxYDifferentialCrossLayer;
        protected float ActorMovementMaxYDifferentialCrossPlatform;

        protected Tilemap Tilemap;
        protected ScnFile ScnFile;
        protected NavFile NavFile;
        //protected LgtFile LgtFile;

        protected (PolFile PolFile, ITextureResourceProvider TextureProvider) ScenePolyMesh;
        protected (CvdFile CvdFile, ITextureResourceProvider TextureProvider)? SceneCvdMesh;

        protected readonly Dictionary<int, SceneObject> SceneObjects = new ();
        protected readonly Dictionary<int, Actor> Actors = new ();

        private HashSet<int> _sceneObjectIdsToNotLoadFromSaveState;

        private GameResourceProvider _resourceProvider;
        private SceneStateManager _sceneStateManager;

        protected void Init(GameResourceProvider resourceProvider,
            ScnFile scnFile,
            SceneStateManager sceneStateManager,
            HashSet<int> sceneObjectIdsToNotLoadFromSaveState)
        {
            _resourceProvider = resourceProvider;
            _sceneStateManager = sceneStateManager;
            _sceneObjectIdsToNotLoadFromSaveState = sceneObjectIdsToNotLoadFromSaveState;

            ScnFile = scnFile;

            InitActorMovementMaxYDifferentialValues();
            InitMeshData();
            InitNavData();
            InitSceneObjectData();
            InitActorData();
        }

        private void InitActorMovementMaxYDifferentialValues()
        {
            ActorMovementMaxYDifferential = ACTOR_MOVEMENT_MAX_Y_DIFFERENTIAL_DEFAULT;
            ActorMovementMaxYDifferentialCrossLayer = ACTOR_MOVEMENT_MAX_Y_DIFFERENTIAL_CROSS_LAYER_DEFAULT;
            ActorMovementMaxYDifferentialCrossPlatform = ACTOR_MOVEMENT_MAX_Y_DIFFERENTIAL_CROSS_PLATFORM_DEFAULT;

            #if PAL3 // Make sure actor cannot walk out of the trap holes in m10_2
            if (ScnFile.SceneInfo.Is("m10", "2"))
            {
                ActorMovementMaxYDifferentialCrossLayer = 0.2f;
            }
            #endif
        }

        protected bool IsNightScene()
        {
            #if PAL3
            if (ScnFile.SceneInfo.LightMap == 1)
            #elif PAL3A
            if (ScnFile.SceneInfo.LightMap == 1 ||
                ScnFile.SceneInfo.SceneName.EndsWith("y", StringComparison.OrdinalIgnoreCase) ||
                ScnFile.SceneInfo.IsCity("q11")) // 蜀山深夜场景
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
            var separator = CpkConstants.DirectorySeparatorChar;

            var meshFileRelativePath = $"{ScnFile.SceneInfo.CityName}{CpkConstants.FileExtension}{separator}" +
                                   $"{ScnFile.SceneInfo.Model}{separator}";

            // Switch to night version of the mesh model when LightMap flag is set to 1
            if (ScnFile.SceneInfo.LightMap == 1)
            {
                meshFileRelativePath += $"1{separator}";
            }

            var sceneMetadataFilePrefix = meshFileRelativePath + ScnFile.SceneInfo.Model;

            ITextureResourceProvider sceneTextureProvider = _resourceProvider.CreateTextureResourceProvider(
                CoreUtility.GetDirectoryName(sceneMetadataFilePrefix, separator));

            PolFile polFile = _resourceProvider.GetGameResourceFile<PolFile>(sceneMetadataFilePrefix + ".pol");
            ScenePolyMesh = (polFile, sceneTextureProvider);

            // Only few of the scenes use CVD models, so we need to check first
            if (_resourceProvider.FileExists(sceneMetadataFilePrefix + ".cvd"))
            {
                CvdFile cvdFile = _resourceProvider.GetGameResourceFile<CvdFile>(sceneMetadataFilePrefix + ".cvd");
                SceneCvdMesh = (cvdFile, sceneTextureProvider);
            }

            // The light file in the original game data does not have good enough
            // information to be used for lighting, so we are not using it for now.
            // if (_resourceProvider.FileExists(sceneMetadataFilePrefix + ".lgt"))
            // {
            //     LgtFile = _resourceProvider.GetGameResourceFile<LgtFile>(sceneMetadataFilePrefix + ".lgt");
            // }
        }

        private void InitNavData()
        {
            NavFile = _resourceProvider.GetGameResourceFile<NavFile>(
                FileConstants.GetNavFileVirtualPath(ScnFile.SceneInfo.CityName, ScnFile.SceneInfo.Model));
            Tilemap = new Tilemap(NavFile);
        }

        private void InitSceneObjectData()
        {
            foreach (ScnObjectInfo originalObjectInfo in ScnFile.ObjectInfos)
            {
                ScnObjectInfo objectInfo = originalObjectInfo;

                // Load scene object state from state manager if it exists
                if (!_sceneObjectIdsToNotLoadFromSaveState.Contains(objectInfo.Id) &&
                    _sceneStateManager.TryGetSceneObjectStateOverride(ScnFile.SceneInfo.CityName,
                        ScnFile.SceneInfo.SceneName, objectInfo.Id, out SceneObjectStateOverride state))
                {
                    objectInfo = state.ApplyOverrides(objectInfo);
                }

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

            foreach (ScnNpcInfo playerInfo in NpcInfoFactory.CreateAllPlayerActorNpcInfos())
            {
                if (!Actors.ContainsKey(playerInfo.Id))
                {
                    Actors[playerInfo.Id] = new Actor(_resourceProvider, playerInfo);
                }
            }

            #if PAL3A
            foreach (ScnNpcInfo fengYaSongInfo in NpcInfoFactory.CreateAllFengYaSongNpcInfos())
            {
                if (!Actors.ContainsKey(fengYaSongInfo.Id))
                {
                    Actors[fengYaSongInfo.Id] = new Actor(_resourceProvider, fengYaSongInfo);
                }
            }
            #endif
        }
    }
}