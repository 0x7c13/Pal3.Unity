// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene
{
    using Core.DataReader.Scn;
    using SceneObjects;

    /// <summary>
    /// Interface for creating scene object instances.
    /// </summary>
    public interface ISceneObjectFactory
    {
        /// <summary>
        /// Creates a new scene object based on the provided object and scene information.
        /// </summary>
        /// <param name="objectInfo">The information for the object to create.</param>
        /// <param name="sceneInfo">The information for the scene in which the object will be created.</param>
        /// <returns>The newly created scene object instance.</returns>
        public SceneObject Create(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo);
    }
}