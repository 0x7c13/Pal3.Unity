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
    /// UserVariableMask: SceCommand property user variable mask
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SceCommandAttribute : Attribute
    {
        public SceCommandAttribute(ushort commandId, string description)
        {
            CommandId = commandId;
            Description = description;
        }

        /// <summary>
        /// SceCommand Id
        /// </summary>
        public ushort CommandId { get; }

        /// <summary>
        /// Description of the command
        /// </summary>
        public string Description { get; }
    }
}