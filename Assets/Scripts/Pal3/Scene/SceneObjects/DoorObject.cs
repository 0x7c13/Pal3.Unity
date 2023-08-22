// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.DataReader.Scn;
    using Core.Extensions;
    using Data;
    using Rendering.Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Door)]
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

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();

            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            if (!ObjectInfo.TileMapTriggerRect.IsEmpty)
            {
                // This is to prevent player from entering back to previous
                // scene when holding the stick while transferring between scenes.
                // We simply disable the auto trigger for a short time window after
                // a fresh scene load.
                var effectiveTime = Time.realtimeSinceStartupAsDouble + 1f;
                _triggerController = sceneGameObject.AddComponent<TilemapTriggerController>();
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
                    _meshCollider = sceneGameObject.AddComponent<SceneObjectMeshCollider>();
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
                        GetPolyModelRenderer().Dispose();
                    }
                }
            }
            #endif

            return sceneGameObject;
        }

        private void OnPlayerActorEntered(object sender, Vector2Int actorTilePosition)
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
                    GetPolyModelRenderer().Dispose();
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