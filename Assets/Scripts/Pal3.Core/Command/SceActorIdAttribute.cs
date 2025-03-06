// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using System;

    /// <summary>
    /// Attribute for SceActorId
    /// ActorId can be -1 (byte value is 255) which means the current player-controlled actor
    /// This attribute is used to mark the field that represents the actor id.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SceActorIdAttribute : Attribute
    {
    }
}