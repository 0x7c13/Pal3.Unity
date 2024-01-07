﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Scene.SceneObjects.Common
{
    using System;
    using Core.Contract.Enums;

    /// <summary>
    /// Attribute for SceneObject
    /// Type: ScnSceneObjectType
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ScnSceneObjectAttribute : Attribute
    {
        public ScnSceneObjectAttribute(SceneObjectType type)
        {
            Type = type;
        }

        public SceneObjectType Type { get; }
    }
}