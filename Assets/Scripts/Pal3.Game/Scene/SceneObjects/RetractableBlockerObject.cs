// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
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
    using Engine.Extensions;

    using Color = Core.Primitives.Color;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.RetractableBlocker)]
    public sealed class RetractableBlockerObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        public RetractableBlockerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            // Add mesh collider to block player
            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();

            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfxIfAny();

            IGameEntity blockerEntity = GetGameEntity();
            Vector3 currentPosition = blockerEntity.Transform.Position;
            float zOffset = ((float)ObjectInfo.Parameters[2]).ToUnityDistance();
            Vector3 toPosition = currentPosition + blockerEntity.Transform.Forward * -zOffset;

            yield return blockerEntity.Transform.MoveAsync(toPosition, 1.5f);

            SaveCurrentPosition();
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

#endif