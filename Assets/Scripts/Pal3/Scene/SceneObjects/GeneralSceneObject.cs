// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Core.DataReader.Scn;

    [ScnSceneObject(ScnSceneObjectType.General)]
    [ScnSceneObject(ScnSceneObjectType.ImpulsiveMechanism)]
    [ScnSceneObject(ScnSceneObjectType.SwordBridge)]
    [ScnSceneObject(ScnSceneObjectType.Knockdownable)]
    [ScnSceneObject(ScnSceneObjectType.JumpableArea)]
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
    [ScnSceneObject(ScnSceneObjectType.Shakeable)]
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
    }
}