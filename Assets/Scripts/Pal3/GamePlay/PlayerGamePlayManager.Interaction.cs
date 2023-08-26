// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GamePlay
{
    using System;
    using System.Collections;
    using Actor;
    using Actor.Controllers;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.Contracts;
    using MetaData;
    using Scene;
    using Scene.SceneObjects;
    using Script;
    using Script.Waiter;
    using State;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public partial class PlayerGamePlayManager :
        ICommandExecutor<PlayerInteractionRequest>,
        ICommandExecutor<PlayerInteractWithObjectCommand>
    {
        public void Execute(PlayerInteractWithObjectCommand command)
        {
            Scene currentScene = _sceneManager.GetCurrentScene();
            if (currentScene.GetAllActivatedSceneObjects().Contains(command.SceneObjectId))
            {
                Pal3.Instance.StartCoroutine(InteractWithSceneObjectAsync(
                    currentScene.GetSceneObject(command.SceneObjectId), startedByPlayer: false));
            }
            else
            {
                Debug.LogError($"[{nameof(PlayerGamePlayManager)}] Scene object not found" +
                               $" or not activated yet: {command.SceneObjectId}.");
            }
        }

        public void Execute(PlayerInteractionRequest _)
        {
            if (IsPlayerActorInsideJumpableArea())
            {
                Pal3.Instance.StartCoroutine(JumpAsync());
            }
            else
            {
                InteractWithFacingInteractable();
            }
        }

        private void InteractionPerformed(InputAction.CallbackContext _)
        {
            if (!_playerActorManager.IsPlayerActorControlEnabled() ||
                !_playerActorManager.IsPlayerInputEnabled())
            {
                return;
            }

            if (IsPlayerActorInsideJumpableArea())
            {
                Pal3.Instance.StartCoroutine(JumpAsync());
            }
            else
            {
                InteractWithFacingInteractable();
            }
        }

        /// <summary>
        /// Interact with the nearby interactable object by the player actor facing direction.
        /// </summary>
        private void InteractWithFacingInteractable()
        {
            Vector3 actorFacingDirection = _playerActorMovementController.transform.forward;
            Vector3 actorCenterPosition = _playerActorActionController.GetRendererBounds().center;
            var currentLayerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            float nearestInteractableFacingAngle = 181f;
            IEnumerator interactionRoutine = null;

            foreach (var sceneObjectId in
                     _sceneManager.GetCurrentScene().GetAllActivatedSceneObjects())
            {
                SceneObject sceneObject = _sceneManager.GetCurrentScene().GetSceneObject(sceneObjectId);

                Vector3 closetPointOnObject = sceneObject.GetRendererBounds().ClosestPoint(actorCenterPosition);
                float distanceToActor = Vector3.Distance(actorCenterPosition, closetPointOnObject);
                Vector3 actorToObjectFacing = closetPointOnObject - actorCenterPosition;
                float facingAngle = Vector2.Angle(
                    new Vector2(actorFacingDirection.x, actorFacingDirection.z),
                    new Vector2(actorToObjectFacing.x, actorToObjectFacing.z));

                if (sceneObject.IsDirectlyInteractable(distanceToActor) &&
                    facingAngle < nearestInteractableFacingAngle)
                {
                    //Debug.DrawLine(actorCenterPosition, closetPointOnObject, Color.white, 1000);
                    nearestInteractableFacingAngle = facingAngle;
                    interactionRoutine = InteractWithSceneObjectAsync(sceneObject, startedByPlayer: true);
                }
            }

            foreach (var actorInfo in
                     _sceneManager.GetCurrentScene().GetAllActorGameObjects())
            {
                var actorController = actorInfo.Value.GetComponent<ActorController>();
                var actorActionController = actorInfo.Value.GetComponent<ActorActionController>();
                var actorMovementController = actorInfo.Value.GetComponent<ActorMovementController>();

                if (actorMovementController.GetCurrentLayerIndex() != currentLayerIndex ||
                    actorInfo.Key == (int)_playerActorManager.GetPlayerActor() ||
                    !actorController.IsActive) continue;

                Vector3 targetActorCenterPosition = actorActionController.GetRendererBounds().center;
                var distance = Vector3.Distance(actorCenterPosition,targetActorCenterPosition);
                Vector3 actorToActorFacing = targetActorCenterPosition - actorCenterPosition;
                float facingAngle = Vector2.Angle(
                    new Vector2(actorFacingDirection.x, actorFacingDirection.z),
                    new Vector2(actorToActorFacing.x, actorToActorFacing.z));

                if (actorController.IsDirectlyInteractable(distance) &&
                    facingAngle < nearestInteractableFacingAngle)
                {
                    //Debug.DrawLine(actorCenterPosition, targetActorCenterPosition, Color.white, 1000);
                    nearestInteractableFacingAngle = facingAngle;
                    interactionRoutine = InteractWithActorAsync(actorInfo.Key, actorInfo.Value);
                }
            }

            if (interactionRoutine != null)
            {
                Pal3.Instance.StartCoroutine(interactionRoutine);
            }
        }

        private IEnumerator InteractWithSceneObjectAsync(SceneObject sceneObject, bool startedByPlayer)
        {
            var correlationId = Guid.NewGuid();
            var requiresStateChange = sceneObject.ShouldGoToCutsceneWhenInteractionStarted();

            if (requiresStateChange)
            {
                _gameStateManager.TryGoToState(GameState.Cutscene);
                _gameStateManager.AddGamePlayStateLocker(correlationId);
            }

            yield return sceneObject.InteractAsync(new InteractionContext
            {
                CorrelationId = correlationId,
                InitObjectId = sceneObject.ObjectInfo.Id,
                PlayerActorGameObject = _playerActorGameObject,
                CurrentScene = _sceneManager.GetCurrentScene(),
                StartedByPlayer = startedByPlayer,
            });

            if (requiresStateChange)
            {
                _gameStateManager.RemoveGamePlayStateLocker(correlationId);
                _gameStateManager.TryGoToState(GameState.Gameplay);
            }
        }

        private IEnumerator InteractWithActorAsync(int actorId, GameObject actorGameObject)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Cutscene));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            Actor targetActor = _sceneManager.GetCurrentScene().GetActor(actorId);
            Quaternion rotationBeforeInteraction = actorGameObject.transform.rotation;

            var actorController = actorGameObject.GetComponent<ActorController>();
            var movementController = actorGameObject.GetComponent<ActorMovementController>();

            // Pause current path follow movement of the interacting actor
            if (actorController != null &&
                movementController != null &&
                actorController.GetCurrentBehaviour() == ActorBehaviourType.PathFollow)
            {
                movementController.PauseMovement();
            }

            // Look at the target actor
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorLookAtActorCommand(_playerActor.Id, targetActor.Id));

            bool shouldResetFacingAfterInteraction = false;

            // Only let target actor look at player actor when NoTurn is set to false
            // and InitBehaviour is not set to Hold
            if (targetActor.Info.NoTurn == 0 && targetActor.Info.InitBehaviour != ActorBehaviourType.Hold)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorLookAtActorCommand(targetActor.Id, _playerActor.Id));
                shouldResetFacingAfterInteraction = true;
            }

            // Resume animation of the target actor if it is set to hold and loop action is set to 0
            if (targetActor.Info.InitBehaviour == ActorBehaviourType.Hold &&
                targetActor.Info.LoopAction == 0)
            {
                var actionController = actorGameObject.GetComponent<ActorActionController>();
                // PerformAction internally checks if the target actor is already performing the action
                actionController.PerformAction(actionController.GetCurrentAction(), false, 1);
            }

            // Run dialogue script
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunCommand((int)targetActor.GetScriptId()));

            // Wait until the dialogue script is finished
            yield return new WaitUntilScriptFinished(PalScriptType.Scene, targetActor.GetScriptId());

            // Resume current path follow movement of the interacting actor
            if (actorController != null &&
                movementController != null &&
                actorController.GetCurrentBehaviour() == ActorBehaviourType.PathFollow)
            {
                movementController.ResumeMovement();
            }

            // Reset facing rotation of the interacting actor if needed
            if (shouldResetFacingAfterInteraction && actorGameObject != null)
            {
                actorGameObject.transform.rotation = rotationBeforeInteraction;
            }
        }
    }
}