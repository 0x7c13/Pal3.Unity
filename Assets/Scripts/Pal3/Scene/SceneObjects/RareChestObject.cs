// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using MetaData;

    [ScnSceneObject(ScnSceneObjectType.RareChest)]
    public sealed class RareChestObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        public RareChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                    ActorConstants.ActionNames[ActorActionType.Check], 1));

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }

            PlaySfx("wa006");

            for (int i = 0; i < 6; i++)
            {
                if (ObjectInfo.Parameters[i] != 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddItemCommand(ObjectInfo.Parameters[i], 1));
                }
            }

            ChangeAndSaveActivationState(false);
        }
    }
}