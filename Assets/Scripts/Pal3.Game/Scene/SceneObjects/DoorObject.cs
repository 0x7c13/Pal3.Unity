// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Extensions;
    using Engine.Services;
    using Rendering.Renderer;

    using Color = Core.Primitives.Color;

    [ScnSceneObject(SceneObjectType.Door)]
    public sealed class DoorObject : SceneObject
    {
        private TilemapTriggerController _triggerController;
        private bool _isInteractionInProgress;

        #if PAL3A
        private SceneObjectMeshCollider _meshCollider;
        #endif

        public DoorObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override IGameEntity Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameEntity();

            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            if (!ObjectInfo.TileMapTriggerRect.IsEmpty)
            {
                // This is to prevent player from entering back to previous
                // scene when holding the stick while transferring between scenes.
                // We simply disable the auto trigger for a short time window after
                // a fresh scene load.
                var effectiveTime = GameTimeProvider.Instance.RealTimeSinceStartup + 1f; // 1 second
                _triggerController = sceneObjectGameEntity.AddComponent<TilemapTriggerController>();
                _triggerController.Init(ObjectInfo.TileMapTriggerRect, ObjectInfo.LayerIndex, effectiveTime);
                _triggerController.OnPlayerActorEntered += OnPlayerActorEntered;
            }

            #if PAL3A
            if (ObjectInfo.Parameters[1] == 1 &&
                ObjectInfo.Parameters[2] == 1)
            {
                if (ObjectInfo.SwitchState == 0)
                {
                    // Add collider to block player
                    _meshCollider = sceneObjectGameEntity.AddComponent<SceneObjectMeshCollider>();
                }
                else if (ObjectInfo.SwitchState == 1)
                {
                    if (ModelType == SceneObjectModelType.CvdModel)
                    {
                        CvdModelRenderer cvdModelRenderer = GetCvdModelRenderer();
                        cvdModelRenderer.SetCurrentTime(cvdModelRenderer.GetDefaultAnimationDuration());
                    }
                    else if (ModelType == SceneObjectModelType.PolModel)
                    {
                        var modelRenderer = GetPolyModelRenderer();
                        modelRenderer.Dispose();
                        modelRenderer.Destroy();
                    }
                }
            }
            #endif

            return sceneObjectGameEntity;
        }

        private void OnPlayerActorEntered(object sender, (int x, int y) tilePosition)
        {
            if (_isInteractionInProgress) return; // Prevent re-entry

            #if PAL3
            _isInteractionInProgress = true;
            RequestForInteraction();
            #elif PAL3A
            if (ObjectInfo.Parameters[1] == 1 &&
                ObjectInfo.Parameters[2] == 1)
            {
                // Do nothing
            }
            else
            {
                _isInteractionInProgress = true;
                RequestForInteraction();
            }
            #endif
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            #if PAL3A
            if (ObjectInfo.Parameters[1] == 1 &&
                ObjectInfo.Parameters[2] == 1)
            {
                if (ObjectInfo.SwitchState == 1)
                {
                    _isInteractionInProgress = false;
                    yield break;
                }

                PlaySfxIfAny();

                if (ModelType == SceneObjectModelType.PolModel)
                {
                    var polyModelRenderer = GetPolyModelRenderer();
                    polyModelRenderer.Dispose();
                    polyModelRenderer.Destroy();
                }
                else if (ModelType == SceneObjectModelType.CvdModel)
                {
                    yield return GetCvdModelRenderer().PlayOneTimeAnimationAsync(true);
                }

                if (_meshCollider != null)
                {
                    _meshCollider.Destroy();
                    _meshCollider = null;
                }

                FlipAndSaveSwitchState();

                yield return ExecuteScriptAndWaitForFinishIfAnyAsync();

                _isInteractionInProgress = false;
                yield break;
            }
            #endif

            // There are doors controlled by the script for it's behaviour & animation which have
            // parameters[0] set to 1, so we are only playing the animation if parameters[0] == 0.
            if (ObjectInfo.Parameters[0] == 0 && ModelType == SceneObjectModelType.CvdModel)
            {
                const float timeScale = 2f; // Make the animation 2X faster for better user experience
                const float durationPercentage = 0.7f; // Just play 70% of the whole animation (good enough).
                yield return GetCvdModelRenderer().PlayAnimationAsync(timeScale,
                    loopCount: 1, durationPercentage, true);
            }

            yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
            _isInteractionInProgress = false;
        }

        public override void Deactivate()
        {
            _isInteractionInProgress = false;

            if (_triggerController != null)
            {
                _triggerController.OnPlayerActorEntered -= OnPlayerActorEntered;
                _triggerController.Destroy();
                _triggerController = null;
            }

            #if PAL3A
            if (_meshCollider != null)
            {
                _meshCollider.Destroy();
                _meshCollider = null;
            }
            #endif

            base.Deactivate();
        }
    }
}