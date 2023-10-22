// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;

    [ScnSceneObject(SceneObjectType.Collectable)]
    public sealed class CollectableObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 3.8f;

        public CollectableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => false;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            if (ObjectInfo.Parameters[0] != 0) // Game item
            {
                Pal3.Instance.Execute(new InventoryAddItemCommand(ObjectInfo.Parameters[0], 1));
            }
            else if (ObjectInfo.Parameters[1] != 0 && ObjectInfo.Parameters[1] != 1)
            {
                Pal3.Instance.Execute(new InventoryAddMoneyCommand(ObjectInfo.Parameters[1]));
            }
            else if (ObjectInfo.Parameters[2] != 0)
            {
                Pal3.Instance.Execute(new InventoryAddMoneyCommand(ObjectInfo.Parameters[2]));
            }

            PlaySfx("wa006");
            ChangeAndSaveActivationState(false);
        }
    }
}