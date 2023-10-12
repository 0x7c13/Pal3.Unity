// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Rendering.Renderer;

    using Color = Core.Primitives.Color;

    [ScnSceneObject(SceneObjectType.SectorBridge)]
    public sealed class SectorBridgeObject : SceneObject
    {
        private StandingPlatformController _standingPlatformController;

        public SectorBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            if (ObjectInfo.SwitchState == 1)
            {
                if (ModelType == SceneObjectModelType.CvdModel)
                {
                    CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                    cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
                }

                _standingPlatformController = sceneObjectGameEntity.AddComponent<StandingPlatformController>();
                _standingPlatformController.Init(GetMeshBounds(), ObjectInfo.LayerIndex);
            }

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (ObjectInfo.SwitchState == 1) yield break;

            FlipAndSaveSwitchState();

            PlaySfxIfAny();

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }

            _standingPlatformController = GetGameEntity().AddComponent<StandingPlatformController>();
            _standingPlatformController.Init(GetMeshBounds(), ObjectInfo.LayerIndex);
        }

        public override void Deactivate()
        {
            if (_standingPlatformController != null)
            {
                _standingPlatformController.Destroy();
                _standingPlatformController = null;
            }

            base.Deactivate();
        }
    }
}

#endif