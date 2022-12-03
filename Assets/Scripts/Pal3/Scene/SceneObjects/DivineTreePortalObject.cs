// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.DivineTreePortal)]
    public sealed class DivineTreePortalObject : SceneObject
    {
        private StandingPlatformController _platformController;

        public DivineTreePortalObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = new Bounds
            {
                center = new Vector3(0f, -1f, 0f),
                size = new Vector3(9f, 2f, 9f),
            };

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex);

            return sceneGameObject;
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                Object.Destroy(_platformController);
            }

            base.Deactivate();
        }
    }
}