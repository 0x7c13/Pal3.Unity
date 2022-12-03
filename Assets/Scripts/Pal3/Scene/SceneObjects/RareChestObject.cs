// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;

    [ScnSceneObject(ScnSceneObjectType.RareChest)]
    public sealed class RareChestObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        public RareChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsInteractable(InteractionContext ctx)
        {
            return Activated && ctx.DistanceToActor < MAX_INTERACTION_DISTANCE;
        }

        public override IEnumerator Interact(bool triggerredByPlayer)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa006", 1));

            for (int i = 0; i < 6; i++)
            {
                if (ObjectInfo.Parameters[i] != 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddItemCommand(ObjectInfo.Parameters[i], 1));
                }
            }

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                GetCvdModelRenderer().StartOneTimeAnimation(true, () =>
                {
                    ChangeAndSaveActivationState(false);
                });
            }
            else
            {
                ChangeAndSaveActivationState(false);
            }
        }
    }
}