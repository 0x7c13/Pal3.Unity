// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Contract.Enums
{
    public enum ScriptExecutionMode
    {
        Asynchronous = 1,
        Synchronous  = 2,
    }

    public enum ScriptOperatorType
    {
        Assign  = 0,
        And     = 1,
        Or      = 2,
    }
}