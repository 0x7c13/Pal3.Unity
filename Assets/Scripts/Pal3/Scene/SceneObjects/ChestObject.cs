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

    [ScnSceneObject(ScnSceneObjectType.Chest)]
    public sealed class ChestObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        public ChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override IEnumerator Interact(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            PlaySfx("wg011");
            PlaySfx("wa006");

            for (int i = 0; i < 4; i++)
            {
                if (ObjectInfo.Parameters[i] != 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddItemCommand(ObjectInfo.Parameters[i], 1));
                }
            }

            #if PAL3A
            if (ObjectInfo.Parameters[5] != 0) // money
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddMoneyCommand(ObjectInfo.Parameters[5]));
            }
            #endif

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