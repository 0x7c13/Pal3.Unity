// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.SuspensionBridge)]
    public class SuspensionBridgeObject : SceneObject
    {
        private StandingPlatformController _platformController;
        
        public SuspensionBridgeObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override void Interact()
        {
            if (Info.Times != 0xFF)
            {
                if (Info.Times <= 0) return;
                else Info.Times--;
            }

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                if (!string.IsNullOrEmpty(Info.SfxName))
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand(Info.SfxName, 1));
                }
                
                GetCvdModelRenderer().StartOneTimeAnimation(EnableCollider);
            }
        }

        private void EnableCollider()
        {
            var bounds = new Bounds
            {
                center = new Vector3(0f, -0.2f, 1.4f),
                size = new Vector3(4.5f, 0.7f, 6f),
            };

            _platformController = GetGameObject().AddComponent<StandingPlatformController>();
            _platformController.SetBounds(bounds);
        }

        public override void Deactivate()
        {
            if (_platformController != null)
            {
                Object.Destroy(_platformController);
            }

            base.Deactivate();
        }
    }
}