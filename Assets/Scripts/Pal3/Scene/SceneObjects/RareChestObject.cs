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

    [ScnSceneObject(ScnSceneObjectType.RareChest)]
    public class RareChestObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private RareChestObjectController _rareChestObjectController;
        private bool _isOpened;

        public RareChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _rareChestObjectController = sceneGameObject.AddComponent<RareChestObjectController>();
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

    internal class RareChestObjectController : MonoBehaviour
    {
        private RareChestObject _rareChestObject;
        
        public void Init(RareChestObject rareChestObject)
        {
            _rareChestObject = rareChestObject;
        }
        
        // TODO: implement rare chest interaction logic
        public void Interact()
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa006", 1));
            
            for (int i = 0; i < 6; i++)
            {
                if (_rareChestObject.Info.Parameters[i] != 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddItemCommand(_rareChestObject.Info.Parameters[i], 1));
                }
            }

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
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneActivateObjectCommand(_rareChestObject.Info.Id, 0));
            CommandDispatcher<ICommand>.Instance.Dispatch(new SceneChangeObjectActivationStateCommand(_rareChestObject.Info.Id, 0));
        }
    }
}