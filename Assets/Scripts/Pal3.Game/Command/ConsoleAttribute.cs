// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Command
{
    using System;

    /// <summary>
    /// Attribute for enabling the command in console.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AvailableInConsoleAttribute : Attribute
    {
        public AvailableInConsoleAttribute() { }
    }
}