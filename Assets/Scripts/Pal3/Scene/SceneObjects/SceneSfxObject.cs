// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Core.DataReader.Scn;
    using Data;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.SceneSfx)]
    public class SceneSfxObject : SceneObject
    {
        public string SfxName { get; }

        public SceneSfxObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo, hasModel: false)
        {
            SfxName = objectInfo.Name;
        }

        public override GameObject Activate(GameResourceProvider gameResourceProvider, GameObject parent, Color tintColor)
        {
            var sceneGameObject = base.Activate(gameResourceProvider, parent, tintColor);

            CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxAtGameObjectRequest(SfxName, 0, sceneGameObject));

            return sceneGameObject;
        }
    }
}