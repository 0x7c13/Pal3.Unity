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

        private CvdModelRenderer _cvdModelRenderer;
        private bool _isOpened;

        public RareChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _cvdModelRenderer = sceneGameObject.GetComponent<CvdModelRenderer>();
            return sceneGameObject;
        }
        
        public override bool IsInteractable(InteractionContext ctx)
        {
            return !_isOpened && ctx.DistanceToActor < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact()
        {
            if (_isOpened) return;
            _isOpened = true;

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa006", 1));
            
            for (int i = 0; i < 6; i++)
            {
                if (Info.Parameters[i] != 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new InventoryAddItemCommand(Info.Parameters[i], 1));
                }
            }
            
            if (_cvdModelRenderer != null)
            {
                _cvdModelRenderer.PlayOneTimeAnimation(() =>
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new SceneActivateObjectCommand(Info.Id, 0));
                    CommandDispatcher<ICommand>.Instance.Dispatch(new SceneChangeObjectActivationStateCommand(Info.Id, 0));
                });
            }
            else
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new SceneActivateObjectCommand(Info.Id, 0));
                CommandDispatcher<ICommand>.Instance.Dispatch(new SceneChangeObjectActivationStateCommand(Info.Id, 0));
            }
        }

        public override void Deactivate()
        {
            _isOpened = false;
            _cvdModelRenderer = null;
            base.Deactivate();
        }
    }
}