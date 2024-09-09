﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

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
    using Engine.Services;
    using State;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

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

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

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

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            // Add a standing platform controller to the flower if it is in the open state
            if (isFlowerInOpenState)
            {
                Bounds bounds = new()
                {
                    center = new Vector3(0f, -1f, 0f),
                    size = new Vector3(17f, 2f, 17f),
                };

                _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
                _platformController.Init(bounds, ObjectInfo.LayerIndex);
            }

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            yield break;
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