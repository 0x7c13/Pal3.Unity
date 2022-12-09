// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System;
    using System.Collections;
    using Command;
    using Command.SceCommands;
    using Common;
    using Core.Animation;
    using Core.DataReader.Nav;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Services;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.WaterSurface)]
    public sealed class WaterSurfaceObject : SceneObject
    {
        private const float WATER_ANIMATION_DURATION = 4f;

        private readonly SceneManager _sceneManager;

        public WaterSurfaceObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
            _sceneManager = ServiceLocator.Instance.Get<SceneManager>();
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            UpdateTileMapWhenConditionMet(true);

            return sceneGameObject;
        }

        public override IEnumerator Interact(InteractionContext ctx)
        {
            if (!IsInteractableBasedOnTimesCount()) yield break;

            GameObject waterSurfaceGameObject = GetGameObject();
            Vector3 finalPosition = waterSurfaceGameObject.transform.position;
            finalPosition.y = GameBoxInterpreter.ToUnityYPosition(ObjectInfo.Parameters[0]);

            PlaySfx("wc007");

            yield return AnimationHelper.MoveTransform(waterSurfaceGameObject.transform,
                finalPosition,
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
            #if PAL3
            if (SceneInfo.IsCity("m09"))
            {
                _sceneManager.GetCurrentScene()
                    .GetTilemap()
                    .MarkFloorKindAsObstacle(NavFloorKind.Soil, setSoilFloorAsObstacle);
            }
            #endif
        }
    }
}