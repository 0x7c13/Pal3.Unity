﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Renderer;

    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.General)]
    public sealed class GeneralSceneObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        public GeneralSceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            // Don't cast shadow on the map entrance/exit indicator.
            // Those indicators are general objects which have Info.Parameters[0] set to 1
            if (ObjectInfo.Parameters[0] == 1)
            {
                foreach (StaticMeshRenderer meshRenderer in
                         sceneObjectGameEntity.GetComponentsInChildren<StaticMeshRenderer>())
                {
                    meshRenderer.ReceiveShadows = false;
                }
            }

            #if PAL3
            // The general object 18 in M10-5 scene should block player
            if (ObjectInfo is { Id: 18 } &&
                SceneInfo.Is("m10", "5"))
            {
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            }
            // The general object 15 in M15-2 scene should block player
            if (ObjectInfo is { Id: 15 } &&
                SceneInfo.Is("m15", "2"))
            {
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            }
            // All general objects (except indicators or object 45 in M22-3)
            // in M22 scene should block player
            if (SceneInfo.IsCity("m22") &&
                ObjectInfo.Parameters[0] == 0 &&
                !(SceneInfo.IsScene("3") && ObjectInfo is { Id: 45 }))
            {
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            }
            // All general objects (except indicators) in M24 scene should block player
            if (SceneInfo.IsCity("m24") &&
                ObjectInfo.Parameters[0] == 0)
            {
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            }
            #elif PAL3A
            // Object 46 in scene M03-1 should block player
            if (SceneInfo.Is("m03", "1") && ObjectInfo.Id == 46)
            {
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            }
            // Object 15 in scene M12-1 should block player
            if (SceneInfo.Is("m12", "1") && ObjectInfo.Id == 15)
            {
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                _meshCollider.Init(new Vector3(2.5f, 0f, 0f));
            }
            // Object 11 in scene M18-4 should block player
            if (SceneInfo.Is("m18", "4") && ObjectInfo.Id == 11)
            {
                _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                _meshCollider.Init(new Vector3(1f, 0f, 1f));
            }
            #endif

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => false;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            yield break;
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                _meshCollider.Destroy();
                _meshCollider = null;
            }

            base.Deactivate();
        }
    }
}