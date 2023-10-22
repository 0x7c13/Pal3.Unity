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
        public SceCommandAttribute(ushort commandId, string description, ushort userVariableMask = 0b0000)
        {
            CommandId = commandId;
            Description = description;
            UserVariableMask = userVariableMask;
        }

        /// <summary>
        /// SceCommand Id
        /// </summary>
        public ushort CommandId { get; }

        /// <summary>
        /// SceCommand property user variable mask
        /// 0b0001 means the first property is user variable (2 bytes UInt16)
        /// 0b0010 means the second property is user variable (2 bytes UInt16)
        /// 0b0100 means the third property is user variable (2 bytes UInt16)
        /// etc.
        /// </summary>
        public ushort UserVariableMask { get; }

        /// <summary>
        /// Description of the command
        /// </summary>
        public string Description { get; }
    }
}