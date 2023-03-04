// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Common;
    using Core.DataReader.Scn;

    /// <summary>
    /// Disabled scene objects will not be loaded to the scene.
    /// </summary>
    [ScnSceneObject(ScnSceneObjectType.SavingPoint)]
    [ScnSceneObject(ScnSceneObjectType.WishPool)]
    [ScnSceneObject(ScnSceneObjectType.FallableWeapon)]
    [ScnSceneObject(ScnSceneObjectType.Arrow)]
    [ScnSceneObject(ScnSceneObjectType.ColdWeapon)]
    [ScnSceneObject(ScnSceneObjectType.WindBlower)]
    [ScnSceneObject(ScnSceneObjectType.Billboard)]
    [ScnSceneObject(ScnSceneObjectType.PreciseTrigger)]
    [ScnSceneObject(ScnSceneObjectType.RotatingWall)]
    public sealed class DisabledSceneObject : SceneObject
    {
        public DisabledSceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo, hasModel: false)
        {
        }
    }
}