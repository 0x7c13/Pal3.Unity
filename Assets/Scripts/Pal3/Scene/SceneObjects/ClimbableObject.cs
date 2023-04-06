// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Actor;
    using Common;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using GamePlay;
    using MetaData;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Climbable)]
    public sealed class ClimbableObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 5f;

        private readonly PlayerGamePlayController _playerGamePlayController;

        private readonly string _interactionIndicatorModelPath = FileConstants.ObjectFolderVirtualPath +
                                                                 CpkConstants.DirectorySeparator + "g02.cvd";
        private CvdModelRenderer _upperInteractionIndicatorRenderer;
        private CvdModelRenderer _lowerInteractionIndicatorRenderer;
        private GameObject _upperInteractionIndicatorGameObject;
        private GameObject _lowerInteractionIndicatorGameObject;

        public ClimbableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _playerGamePlayController = ServiceLocator.Instance.Get<PlayerGamePlayController>();
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return Activated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            Vector3 climbablePosition = sceneGameObject.transform.position;
            Bounds bounds = GetRendererBounds();

            Vector3 upperPosition = new Vector3(climbablePosition.x, bounds.max.y, climbablePosition.z) +
                                    sceneGameObject.transform.forward * 0.7f;

            IMaterialFactory materialFactory = resourceProvider.GetMaterialFactory();

            // Upper indicator
            {
                _upperInteractionIndicatorGameObject = new GameObject("Climbable_Interaction_Indicator_Upper");
                _upperInteractionIndicatorGameObject.transform.SetParent(sceneGameObject.transform, false);
                _upperInteractionIndicatorGameObject.transform.localScale = new Vector3(1f, -1f, 1f);
                _upperInteractionIndicatorGameObject.transform.position = upperPosition;
                (CvdFile cvdFile, string relativeDirectoryPath) =
                    resourceProvider.GetGameResourceFile<CvdFile>(_interactionIndicatorModelPath);
                ITextureResourceProvider textureProvider = resourceProvider.GetTextureResourceProvider(relativeDirectoryPath);
                _upperInteractionIndicatorRenderer = _upperInteractionIndicatorGameObject.AddComponent<CvdModelRenderer>();
                _upperInteractionIndicatorRenderer.Init(cvdFile,
                    textureProvider,
                    materialFactory,
                    tintColor);
                _upperInteractionIndicatorRenderer.LoopAnimation();
            }

            Vector3 lowerPosition = new Vector3(climbablePosition.x, bounds.min.y + 1f, climbablePosition.z) +
                                    sceneGameObject.transform.forward * 0.7f;

            // Lower indicator
            {
                _lowerInteractionIndicatorGameObject = new GameObject("Climbable_Interaction_Indicator_Lower");
                _lowerInteractionIndicatorGameObject.transform.SetParent(sceneGameObject.transform, false);
                _lowerInteractionIndicatorGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                _lowerInteractionIndicatorGameObject.transform.position = lowerPosition;
                (CvdFile cvdFile, string relativeDirectoryPath) =
                    resourceProvider.GetGameResourceFile<CvdFile>(_interactionIndicatorModelPath);
                ITextureResourceProvider textureProvider = resourceProvider.GetTextureResourceProvider(relativeDirectoryPath);
                _lowerInteractionIndicatorRenderer = _lowerInteractionIndicatorGameObject.AddComponent<CvdModelRenderer>();
                _lowerInteractionIndicatorRenderer.Init(cvdFile,
                    textureProvider,
                    materialFactory,
                    tintColor);
                _lowerInteractionIndicatorRenderer.LoopAnimation();
            }

            return sceneGameObject;
        }

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (ObjectInfo.ScriptId != ScriptConstants.InvalidScriptId)
            {
                yield return ExecuteScriptAndWaitForFinishIfAnyAsync();
                yield break;
            }

            // We should not execute further if interaction is initially
            // triggered by another object.
            if (ctx.InitObjectId != ObjectInfo.Id)
            {
                yield break;
            }

            var fromTilePosition = new Vector2Int(ObjectInfo.Parameters[0], ObjectInfo.Parameters[1]);
            var toTilePosition = new Vector2Int(ObjectInfo.Parameters[2], ObjectInfo.Parameters[3]);
            var crossLayer = ObjectInfo.Parameters[4] == 1;

            var actorMovementController = ctx.PlayerActorGameObject.GetComponent<ActorMovementController>();
            GameObject climbableObject = GetGameObject();

            Vector3 climbableObjectPosition = climbableObject.transform.position;
            Vector3 climbableObjectFacing = GameBoxInterpreter.ToUnityRotation(
                new Vector3(0f, ObjectInfo.GameBoxYRotation, 0f)) * Vector3.forward;

            Tilemap tileMap = ctx.CurrentScene.GetTilemap();

            int upperLayer, lowerLayer;

            if (crossLayer)
            {
                upperLayer = 1;
                lowerLayer = 0;
            }
            else
            {
                var playerCurrentLayer = actorMovementController.GetCurrentLayerIndex();
                upperLayer = playerCurrentLayer;
                lowerLayer = playerCurrentLayer;
            }

            Vector3 fromPosition = tileMap.GetWorldPosition(fromTilePosition, lowerLayer);
            Vector3 toPosition = tileMap.GetWorldPosition(toTilePosition, upperLayer);

            Vector3 upperStandingPosition, lowerStandingPosition;
            if (fromPosition.y > toPosition.y)
            {
                upperStandingPosition = fromPosition;
                lowerStandingPosition = toPosition;
            }
            else
            {
                upperStandingPosition = toPosition;
                lowerStandingPosition = fromPosition;
            }

            Vector3 upperPosition = -climbableObjectFacing.normalized * 1f + climbableObjectPosition;
            Vector3 lowerPosition = climbableObjectFacing.normalized * 1f + climbableObjectPosition;
            upperPosition.y = upperStandingPosition.y;
            lowerPosition.y = lowerStandingPosition.y;

            Vector3 playerActorPosition = actorMovementController.GetWorldPosition();
            var climbUp = Mathf.Abs(playerActorPosition.y - lowerPosition.y) <
                              Mathf.Abs(playerActorPosition.y - upperPosition.y);

            var climbableHeight = upperPosition.y - lowerPosition.y;

            yield return _playerGamePlayController.PlayerActorMoveToClimbableObjectAndClimbAsync(climbableObject,
                climbUp,
                false,
                climbableHeight,
                lowerPosition,
                lowerStandingPosition,
                upperPosition,
                upperStandingPosition,
                lowerLayer,
                upperLayer);
        }

        public override void Deactivate()
        {
            if (_upperInteractionIndicatorRenderer != null)
            {
                _upperInteractionIndicatorRenderer.Dispose();
                Object.Destroy(_upperInteractionIndicatorRenderer);
            }

            if (_lowerInteractionIndicatorRenderer != null)
            {
                _lowerInteractionIndicatorRenderer.Dispose();
                Object.Destroy(_lowerInteractionIndicatorRenderer);
            }

            if (_upperInteractionIndicatorGameObject != null)
            {
                Object.Destroy(_upperInteractionIndicatorGameObject);
            }

            if (_lowerInteractionIndicatorGameObject != null)
            {
                Object.Destroy(_lowerInteractionIndicatorGameObject);
            }

            base.Deactivate();
        }
    }
}