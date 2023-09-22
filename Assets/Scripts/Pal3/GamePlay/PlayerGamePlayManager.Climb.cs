// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GamePlay
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
    using Engine.Extensions;
    using Engine.Renderer;
    using Scene;
    using Scene.SceneObjects;
    using Script.Waiter;
    using UnityEngine;

    public partial class PlayerGamePlayManager :
        ICommandExecutor<ActorPerformClimbActionCommand>
    {
        public void Execute(ActorPerformClimbActionCommand command)
        {
            Scene scene = _sceneManager.GetCurrentScene();
            SceneObject climbableSceneObject = scene.GetSceneObject(command.ObjectId);
            GameObject climbableObject = climbableSceneObject.GetGameObject();
            if (climbableObject == null)
            {
                Debug.LogError($"[{nameof(PlayerGamePlayManager)}] Scene object not found or not activated yet: {command.ObjectId}.");
                return;
            }

            Vector3 climbableObjectPosition = climbableObject.transform.position;
            Vector3 climbableObjectFacing = new GameBoxVector3(0f, climbableSceneObject.ObjectInfo.GameBoxYRotation, 0f)
                .ToUnityQuaternion() * Vector3.forward;

            Vector3 lowerPosition = climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            Vector3 lowerStandingPosition = climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;
            Vector3 upperPosition = -climbableObjectFacing.normalized * 0.5f + climbableObjectPosition;
            Vector3 upperStandingPosition = -climbableObjectFacing.normalized * 1.5f + climbableObjectPosition;

            var currentPlayerLayer = _playerActorMovementController.GetCurrentLayerIndex();

            var climbUp = (command.ClimbUp == 1);
            var climbableHeight = climbableObject.GetComponentInChildren<StaticMeshRenderer>()
                                      .GetRendererBounds().max.y / 2f; // Half is enough for the animation

            Vector3 playerCurrentPosition = _playerActorGameObject.transform.position;
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

            var waiter = new WaitUntilCanceled();
            CommandDispatcher<ICommand>.Instance.Dispatch(new ScriptRunnerAddWaiterRequest(waiter));

            var climbAnimationOnly = command.ClimbUp != -1;
            Pal3.Instance.StartCoroutine(PlayerActorMoveToClimbableObjectAndClimbAsync(climbableObject,
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
            GameObject climbableObject,
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

            Vector3 newPosition = new Vector3(lowerPosition.x, _playerActorGameObject.transform.position.y, lowerPosition.z);
            var objectRotationY = climbableObject.transform.rotation.eulerAngles.y;
            Quaternion newRotation = Quaternion.Euler(0f, objectRotationY + 180f, 0f);

            _playerActorGameObject.transform.SetPositionAndRotation(newPosition, newRotation);

            if (climbUp)
            {
                var currentHeight = 0f;
                while (currentHeight < climbableHeight)
                {
                    var delta = Time.deltaTime * ActorConstants.PlayerActorClimbSpeed;
                    currentHeight += delta;
                    _playerActorGameObject.transform.position += new Vector3(0f, delta, 0f);
                    yield return null;
                }

                if (!climbOnly)
                {
                    _playerActorMovementController.SetNavLayer(upperLayer);
                    yield return _playerActorMovementController.MoveDirectlyToAsync(upperStandingPosition, 0, true);
                    _playerActorGameObject.transform.position = upperStandingPosition;
                }
            }
            else
            {
                var currentHeight = climbableHeight;
                while (currentHeight > 0f)
                {
                    var delta = Time.deltaTime * ActorConstants.PlayerActorClimbSpeed;
                    currentHeight -= delta;
                    _playerActorGameObject.transform.position -= new Vector3(0f, delta, 0f);
                    yield return null;
                }

                if (!climbOnly)
                {
                    _playerActorMovementController.SetNavLayer(lowerLayer);
                    yield return _playerActorMovementController.MoveDirectlyToAsync(lowerStandingPosition, 0, true);
                    _playerActorGameObject.transform.position = lowerStandingPosition;
                }
            }

            onFinished?.Invoke();
        }
    }
}