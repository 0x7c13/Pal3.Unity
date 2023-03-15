// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using System.Collections.Generic;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Core.Services;
    using MetaData;
    using Player;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.PaperweightDesk)]
    public sealed class PaperweightDeskObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 2f;

        private readonly Dictionary<int, int> _deskIdToRequiredItemIdMap = new()
        {
            [20] = 6657,
            [21] = 6659,
            [22] = 6658,
        };

        private readonly InventoryManager _inventoryManager;

        public PaperweightDeskObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _inventoryManager = ServiceLocator.Instance.Get<InventoryManager>();
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated &&
                   distance < MAX_INTERACTION_DISTANCE &&
                   ObjectInfo.Times > 0;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!_deskIdToRequiredItemIdMap.ContainsKey(ObjectInfo.Id)) yield break;

            if (_inventoryManager.HaveItem(_deskIdToRequiredItemIdMap[ObjectInfo.Id]))
            {
                if (!IsInteractableBasedOnTimesCount()) yield break;

                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionNames[ActorActionType.Check], 1));

                yield return new WaitForSeconds(0.8f); // Wait for actor animation to finish

                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
            }
            else
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand("还没有找到合适的东西"));
            }
        }
    }
}

#endif