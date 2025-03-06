// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

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

    [ScnSceneObject(SceneObjectType.ElevatorDoor)]
    public sealed class ElevatorDoorObject : SceneObject
    {
        private SceneObjectMeshCollider _meshCollider;

        public ElevatorDoorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);
            // Add collider to block player
            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            return sceneObjectGameEntity;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            PlaySfx("wg005");

            IGameEntity doorEntity = GetGameEntity();
            Vector3 currentPosition = doorEntity.Transform.Position;
            Vector3 toPosition = currentPosition +
                (ObjectInfo.SwitchState == 0 ? Vector3.down : Vector3.up) * (GetMeshBounds().size.y + 0.5f);

            yield return doorEntity.Transform.MoveAsync(toPosition, 2f);

            FlipAndSaveSwitchState();
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