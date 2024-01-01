// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Common;
    using Core.Command;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Services;

    using Color = Core.Primitives.Color;
    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.EyeBall)]
    public sealed class EyeBallObject : SceneObject,
        ICommandExecutor<PlayerActorTilePositionUpdatedNotification>
    {
        private readonly Tilemap _tilemap;

        public EyeBallObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
            return sceneObjectGameEntity;
        }

        public void Execute(PlayerActorTilePositionUpdatedNotification command)
        {
            IGameEntity eyeBallGameEntity = GetGameEntity();
            Vector3 actorPosition = _tilemap.GetWorldPosition(
                new Vector2Int(command.TileXPosition, command.TileYPosition), command.LayerIndex);
            actorPosition.y += 3f; // 3f is about the height of the player actor
            eyeBallGameEntity.Transform.LookAt(actorPosition);
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            yield break;
        }

        public override void Deactivate()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            base.Deactivate();
        }
    }
}

#endif