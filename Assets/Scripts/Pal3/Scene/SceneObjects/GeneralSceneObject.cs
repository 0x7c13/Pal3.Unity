// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Core.DataReader.Scn;

    [ScnSceneObject(ScnSceneObjectType.General)]
    [ScnSceneObject(ScnSceneObjectType.ImpulsiveMechanism)]
    [ScnSceneObject(ScnSceneObjectType.SwordBridge)]
    [ScnSceneObject(ScnSceneObjectType.Knockdownable)]
    [ScnSceneObject(ScnSceneObjectType.JumpableArea)]
    [ScnSceneObject(ScnSceneObjectType.Chest)]
    [ScnSceneObject(ScnSceneObjectType.RareChest)]
    [ScnSceneObject(ScnSceneObjectType.Trap)]
    [ScnSceneObject(ScnSceneObjectType.SuspensionBridge)]
    [ScnSceneObject(ScnSceneObjectType.FallableWeapon)]
    [ScnSceneObject(ScnSceneObjectType.WaterSurfaceMechanism)]
    [ScnSceneObject(ScnSceneObjectType.PedalSwitch)]
    [ScnSceneObject(ScnSceneObjectType.ElevatorSwitch)]
    [ScnSceneObject(ScnSceneObjectType.GravityTrigger)]
    [ScnSceneObject(ScnSceneObjectType.Elevator)]
    [ScnSceneObject(ScnSceneObjectType.ElevatorPedal)]
    [ScnSceneObject(ScnSceneObjectType.RotatingStoneBeam)]
    [ScnSceneObject(ScnSceneObjectType.EyeBall)]
    [ScnSceneObject(ScnSceneObjectType.MovableCarrier)]
    [ScnSceneObject(ScnSceneObjectType.SlideWay)]
    [ScnSceneObject(ScnSceneObjectType.ColdWeapon)]
    public class GeneralSceneObject : SceneObject
    {
        public GeneralSceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }
    }
}