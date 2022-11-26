// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects.Common
{
    using System;
    using Core.DataReader.Scn;

    /// <summary>
    /// Attribute for SceneObject
    /// Type: ScnSceneObjectType
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ScnSceneObjectAttribute : Attribute
    {
        public ScnSceneObjectAttribute(ScnSceneObjectType type)
        {
            Type = type;
        }

        public ScnSceneObjectType Type { get; }
    }
}