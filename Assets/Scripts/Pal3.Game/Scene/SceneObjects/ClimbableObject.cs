// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Actor.Controllers;
    using Common;
    using Core.Contract.Constants;
    using Core.Contract.Enums;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Scn;
    using Core.Primitives;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;
    using Engine.Core.Implementation;
    using Engine.Extensions;
    using Engine.Services;
    using GamePlay;
    using Rendering.Material;
    using Rendering.Renderer;

    using Bounds = UnityEngine.Bounds;
    using Color = Core.Primitives.Color;
    using Vector2Int = UnityEngine.Vector2Int;
    using Vector3 = UnityEngine.Vector3;

    [ScnSceneObject(SceneObjectType.Climbable)]
    public sealed class ClimbableObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 5f;

        private readonly PlayerGamePlayManager _playerGamePlayManager;

        private const string INTERACTION_INDICATOR_MODEL_FILE_NAME = "g02.cvd";

        private CvdModelRenderer _upperInteractionIndicatorRenderer;
        private CvdModelRenderer _lowerInteractionIndicatorRenderer;

        private IGameEntity _upperInteractionIndicatorGameEntity;
        private IGameEntity _lowerInteractionIndicatorGameEntity;

        public ClimbableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _playerGamePlayManager = ServiceLocator.Instance.Get<PlayerGamePlayManager>();
        }

        public override bool IsDirectlyInteractable(float distance)
        {
            return IsActivated && distance < MAX_INTERACTION_DISTANCE;
        }

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IGameEntity Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            if (IsActivated) return GetGameEntity();
            IGameEntity sceneObjectGameEntity = base.Activate(resourceProvider, tintColor);

            Vector3 climbablePosition = sceneObjectGameEntity.Transform.Position;
            Bounds bounds = GetRendererBounds();

            Vector3 upperPosition = new Vector3(climbablePosition.x, bounds.max.y, climbablePosition.z) +
                                    sceneObjectGameEntity.Transform.Forward * 0.7f;

            IMaterialManager materialManager = resourceProvider.GetMaterialManager();

            string indicatorModelPath = FileConstants.GetGameObjectModelFileVirtualPath(INTERACTION_INDICATOR_MODEL_FILE_NAME);
            CvdFile indicatorCvdFile = resourceProvider.GetGameResourceFile<CvdFile>(indicatorModelPath);
            ITextureResourceProvider textureProvider = resourceProvider.CreateTextureResourceProvider(
                CoreUtility.GetDirectoryName(indicatorModelPath, CpkConstants.DirectorySeparatorChar));

            // Upper indicator
            {
                _upperInteractionIndicatorGameEntity = GameEntityFactory.Create("Climbable_Interaction_Indicator_Upper",
                    sceneObjectGameEntity, worldPositionStays: false);
                _upperInteractionIndicatorGameEntity.Transform.LocalScale = new Vector3(1f, -1f, 1f);
                _upperInteractionIndicatorGameEntity.Transform.Position = upperPosition;
                _upperInteractionIndicatorRenderer = _upperInteractionIndicatorGameEntity.AddComponent<CvdModelRenderer>();
                _upperInteractionIndicatorRenderer.Init(indicatorCvdFile,
                    textureProvider,
                    materialManager,
                    tintColor);
                _upperInteractionIndicatorRenderer.LoopAnimation();
            }

            Vector3 lowerPosition = new Vector3(climbablePosition.x, bounds.min.y + 1f, climbablePosition.z) +
                                    sceneObjectGameEntity.Transform.Forward * 0.7f;

            // Lower indicator
            {
                _lowerInteractionIndicatorGameEntity = GameEntityFactory.Create("Climbable_Interaction_Indicator_Lower",
                    sceneObjectGameEntity, worldPositionStays: false);
                _lowerInteractionIndicatorGameEntity.Transform.LocalScale = new Vector3(1f, 1f, 1f);
                _lowerInteractionIndicatorGameEntity.Transform.Position = lowerPosition;
                _lowerInteractionIndicatorRenderer = _lowerInteractionIndicatorGameEntity.AddComponent<CvdModelRenderer>();
                _lowerInteractionIndicatorRenderer.Init(indicatorCvdFile,
                    textureProvider,
                    materialManager,
                    tintColor);
                _lowerInteractionIndicatorRenderer.LoopAnimation();
            }

            return sceneObjectGameEntity;
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

            var actorMovementController = ctx.PlayerActorGameEntity.GetComponent<ActorMovementController>();
            IGameEntity climbableGameEntity = GetGameEntity();

            Vector3 climbableObjectPosition = climbableGameEntity.Transform.Position;
            Vector3 climbableObjectFacing = new GameBoxVector3(0f, ObjectInfo.GameBoxYRotation, 0f)
                .ToUnityQuaternion() * Vector3.forward;

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
            var climbUp = MathF.Abs(playerActorPosition.y - lowerPosition.y) <
                              MathF.Abs(playerActorPosition.y - upperPosition.y);

            var climbableHeight = upperPosition.y - lowerPosition.y;

            yield return _playerGamePlayManager.PlayerActorMoveToClimbableObjectAndClimbAsync(climbableGameEntity,
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
                _upperInteractionIndicatorRenderer.Destroy();
                _upperInteractionIndicatorRenderer = null;
            }

            if (_lowerInteractionIndicatorRenderer != null)
            {
                _lowerInteractionIndicatorRenderer.Dispose();
                _lowerInteractionIndicatorRenderer.Destroy();
                _lowerInteractionIndicatorRenderer = null;
            }

            if (_upperInteractionIndicatorGameEntity != null)
            {
                _upperInteractionIndicatorGameEntity.Destroy();
                _upperInteractionIndicatorGameEntity = null;
            }

            if (_lowerInteractionIndicatorGameEntity != null)
            {
                _lowerInteractionIndicatorGameEntity.Destroy();
                _lowerInteractionIndicatorGameEntity = null;
            }

            base.Deactivate();
        }
    }
}