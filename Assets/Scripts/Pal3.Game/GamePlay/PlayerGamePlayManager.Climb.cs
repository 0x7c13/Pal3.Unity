// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.GamePlay
{
    using System;
    using System.Collections;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.Primitives;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Logging;
    using Engine.Renderer;
    using Engine.Services;
    using Scene;
    using Scene.SceneObjects;
    using Script.Waiter;

    using Quaternion = UnityEngine.Quaternion;
    using Vector3 = UnityEngine.Vector3;

    public partial class PlayerGamePlayManager :
        ICommandExecutor<ActorPerformClimbActionCommand>
    {
        public void Execute(ActorPerformClimbActionCommand command)
        {
            GameScene scene = _sceneManager.GetCurrentScene();
            SceneObject climbableSceneObject = scene.GetSceneObject(command.ObjectId);
            IGameEntity climbableEntity = climbableSceneObject.GetGameEntity();
            if (climbableEntity == null)
            {
                EngineLogger.LogError($"Scene object not found or not activated yet: {command.ObjectId}");
                return;
            }

            Vector3 climbableObjectPosition = climbableEntity.Transform.Position;
            Vector3 climbableObjectFacing = new GameBoxVector3(0f, climbableSceneObject.ObjectInfo.GameBoxYRotation, 0f)
                .ToUnityQuaternion() * Vector3.forward;

            Vector3 lowerPosition = climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            Vector3 lowerStandingPosition = climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;
            Vector3 upperPosition = -climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            Vector3 upperStandingPosition = -climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;

            int currentPlayerLayer = _playerActorMovementController.GetCurrentLayerIndex();

            bool climbUp = (command.ClimbUp == 1);
            float climbableHeight = climbableEntity.GetComponentInChildren<StaticMeshRenderer>()
                                      .GetRendererBounds().max.y / 2f; // Half is enough for the animation

            Vector3 playerCurrentPosition = _playerActorGameEntity.Transform.Position;
            if (command.ClimbUp == 1)
            {
                lowerPosition.y = playerCurrentPosition.y;
                lowerStandingPosition.y = playerCurrentPosition.y;
                upperPosition.y = playerCurrentPosition.y + climbableHeight;
                upperStandingPosition.y = playerCurrentPosition.y + climbableHeight;
            }
            else
            {
                lowerPosition.y = playerCurrentPosition.y - climbableHeight;
                lowerStandingPosition.y = playerCurrentPosition.y - climbableHeight;
                upperPosition.y = playerCurrentPosition.y;
                upperStandingPosition.y = playerCurrentPosition.y;
            }

            WaitUntilCanceled waiter = new();
            Pal3.Instance.Execute(new ScriptRunnerAddWaiterRequest(waiter));

            bool climbAnimationOnly = command.ClimbUp != -1;
            Pal3.Instance.StartCoroutine(PlayerActorMoveToClimbableObjectAndClimbAsync(climbableEntity,
                climbUp,
                climbAnimationOnly,
                climbableHeight,
                lowerPosition,
                lowerStandingPosition,
                upperPosition,
                upperStandingPosition,
                currentPlayerLayer,
                currentPlayerLayer,
                () => waiter.CancelWait()));
        }

        public IEnumerator PlayerActorMoveToClimbableObjectAndClimbAsync(
            IGameEntity climbableGameEntity,
            bool climbUp,
            bool climbOnly,
            float climbableHeight,
            Vector3 lowerPosition,
            Vector3 lowerStandingPosition,
            Vector3 upperPosition,
            Vector3 upperStandingPosition,
            int lowerLayer,
            int upperLayer,
            Action onFinished = null)
        {
            yield return _playerActorMovementController
                .MoveDirectlyToAsync(climbUp ? lowerPosition : upperPosition, 0, true);

            _playerActorActionController.PerformAction(climbUp ? ActorActionType.Climb : ActorActionType.ClimbDown);

            Vector3 newPosition = new Vector3(lowerPosition.x, _playerActorGameEntity.Transform.Position.y, lowerPosition.z);
            float objectRotationY = climbableGameEntity.Transform.EulerAngles.y;
            Quaternion newRotation = Quaternion.Euler(0f, objectRotationY + 180f, 0f);

            _playerActorGameEntity.Transform.SetPositionAndRotation(newPosition, newRotation);

            if (climbUp)
            {
                float currentHeight = 0f;
                while (currentHeight < climbableHeight)
                {
                    float deltaHeight = GameTimeProvider.Instance.DeltaTime * ActorConstants.PlayerActorClimbSpeed;
                    currentHeight += deltaHeight;
                    _playerActorGameEntity.Transform.Position += new Vector3(0f, deltaHeight, 0f);
                    yield return null;
                }

                if (!climbOnly)
                {
                    _playerActorMovementController.SetNavLayer(upperLayer);
                    yield return _playerActorMovementController
                        .MoveDirectlyToAsync(upperStandingPosition, MovementMode.Walk, ignoreObstacle: true);
                    _playerActorGameEntity.Transform.Position = upperStandingPosition;
                }
            }
            else
            {
                float currentHeight = climbableHeight;
                while (currentHeight > 0f)
                {
                    float deltaHeight = GameTimeProvider.Instance.DeltaTime * ActorConstants.PlayerActorClimbSpeed;
                    currentHeight -= deltaHeight;
                    _playerActorGameEntity.Transform.Position -= new Vector3(0f, deltaHeight, 0f);
                    yield return null;
                }

                if (!climbOnly)
                {
                    _playerActorMovementController.SetNavLayer(lowerLayer);
                    yield return _playerActorMovementController
                        .MoveDirectlyToAsync(lowerStandingPosition, MovementMode.Walk, ignoreObstacle: true);
                    _playerActorGameEntity.Transform.Position = lowerStandingPosition;
                }
            }

            onFinished?.Invoke();
        }
    }
}