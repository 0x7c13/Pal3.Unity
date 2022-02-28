// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    using System;

    /// <summary>
    /// Attribute for SceCommands
    /// Id: SceCommand ID
    /// Description: Command description
    /// IsAvailableInConsole: Make the command also available in the debug console
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SceCommandAttribute : Attribute
    {
        public SceCommandAttribute(int id, string description, int parameterFlag = 0, bool isAvailableInConsole = true)
        {
            Id = id;
            Description = description;
            ParameterFlag = parameterFlag;
            IsAvailableInConsole = isAvailableInConsole;
        }

        public int Id { get; }
        public int ParameterFlag { get; }
        public string Description { get; }
        public bool IsAvailableInConsole { get; }
    }
}