// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Common;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Data;
    using Engine.Animation;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using UnityEngine;

    using Color = Core.Primitives.Color;

    [ScnSceneObject(SceneObjectType.Trap)]
    public sealed class TrapObject : SceneObject
    {
        private TilemapTriggerController _triggerController;

        public TrapObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            // Make trap trigger rect a bit smaller for better game experience
            GameBoxRect triggerRect = new()
            {
                Left = ObjectInfo.TileMapTriggerRect.Left + 1,
                Right = ObjectInfo.TileMapTriggerRect.Right - 1,
                Top = ObjectInfo.TileMapTriggerRect.Top + 1,
                Bottom = ObjectInfo.TileMapTriggerRect.Bottom - 1
            };

            _triggerController = sceneObjectGameEntity.AddComponent<TilemapTriggerController>();
            _triggerController.Init(triggerRect, ObjectInfo.LayerIndex);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, (int x, int y) tilePosition)
        {
            RequestForInteraction();
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            #if PAL3
            PlaySfx("wg007");
            #elif PAL3A
            PlaySfxIfAny();
            #endif

            // Let player actor fall down
            if (ctx.PlayerActorGameEntity.GetComponent<Rigidbody>() is { } rigidbody)
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
                ITransform transform = GetGameEntity().Transform;
                float yOffset = ((float)ObjectInfo.Parameters[2]).ToUnityDistance();
                Vector3 finalPosition = transform.Position + new Vector3(0f, -yOffset, 0f);
                yield return transform.MoveAsync(finalPosition, 0.5f);
                #endif
            }

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
        }

        public override void Deactivate()
        {
            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.Destroy();
                _triggerController = null;
            }

            base.Deactivate();
        }
    }
}