// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
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

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.SwordBridge)]
    public sealed class SwordBridgeObject : SceneObject
    {
        private const float EXTEND_LENGTH = 6f;

        private StandingPlatformController _platformController;

        public SwordBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();
            bounds.size += new Vector3(0.1f, 0.1f, 0.1f);
            var heightOffset = 0f;

            if (SceneInfo.IsCity("m11") &&
                ObjectInfo.Name.Equals("_t.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, -0.4f, -7.5f),
                    size = new Vector3(6f, 1f, 14.5f),
                };
            }
            else if (SceneInfo.IsCity("m15") &&
                     ObjectInfo.Name.Equals("_g.pol", StringComparison.OrdinalIgnoreCase))
            {
                bounds = new Bounds
                {
                    center = new Vector3(0f, 0.5f, -0f),
                    size = new Vector3(4.5f, 2.8f, 19f),
                };
                heightOffset = -1.7f;
            }

            _platformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
            _platformController.Init(bounds, ObjectInfo.LayerIndex, heightOffset);

            return sceneObjectGameEntity;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfx("wg005");

            IGameEntity bridgeEntity = GetGameEntity();
            bridgeEntity.Transform.Translate(new Vector3(0f, 0f, EXTEND_LENGTH));

            SaveCurrentPosition();

            yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted()
        {
            return false; // Bridge can extend itself in gameplay mode
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