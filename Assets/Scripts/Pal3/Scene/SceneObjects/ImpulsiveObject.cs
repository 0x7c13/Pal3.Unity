// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using System.Threading;
    using Actor;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataLoader;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Data;
    using MetaData;
    using Renderer;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using Random = UnityEngine.Random;

    [ScnSceneObject(ScnSceneObjectType.Impulsive)]
    public sealed class ImpulsiveObject : SceneObject
    {
        private const float HIT_ANIMATION_DURATION = 0.5f;

        private GameObject _subObjectGameObject;
        private ImpulsiveMechanismSubObjectController _subObjectController;

        private bool _isDuringInteraction;

        public ImpulsiveObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
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

            _subObjectController = _subObjectGameObject.AddComponent<ImpulsiveMechanismSubObjectController>();
            _subObjectController.Init();
            _subObjectController.OnPlayerActorHit += OnPlayerActorHit;

            _subObjectGameObject.transform.SetParent(sceneGameObject.transform, false);

            return sceneGameObject;
        }

        private void OnPlayerActorHit(object sender, GameObject playerActorGameObject)
        {
            if (_isDuringInteraction) return; // Prevent multiple interactions during animation
            _isDuringInteraction = true;

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFreeCommand(0));
            RequestForInteraction();
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfx("wb002");

            ctx.PlayerActorGameObject.GetComponent<ActorActionController>()
                .PerformAction(ActorActionType.BeAttack);

            Vector3 targetPosition = ctx.PlayerActorGameObject.transform.position +
                                     (_subObjectGameObject.transform.forward * 6f) + Vector3.up * 2f;

            yield return AnimationHelper.MoveTransformAsync(ctx.PlayerActorGameObject.transform,
                targetPosition,
                HIT_ANIMATION_DURATION);

            ctx.PlayerActorGameObject.GetComponent<ActorMovementController>().SetNavLayer(ObjectInfo.Parameters[2]);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    ObjectInfo.Parameters[0],
                    ObjectInfo.Parameters[1]));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFreeCommand(1));

            _isDuringInteraction = false;
        }

        public override void Deactivate()
        {
            if (_subObjectController != null)
            {
                _subObjectController.OnPlayerActorHit -= OnPlayerActorHit;
                Object.Destroy(_subObjectController);
            }

            if (_subObjectGameObject != null)
            {
                Object.Destroy(_subObjectGameObject);
            }

            base.Deactivate();
        }
    }

    internal class ImpulsiveMechanismSubObjectController : MonoBehaviour
    {
        public event EventHandler<GameObject> OnPlayerActorHit;

        private const float MIN_Z_POSITION = -1.7f;
        private const float MAX_Z_POSITION = 4f;
        private const float POSITION_HOLD_TIME = 3f;
        private const float MOVEMENT_ANIMATION_DURATION = 2.5f;

        private BoundsTriggerController _triggerController;
        private Coroutine _movementCoroutine;

        private CancellationTokenSource _movementAnimationCts = new ();

        public void Init()
        {
            // Add collider
            var bounds = new Bounds
            {
                center = new Vector3(0f, 1f, -1f),
                size = new Vector3(3f, 2f, 7f),
            };

            _triggerController = gameObject.AddComponent<BoundsTriggerController>();
            _triggerController.SetupCollider(bounds, true);
            _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;

            // Set initial position
            Vector3 subObjectInitPosition = transform.position;
            subObjectInitPosition.z = MIN_Z_POSITION;
            transform.position = subObjectInitPosition;
        }

        private void OnPlayerActorEntered(object sender, GameObject playerActorGameObject)
        {
            OnPlayerActorHit?.Invoke(sender, playerActorGameObject);
        }

        private void OnDisable()
        {
            _movementAnimationCts.Cancel();

            if (_movementCoroutine != null)
            {
                StopCoroutine(_movementCoroutine);
                _movementCoroutine = null;
            }

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                Destroy(_triggerController);
            }
        }

        private void Start()
        {
            _movementAnimationCts = new CancellationTokenSource();
            _movementCoroutine = StartCoroutine(StartMovementAsync(_movementAnimationCts.Token));
        }

        private IEnumerator StartMovementAsync(CancellationToken cancellationToken)
        {
            var startDelay = Random.Range(0f, 3.5f);
            yield return new WaitForSeconds(startDelay);

            Vector3 initPosition = transform.localPosition;
            var holdTimeWaiter = new WaitForSeconds(POSITION_HOLD_TIME);

            while (!cancellationToken.IsCancellationRequested)
            {
                yield return AnimationHelper.EnumerateValueAsync(MIN_Z_POSITION,
                    MAX_Z_POSITION,
                    MOVEMENT_ANIMATION_DURATION,
                    AnimationCurveType.Linear,
                    (value) =>
                {
                    Vector3 newPosition = initPosition;
                    newPosition.z = value;
                    transform.localPosition = newPosition;
                }, cancellationToken);

                yield return holdTimeWaiter;

                yield return AnimationHelper.EnumerateValueAsync(MAX_Z_POSITION,
                    MIN_Z_POSITION,
                    MOVEMENT_ANIMATION_DURATION,
                    AnimationCurveType.Linear,
                    (value) =>
                    {
                        Vector3 newPosition = initPosition;
                        newPosition.z = value;
                        transform.localPosition = newPosition;
                    }, cancellationToken);

                yield return holdTimeWaiter;
            }
        }
    }
}