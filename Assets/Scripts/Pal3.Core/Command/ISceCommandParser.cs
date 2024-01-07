// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.Command
{
    using DataReader;

    /// <summary>
    /// Defines the interface for parsing SCE commands from a binary reader.
    /// </summary>
    public interface ISceCommandParser
    {
        /// <summary>
        /// Parse the next SceCommand from the given reader
        /// in the SCE script data block.
        /// </summary>
        /// <param name="reader">The binary reader to read from.</param>
        /// <param name="codepage">The codepage to use for decoding strings.</param>
        /// <param name="commandId">The ID of the parsed command.</param>
        /// <returns>The parsed command.</returns>
        public ICommand ParseNextCommand(IBinaryReader reader,
            int codepage,
            out ushort commandId);
    }
}