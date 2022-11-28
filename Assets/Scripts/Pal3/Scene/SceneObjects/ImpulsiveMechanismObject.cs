// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Actor;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataLoader;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.Services;
    using Data;
    using MetaData;
    using Player;
    using Renderer;
    using State;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using Random = UnityEngine.Random;

    [ScnSceneObject(ScnSceneObjectType.ImpulsiveMechanism)]
    public class ImpulsiveMechanismObject : SceneObject
    {
        private GameObject _subObjectGameObject;
        
        public ImpulsiveMechanismObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            _subObjectGameObject = new GameObject($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}_SubObject");

            var subObjectModelPath = ModelFilePath.Insert(ModelFilePath.LastIndexOf('.'), "a");
            (PolFile PolFile, ITextureResourceProvider TextureProvider) poly = resourceProvider.GetPol(subObjectModelPath);
            var subObjectModelRenderer = _subObjectGameObject.AddComponent<PolyModelRenderer>();
            subObjectModelRenderer.Render(poly.PolFile,
                resourceProvider.GetMaterialFactory(),
                poly.TextureProvider,
                tintColor);
            
            _subObjectGameObject.AddComponent<ImpulsiveMechanismObjectController>().Init(this);
            
            _subObjectGameObject.transform.SetParent(sceneGameObject.transform, false);
            
            return sceneGameObject;
        }

        public override void Deactivate()
        {
            if (_subObjectGameObject != null)
            {
                Object.Destroy(_subObjectGameObject);
            }
            
            base.Deactivate();
        }
    }

    internal class ImpulsiveMechanismObjectController : MonoBehaviour
    {
        private const float MIN_Z_POSITION = -1.7f;
        private const float MAX_Z_POSITION = 4f;
        private const float POSITION_HOLD_TIME = 3f;
        private const float MOVEMENT_ANIMATION_DURATION = 2.5f;
        private const float HIT_ANIMATION_DURATION = 0.5f;

        private ImpulsiveMechanismObject _object;
        private BoxCollider _collider;
        private Coroutine _movementCoroutine;
        private PlayerManager _playerManager;
        private bool _isDuringInteraction;

        public void Init(ImpulsiveMechanismObject impulsiveMechanismObject)
        {
            _playerManager = ServiceLocator.Instance.Get<PlayerManager>();
            _object = impulsiveMechanismObject;

            // Add collider
            var bounds = new Bounds
            {
                center = new Vector3(0f, 1f, -1f),
                size = new Vector3(3f, 2f, 7f),
            };
            
            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.center = bounds.center;
            _collider.size = bounds.size;
            _collider.isTrigger = true;

            // Set initial position
            Vector3 subObjectInitPosition = transform.position;
            subObjectInitPosition.z = MIN_Z_POSITION;
            transform.position = subObjectInitPosition;
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (_isDuringInteraction) return; // Prevent multiple interactions during animation
            
            // Check if collider game object is player actor
            if (collider.gameObject.GetComponent<ActorController>() is { } actorController &&
                actorController.GetActor().Info.Id == (byte) _playerManager.GetPlayerActor())
            {
                _isDuringInteraction = true;
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new GameStateChangeRequest(GameState.Cutscene));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new CameraFreeCommand(0));
                StartCoroutine(Interact(collider.gameObject));
            }
        }

        private IEnumerator Interact(GameObject playerActorGameObject)
        {
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlaySfxCommand("wb002", 1));
            
            playerActorGameObject.GetComponent<ActorActionController>()
                .PerformAction(ActorActionType.BeAttack);
            
            Vector3 targetPosition = playerActorGameObject.transform.position + (transform.forward * 6f) + Vector3.up * 2f;

            yield return AnimationHelper.MoveTransform(playerActorGameObject.transform,
                targetPosition,
                HIT_ANIMATION_DURATION);
            
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetNavLayerCommand(ActorConstants.PlayerActorVirtualID,
                    _object.ObjectInfo.Parameters[2]));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    _object.ObjectInfo.Parameters[0],
                    _object.ObjectInfo.Parameters[1]));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFreeCommand(1));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new GameStateChangeRequest(GameState.Gameplay));
            
            _isDuringInteraction = false;
        }
        
        private void OnDisable()
        {
            _isDuringInteraction = false;
            
            if (_movementCoroutine != null)
            {
                StopCoroutine(_movementCoroutine);
                _movementCoroutine = null;
            }
            
            if (_collider != null)
            {
                Destroy(_collider);
            }
        }

        public void Start()
        {
            _movementCoroutine = StartCoroutine(StartMovement());
        }

        private IEnumerator StartMovement()
        {
            float startDelay = Random.Range(0f, 3.5f);
            yield return new WaitForSeconds(startDelay);

            Vector3 initPosition = transform.localPosition;
            WaitForSeconds holdTimeWaiter = new WaitForSeconds(POSITION_HOLD_TIME);
            
            while (isActiveAndEnabled)
            {
                yield return AnimationHelper.EnumerateValue(MIN_Z_POSITION,
                    MAX_Z_POSITION,
                    MOVEMENT_ANIMATION_DURATION,
                    AnimationCurveType.Linear,
                    (value) =>
                {
                    Vector3 newPosition = initPosition;
                    newPosition.z = value;
                    transform.localPosition = newPosition;
                });

                yield return holdTimeWaiter;
                
                yield return AnimationHelper.EnumerateValue(MAX_Z_POSITION,
                    MIN_Z_POSITION,
                    MOVEMENT_ANIMATION_DURATION,
                    AnimationCurveType.Linear,
                    (value) =>
                    {
                        Vector3 newPosition = initPosition;
                        newPosition.z = value;
                        transform.localPosition = newPosition;
                    });

                
                yield return holdTimeWaiter;
            }
        }
    }
}