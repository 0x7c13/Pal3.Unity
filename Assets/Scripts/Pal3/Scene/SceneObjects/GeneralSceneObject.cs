// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.General)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj47)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj48)]
    public sealed class GeneralSceneObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        public GeneralSceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            if (ObjectInfo.Type == ScnSceneObjectType.General)
            {
                // Don't cast shadow on the map entrance/exit indicator.
                // Those indicators are general objects which have Info.Parameters[0] set to 1
                if (ObjectInfo.Parameters[0] == 1)
                {
                    foreach (MeshRenderer meshRenderer in sceneGameObject.GetComponentsInChildren<MeshRenderer>())
                    {
                        meshRenderer.receiveShadows = false;
                    }
                }

                #if PAL3
                // The general object 15 in M15 2 scene should block player
                if (ObjectInfo is { Id: 15 } &&
                    SceneInfo.Is("m15", "2"))
                {
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                }
                // All general objects (except indicators) in M22 scene should block player
                if (SceneInfo.IsCity("m22") &&
                    ObjectInfo.Parameters[0] == 0)
                {
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                }
                // All general objects (except indicators) in M24 scene should block player
                if (SceneInfo.IsCity("m24") &&
                    ObjectInfo.Parameters[0] == 0)
                {
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                }
                #elif PAL3A
                // Object 15 in scene M12-1 should block player
                if (SceneInfo.Is("m12", "1") && ObjectInfo.Id == 15)
                {
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
                    _meshCollider.Init(new Vector3(2.5f, 0f, 0f));
                }
                #endif
            }

            return sceneGameObject;
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
            }

            base.Deactivate();
        }
    }
}