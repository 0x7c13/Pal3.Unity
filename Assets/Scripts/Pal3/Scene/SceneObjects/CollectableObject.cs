// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Collectable)]
    public class CollectableObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private bool _isCollected;
        
        public CollectableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsInteractable(float distance, Vector2Int actorTilePosition)
        {
            return !_isCollected && distance < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact()
        {
            _isCollected = true;
            
            if (Info.Parameters[0] != 0) // Game item
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddItemCommand(Info.Parameters[0], 1));
            }
            else if (Info.Parameters[1] != 0 && Info.Parameters[1] != 1)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddMoneyCommand(Info.Parameters[1]));
            }
            else if (Info.Parameters[2] != 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddMoneyCommand(Info.Parameters[2]));
            }

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa006", 1));
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneActivateObjectCommand(Info.Id, 0));
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneChangeObjectActivationStateCommand(Info.Id, 0));
        }

        public override void Deactivate()
        {
            _isCollected = false;
            base.Deactivate();
        }
    }
}