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
    using Engine.Animation;
    using Engine.Core.Abstraction;

    using Color = Core.Primitives.Color;
    using Quaternion = UnityEngine.Quaternion;

    [ScnSceneObject(SceneObjectType.Teapot)]
    public sealed class TeapotObject : SceneObject
    {
        public TeapotObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            if (ObjectInfo.SwitchState == 1)
            {
                sceneObjectGameEntity.Transform.Rotation *= Quaternion.Euler(0, 0, -ObjectInfo.Parameters[2]);
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

            ITransform objectTransform = GetGameEntity().Transform;
            Quaternion rotation = objectTransform.Rotation;
            Quaternion targetRotation = rotation * Quaternion.Euler(0, 0, -ObjectInfo.Parameters[2]);

            yield return objectTransform.RotateAsync(targetRotation, 1f, AnimationCurveType.Sine);

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
        }
    }
}

#endif