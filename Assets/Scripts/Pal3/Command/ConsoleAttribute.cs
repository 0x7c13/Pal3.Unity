// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command
{
    using System;

    /// <summary>
    /// Attribute for enabling the command in console.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AvailableInConsoleAttribute : Attribute
    {
        public AvailableInConsoleAttribute() { }
    }
}