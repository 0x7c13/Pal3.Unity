// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Chest)]
    public sealed class ChestObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 4f;

        private SceneObjectMeshCollider _meshCollider;

        public ChestObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted()
        {
            return false;
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
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
                GetCvdModelRenderer().StartOneTimeAnimation(true, 1f, () =>
                {
                    ExecuteScriptIfAny();
                    ChangeAndSaveActivationState(false);
                });
            }
            else
            {
                ExecuteScriptIfAny();
                ChangeAndSaveActivationState(false);
            }
        }

        public override void Deactivate()
        {
            if (_meshCollider != null)
            {
                Object.Destroy(_meshCollider);
            }

            base.Deactivate();
        }
    }
}