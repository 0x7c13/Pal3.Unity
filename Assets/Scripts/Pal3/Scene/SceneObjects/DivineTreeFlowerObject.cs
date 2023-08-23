// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Scene.SceneObjects
{
    using System;
    using Common;
    using Core.Contracts;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Core.Services;
    using Data;
    using State;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.DivineTreeFlower)]
    public sealed class DivineTreeFlowerObject : SceneObject
    {
        private StandingPlatformController _platformController;

        private readonly SceneStateManager _sceneStateManager;

        public DivineTreeFlowerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneStateManager = ServiceLocator.Instance.Get<SceneStateManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();

            bool isFlowerInOpenState;

            // The flower object state is controlled by the master flower located
            // in the scene m16 4
            if (_sceneStateManager.TryGetSceneObjectStateOverride(
                    "m16", "4", 22, out SceneObjectStateOverride state) &&
                state.SwitchState == 1)
            {
                isFlowerInOpenState = false;
                ModelFileVirtualPath = ModelFileVirtualPath.Replace("1.pol", "2.pol",
                    StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isFlowerInOpenState = true;
                ModelFileVirtualPath = ModelFileVirtualPath.Replace("2.pol", "1.pol",
                    StringComparison.OrdinalIgnoreCase);
            }

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Add a standing platform controller to the flower if it is in the open state
            if (isFlowerInOpenState)
            {
                var bounds = new Bounds
                {
                    center = new Vector3(0f, -1f, 0f),
                    size = new Vector3(17f, 2f, 17f),
                };

                _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
                _platformController.Init(bounds, ObjectInfo.LayerIndex);
            }

            return sceneGameObject;
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                _platformController.Destroy();
                _platformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif