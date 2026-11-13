// ------------------------------------------------------------------------
//  Copyright (c) 2021-2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.GamePlay
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Actor;
    using Actor.Controllers;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Command.SceCommands;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Logging;
    using Scene;
    using Scene.SceneObjects;
    using Script;
    using Script.Waiter;
    using State;
    using UnityEngine.InputSystem;

    using Quaternion = UnityEngine.Quaternion;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;

    public partial class PlayerGamePlayManager :
        ICommandExecutor<PlayerInteractionRequest>,
        ICommandExecutor<PlayerInteractWithObjectCommand>
    {
        private int _interactionGeneration;

        private void StartInteraction(IEnumerator routine)
        {
            int generation = _interactionGeneration;
            Pal3.Instance.StartCoroutine(RunInteractionAsync(routine, () => generation != _interactionGeneration));
        }

        // Unwind the entire linked-object chain on reset, including nested script
        // waits. Resuming just the innermost waiter could run an old parent's actions.
        internal static IEnumerator RunInteractionAsync(IEnumerator routine, Func<bool> isCanceled)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            try
            {
                while (stack.Count > 0 && !isCanceled())
                {
                    IEnumerator current = stack.Peek();
                    if (!current.MoveNext())
                    {
                        stack.Pop();
                        (current as IDisposable)?.Dispose();
                    }
                    else if (current.Current is IEnumerator nested)
                    {
                        stack.Push(nested);
                    }
                    else
                    {
                        if (isCanceled()) yield break;
                        yield return current.Current;
                    }
                }
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            }
        }

        public void Execute(PlayerInteractWithObjectCommand command)
        {
            GameScene currentScene = _sceneManager.GetCurrentScene();
            if (currentScene.GetAllActivatedSceneObjects().Contains(command.SceneObjectId))
            {
                StartInteraction(InteractWithSceneObjectAsync(
                    currentScene.GetSceneObject(command.SceneObjectId), startedByPlayer: false));
            }
            else
            {
                EngineLogger.LogError($"Scene object not found or not activated yet: {command.SceneObjectId}");
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
            Vector3 actorFacingDirection = _playerActorMovementController.Transform.Forward;
            Vector3 actorCenterPosition = _playerActorActionController.GetRendererBounds().center;
            int currentLayerIndex = _playerActorMovementController.GetCurrentLayerIndex();

            float nearestInteractableFacingAngle = 181f;
            IEnumerator interactionRoutine = null;

            foreach (int sceneObjectId in
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

            foreach (KeyValuePair<int, IGameEntity> actorInfo in
                     _sceneManager.GetCurrentScene().GetAllActorGameEntities())
            {
                ActorController actorController = actorInfo.Value.GetComponent<ActorController>();
                ActorActionController actorActionController = actorInfo.Value.GetComponent<ActorActionController>();
                ActorMovementController actorMovementController = actorInfo.Value.GetComponent<ActorMovementController>();

                if (actorMovementController.GetCurrentLayerIndex() != currentLayerIndex ||
                    actorInfo.Key == _playerActorManager.GetPlayerActorId() ||
                    !actorController.IsActive) continue;

                Vector3 targetActorCenterPosition = actorActionController.GetRendererBounds().center;
                float distance = Vector3.Distance(actorCenterPosition,targetActorCenterPosition);
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
                StartInteraction(interactionRoutine);
            }
        }

        private IEnumerator InteractWithSceneObjectAsync(SceneObject sceneObject, bool startedByPlayer)
        {
            Guid correlationId = Guid.NewGuid();
            bool requiresStateChange = sceneObject.ShouldGoToCutsceneWhenInteractionStarted();

            if (requiresStateChange)
            {
                _gameStateManager.TryGoToState(GameState.Cutscene);
                _gameStateManager.AddGamePlayStateLocker(correlationId);
            }

            yield return sceneObject.InteractAsync(new InteractionContext
            {
                CorrelationId = correlationId,
                InitObjectId = sceneObject.ObjectInfo.Id,
                PlayerActorGameEntity = _playerActorGameEntity,
                CurrentScene = _sceneManager.GetCurrentScene(),
                StartedByPlayer = startedByPlayer,
            });

            if (requiresStateChange)
            {
                _gameStateManager.RemoveGamePlayStateLocker(correlationId);
                _gameStateManager.TryGoToState(GameState.Gameplay);
            }
        }

        private IEnumerator InteractWithActorAsync(int actorId, IGameEntity actorGameEntity)
        {
            Pal3.Instance.Execute(new GameStateChangeRequest(GameState.Cutscene));
            Pal3.Instance.Execute(new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));

            GameActor targetActor = _sceneManager.GetCurrentScene().GetActor(actorId);
            Quaternion rotationBeforeInteraction = actorGameEntity.Transform.Rotation;

            ActorController actorController = actorGameEntity.GetComponent<ActorController>();
            ActorMovementController movementController = actorGameEntity.GetComponent<ActorMovementController>();
            ActorActionController actionController = actorGameEntity.GetComponent<ActorActionController>();
            ActorBehaviourType behaviourBeforeInteraction = actorController?.GetCurrentBehaviour() ?? targetActor.Info.InitBehaviour;
            string actionBeforeInteraction = actionController?.GetCurrentAction();
            bool restoreLoopingAction = actionController?.CurrentActionLoopCount == -1;

            // Pause current path follow movement of the interacting actor
            bool pausedMovement = targetActor.NoTurn == 0 && actorController != null &&
                movementController != null &&
                actorController.GetCurrentBehaviour() == ActorBehaviourType.PathFollow;
            if (pausedMovement)
            {
                movementController.PauseMovement();
            }

            // Look at the target actor
            Pal3.Instance.Execute(new ActorLookAtActorCommand(_playerActor.Id, targetActor.Id));

            bool shouldResetFacingAfterInteraction = false;

            // Only let target actor look at player actor when NoTurn is set to false
            // and the current behaviour is not Hold.
            if (targetActor.NoTurn == 0 && behaviourBeforeInteraction != ActorBehaviourType.Hold)
            {
                actionController?.PerformAction(targetActor.GetIdleAction());
                Pal3.Instance.Execute(new ActorLookAtActorCommand(targetActor.Id, _playerActor.Id));
                shouldResetFacingAfterInteraction = true;
            }

            // Resume animation of the target actor if it is set to hold and loop action is set to 0
            if (targetActor.NoTurn == 0 && behaviourBeforeInteraction == ActorBehaviourType.Hold &&
                targetActor.Info.LoopAction == 0)
            {
                // PerformAction internally checks if the target actor is already performing the action
                actionController.PerformAction(actionController.GetCurrentAction(), false, 1);
            }

            // Run dialogue script
            Pal3.Instance.Execute(new ScriptExecuteCommand((int)targetActor.GetScriptId()));

            // Wait until the dialogue script is finished
            using (var completion = new WaitUntilScriptFinished(PalScriptType.Scene, targetActor.GetScriptId()))
            {
                yield return completion;
                if (completion.IsCanceled || actorGameEntity.IsNullOrDisposed()) yield break;
            }

            // Resume current path follow movement of the interacting actor
            if (pausedMovement)
            {
                movementController.ResumeMovement();
            }

            // Reset facing rotation of the interacting actor if needed
            if (shouldResetFacingAfterInteraction && targetActor.NoTurn == 0 &&
                !actorGameEntity.IsNullOrDisposed() && actorController?.GetCurrentBehaviour() != ActorBehaviourType.Hold)
            {
                actorGameEntity.Transform.Rotation = rotationBeforeInteraction;
                // Finite/hold actions were interrupted by dialogue; replaying them
                // with the default loop count would turn them into endless loops.
                if (restoreLoopingAction && !string.IsNullOrEmpty(actionBeforeInteraction))
                {
                    actionController?.PerformAction(actionBeforeInteraction);
                }
            }
        }
    }
}