// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Common;
    using Core.DataReader.Scn;
    using Core.Services;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.EyeBall)]
    public class EyeBallObject : SceneObject,
        ICommandExecutor<PlayerActorTilePositionUpdatedNotification>
    {
        private readonly Tilemap _tilemap;

        public EyeBallObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
            _tilemap = ServiceLocator.Instance.Get<SceneManager>().GetCurrentScene().GetTilemap();
        }

        public void Execute(PlayerActorTilePositionUpdatedNotification command)
        {
            GameObject eyeBallGameObject = GetGameObject();
            Vector3 actorPosition = _tilemap.GetWorldPosition(command.Position, command.LayerIndex);
            actorPosition.y += 3f; // 3f is about the height of the player actor
            eyeBallGameObject.transform.LookAt(actorPosition);
        }

        public override void Deactivate()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            base.Deactivate();
        }
    }
}