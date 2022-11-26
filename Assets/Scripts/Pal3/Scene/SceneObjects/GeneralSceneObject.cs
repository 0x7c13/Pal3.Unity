// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.General)]
    [ScnSceneObject(ScnSceneObjectType.ImpulsiveMechanism)]
    [ScnSceneObject(ScnSceneObjectType.SwordBridge)]
    [ScnSceneObject(ScnSceneObjectType.JumpableArea)]
    [ScnSceneObject(ScnSceneObjectType.Trap)]
    [ScnSceneObject(ScnSceneObjectType.FallableWeapon)]
    [ScnSceneObject(ScnSceneObjectType.PedalSwitch)]
    [ScnSceneObject(ScnSceneObjectType.ElevatorSwitch)]
    [ScnSceneObject(ScnSceneObjectType.Elevator)]
    [ScnSceneObject(ScnSceneObjectType.ElevatorPedal)]
    [ScnSceneObject(ScnSceneObjectType.RotatingStoneBeam)]
    [ScnSceneObject(ScnSceneObjectType.EyeBall)]
    [ScnSceneObject(ScnSceneObjectType.MovableCarrier)]
    [ScnSceneObject(ScnSceneObjectType.SlideWay)]
    [ScnSceneObject(ScnSceneObjectType.ColdWeapon)]
    [ScnSceneObject(ScnSceneObjectType.FallableObstacle)]
    [ScnSceneObject(ScnSceneObjectType.Billboard)]
    [ScnSceneObject(ScnSceneObjectType.WindBlower)]
    [ScnSceneObject(ScnSceneObjectType.PreciseTrigger)]
    [ScnSceneObject(ScnSceneObjectType.RotatingWall)]
    [ScnSceneObject(ScnSceneObjectType.Arrow)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj47)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj48)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj49)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj51)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj52)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj53)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj54)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj55)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj56)]
    [ScnSceneObject(ScnSceneObjectType.UnknownObj59)]
    public class GeneralSceneObject : SceneObject
    {
        public GeneralSceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
        
        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            if (Activated) return GetGameObject();
            
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);

            // Don't cast shadow on the map entrance/exit indicator.
            // Those indicators are general objects which have Info.Parameters[0] set to 1
            if (ObjectInfo.Type == ScnSceneObjectType.General && ObjectInfo.Parameters[0] == 1)
            {
                foreach (MeshRenderer meshRenderer in sceneGameObject.GetComponentsInChildren<MeshRenderer>())
                {
                    meshRenderer.receiveShadows = false;
                }
            }
            
            return sceneGameObject;
        }
    }
}