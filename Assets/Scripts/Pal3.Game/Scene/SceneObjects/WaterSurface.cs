// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Engine.Abstraction;
    using Engine.Animation;

    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.WaterSurface)]
    public sealed class WaterSurfaceObject : SceneObject
    {
        private const float WATER_ANIMATION_DURATION = 3f;
        private const float WATER_DESCEND_HEIGHT = 4f;

        public WaterSurfaceObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            PlaySfxIfAny();

            ITransform transform = GetGameEntity().Transform;
            Vector3 finalPosition = transform.Position;
            finalPosition += new Vector3(0f, -WATER_DESCEND_HEIGHT, 0f);

            yield return transform.MoveAsync(finalPosition,
                WATER_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            ChangeAndSaveActivationState(false);
        }
    }
}

#endif