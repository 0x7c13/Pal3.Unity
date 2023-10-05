// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using System;

    /// <summary>
    /// Attribute for SceCommands
    /// Id: SceCommand ID
    /// Description: Command description
    /// ParameterFlag: SceCommand parameter flag
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SceCommandAttribute : Attribute
    {
        public SceCommandAttribute(int id, string description, int parameterFlag = 0)
        {
            Id = id;
            Description = description;
            ParameterFlag = parameterFlag;
        }

        public int Id { get; }
        public int ParameterFlag { get; }
        public string Description { get; }
    }
}