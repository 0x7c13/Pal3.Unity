﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Trap)]
    public sealed class TrapObject : SceneObject
    {
        private TilemapTriggerController _triggerController;

        public TrapObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Make trap trigger rect a bit smaller for better game experience
            GameBoxRect triggerRect = new()
            {
                Left = ObjectInfo.TileMapTriggerRect.Left + 1,
                Right = ObjectInfo.TileMapTriggerRect.Right - 1,
                Top = ObjectInfo.TileMapTriggerRect.Top + 1,
                Bottom = ObjectInfo.TileMapTriggerRect.Bottom - 1
            };

            _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
            _triggerController.Init(triggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
        {
            RequestForInteraction();
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            #if PAL3
            PlaySfx("wg007");
            #elif PAL3A
            PlaySfxIfAny();
            #endif

            // Let player actor fall down
            if (ctx.PlayerActorGameObject.GetComponent<Rigidbody>() is { } rigidbody)
            {
                rigidbody.useGravity = true;
                rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                rigidbody.isKinematic = false;
            }

            if (ModelType == SceneObjectModelType.CvdModel)
            {
                yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
            }
            else
            {
                #if PAL3A
                Transform transform = GetGameObject().transform;
                float yOffset = GameBoxInterpreter.ToUnityDistance(ObjectInfo.Parameters[2]);
                Vector3 finalPosition = transform.position + new Vector3(0f, -yOffset, 0f);
                yield return AnimationHelper.MoveTransformAsync(transform, finalPosition, 0.5f);
                #endif
            }

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Object.Destroy(_triggerController);
            }

            base.Deactivate();
        }
    }
}