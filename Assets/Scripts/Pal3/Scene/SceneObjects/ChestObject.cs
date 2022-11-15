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
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Chest)]
    public class ChestObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private ChestObjectController _rareChestObjectController;
        private bool _isOpened;

        public ChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _rareChestObjectController = sceneGameObject.AddComponent<ChestObjectController>();
            _rareChestObjectController.Init(this);
            return sceneGameObject;
        }
        
        public override bool IsInteractable(float distance, Vector2Int actorTilePosition)
        {
            return !_isOpened && distance < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact()
        {
            if (_isOpened) return;
            _isOpened = true;
            _rareChestObjectController.Interact();
        }
    }

    internal class ChestObjectController : MonoBehaviour
    {
        private ChestObject _chestObject;
        
        public void Init(ChestObject rareChestObject)
        {
            _chestObject = rareChestObject;
        }
        
        public void Interact()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa006", 1));
            
            for (int i = 0; i < 4; i++)
            {
                if (_chestObject.Info.Parameters[i] != 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddItemCommand(_chestObject.Info.Parameters[i], 1));
                }
            }
            
            #if PAL3A
            if (_chestObject.Info.Parameters[5] != 0) // money
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddMoneyCommand(_chestObject.Info.Parameters[5]));
            }
            #endif

            var animationDuration = 0f;
            
            if (GetComponent<CvdModelRenderer>() is { } cvdModelRenderer)
            {
                animationDuration = cvdModelRenderer.GetAnimationDuration();
                cvdModelRenderer.PlayAnimation(timeScale: 1, loopCount: 1);
            }
            
            Invoke(nameof(OnAnimationFinished), animationDuration);
        }

        public void OnAnimationFinished()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneActivateObjectCommand(_chestObject.Info.Id, 0));
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneChangeObjectActivationStateCommand(_chestObject.Info.Id, 0));
        }
    }
}