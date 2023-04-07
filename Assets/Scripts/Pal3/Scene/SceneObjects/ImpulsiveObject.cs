// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

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

            Color subObjectTintColor = tintColor;

            #if PAL3
            // Fix the color(texture) issue of impulsive mechanism in M11-2 which uses _r.pol as the main model
            if (string.Equals(ModelFilePath, @"M11.cpk\2\_r.pol", StringComparison.OrdinalIgnoreCase))
            {
                tintColor = new Color(0.9f, 0.45f, 0f, 0.1f);
            }
            #endif

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            _subObjectGameObject = new GameObject($"Object_{ObjectInfo.Id}_{ObjectInfo.Type}_SubObject");

            var subObjectModelPath = ModelFilePath.Insert(ModelFilePath.LastIndexOf('.'), "a");
            (PolFile polFile, string relativeDirectoryPath) = resourceProvider.GetGameResourceFile<PolFile>(subObjectModelPath);
            ITextureResourceProvider textureProvider = resourceProvider.GetTextureResourceProvider(relativeDirectoryPath);
            var subObjectModelRenderer = _subObjectGameObject.AddComponent<PolyModelRenderer>();
            subObjectModelRenderer.Render(polFile,
                textureProvider,
                resourceProvider.GetMaterialFactory(),
                subObjectTintColor);

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
                new CameraFollowPlayerCommand(0));
            RequestForInteraction();
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            PlaySfx("wb002");

            ctx.PlayerActorGameObject.GetComponent<ActorActionController>()
                .PerformAction(ActorActionType.BeAttack, true, 1);

            Vector3 targetPosition = ctx.PlayerActorGameObject.transform.position +
                                     (_subObjectGameObject.transform.forward * 6f) + Vector3.up * 2f;

            yield return ctx.PlayerActorGameObject.transform.MoveAsync(targetPosition,
                HIT_ANIMATION_DURATION);

            ctx.PlayerActorGameObject.GetComponent<ActorMovementController>().SetNavLayer(ObjectInfo.Parameters[2]);
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorSetTilePositionCommand(ActorConstants.PlayerActorVirtualID,
                    ObjectInfo.Parameters[0],
                    ObjectInfo.Parameters[1]));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new CameraFollowPlayerCommand(1));

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
            _triggerController.SetBounds(bounds, true);
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
                yield return CoreAnimation.EnumerateValueAsync(MIN_Z_POSITION,
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

                yield return CoreAnimation.EnumerateValueAsync(MAX_Z_POSITION,
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

#endif