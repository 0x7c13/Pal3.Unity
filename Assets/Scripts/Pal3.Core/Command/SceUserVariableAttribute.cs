// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using System;

    /// <summary>
    /// Attribute for SceUserVariable
    /// User variables are used to store the state of the game, and can be used in scripts.
    /// This attribute is used to mark the field that represents the user variable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SceUserVariableAttribute : Attribute
    {
    }
}