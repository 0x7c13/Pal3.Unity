﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [ScnSceneObject(ScnSceneObjectType.SwordBridge)]
    public sealed class SwordBridgeObject : SceneObject
    {
        private const float EXTEND_LENGTH = 6f;

        private StandingPlatformController _platformController;

        public SwordBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Bounds bounds = GetMeshBounds();
            var heightOffset = 0f;

            #if PAL3
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
            #endif

            _platformController = sceneGameObject.AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds, ObjectInfo.LayerIndex, heightOffset);

            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfx("wg005");

            GameObject bridgeObject = GetGameObject();
            bridgeObject.transform.Translate(new Vector3(0f, 0f, EXTEND_LENGTH));

            SaveCurrentPosition();

            yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted()
        {
            return false; // Bridge can extend itself in gameplay mode
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