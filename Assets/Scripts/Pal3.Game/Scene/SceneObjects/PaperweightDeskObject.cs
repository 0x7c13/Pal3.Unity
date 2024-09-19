// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3A

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using System.Collections.Generic;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Engine.Coroutine;
    using Engine.Services;
    using GameSystems.Inventory;

    [ScnSceneObject(SceneObjectType.PaperweightDesk)]
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
            return IsActivated &&
                   distance < MAX_INTERACTION_DISTANCE &&
                   ObjectInfo.Times > 0;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!_deskIdToRequiredItemIdMap.TryGetValue(ObjectInfo.Id, out int requiredItemId)) yield break;

            if (_inventoryManager.HaveItem(requiredItemId))
            {
                if (!IsInteractableBasedOnTimesCount()) yield break;

                Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                Pal3.Instance.Execute(new PlayerActorLookAtSceneObjectCommand(ObjectInfo.Id));
                Pal3.Instance.Execute(new ActorPerformActionCommand(ActorConstants.PlayerActorVirtualID,
                        ActorConstants.ActionToNameMap[ActorActionType.Check], 1));

                yield return CoroutineYieldInstruction.WaitForSeconds(0.8f); // Wait for actor animation to finish

                yield return ActivateOrInteractWithObjectIfAnyAsync(ctx, ObjectInfo.LinkedObjectId);
            }
            else
            {
                Pal3.Instance.Execute(new UIDisplayNoteCommand("还没有找到合适的东西"));
            }
        }
    }
}

#endif