// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;

    using Color = Core.Primitives.Color;

    [ScnSceneObject(SceneObjectType.RareChest)]
    public sealed class RareChestObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 3f;

        private SceneObjectMeshCollider _meshCollider;

        public RareChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);
            _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
            return sceneObjectGameEntity;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            Pal3.Instance.Execute(new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
            Pal3.Instance.Execute(new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                    ActorConstants.ActionToNameMap[ActorActionType.Check], 1));

            PlaySfx("wg011");

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }

            PlaySfx("wa006");

            for (int i = 0; i < 6; i++)
            {
                if (ObjectInfo.Parameters[i] != 0)
                {
                    Pal3.Instance.Execute(new InventoryAddItemCommand(ObjectInfo.Parameters[i], 1));
                }
            }

            ChangeAndSaveActivationState(false);
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