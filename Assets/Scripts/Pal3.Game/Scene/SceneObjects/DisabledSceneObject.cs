// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects
{
    using System.Collections;
    using Common;
    using Core.Contract.Enums;
    using Core.DataReader.Scn;

    /// <summary>
    /// Disabled scene objects will not be loaded to the scene.
    /// </summary>
    [ScnSceneObject(SceneObjectType.SavingPoint)]
    #if PAL3
    [ScnSceneObject(SceneObjectType.WishPool)]
    #endif
    [ScnSceneObject(SceneObjectType.FallableWeapon)]
    [ScnSceneObject(SceneObjectType.Arrow)]
    [ScnSceneObject(SceneObjectType.ColdWeapon)]
    [ScnSceneObject(SceneObjectType.WindBlower)]
    [ScnSceneObject(SceneObjectType.Billboard)]
    [ScnSceneObject(SceneObjectType.PreciseTrigger)]
    public sealed class DisabledSceneObject : SceneObject
    {
        public DisabledSceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo, hasModel: false)
        {
        }

        public override bool IsDirectlyInteractable(float distance) => false;

        public override bool ShouldGoToCutsceneWhenInteractionStarted() => false;

        public override IEnumerator InteractAsync(InteractionContext ctx)
        {
            yield break;
        }
    }
}