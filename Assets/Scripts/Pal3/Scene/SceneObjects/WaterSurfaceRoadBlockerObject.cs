// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;
    using Data;
    using Engine.Animation;
    using Engine.Extensions;
    using Engine.Services;
    using UnityEngine;

    [ScnSceneObject(SceneObjectType.WaterSurfaceRoadBlocker)]
    public sealed class WaterSurfaceRoadBlockerObject : SceneObject
    {
        private const float WATER_ANIMATION_DURATION = 4f;

        private readonly SceneManager _sceneManager;

        public WaterSurfaceRoadBlockerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneManager = ServiceLocator.Instance.Get<SceneManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (IsActivated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            UpdateTileMapWhenConditionMet(true);

            return sceneGameObject;
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => true;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            GameObject waterSurfaceGameObject = GetGameObject();
            Vector3 finalPosition = waterSurfaceGameObject.transform.position;
            finalPosition.y = ((float)ObjectInfo.Parameters[0]).ToUnityYPosition();

            PlaySfx("wc007");

            yield return waterSurfaceGameObject.transform.MoveAsync(finalPosition,
                WATER_ANIMATION_DURATION,
                AnimationCurveType.Sine);

            ChangeAndSaveActivationState(false);
        }

        public override void Deactivate()
        {
            UpdateTileMapWhenConditionMet(false);
            base.Deactivate();
        }

        // 仙三霹雳堂水面机关对TileMap地砖通过性的特殊处理:
        // * 当水面机关开启之前，需要将地砖类型为土壤的地砖设置为不可通过
        // * 当水面机关开启之后，需要将地砖类型为土壤的地砖设置为可通过
        private void UpdateTileMapWhenConditionMet(bool setSoilFloorAsObstacle)
        {
            if (SceneInfo.IsCity("m09"))
            {
                _sceneManager.GetCurrentScene()
                    .GetTilemap()
                    .MarkFloorTypeAsObstacle(FloorType.Soil, setSoilFloorAsObstacle);
            }
        }
    }
}

#endif