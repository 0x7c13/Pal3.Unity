// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    public static class GameConstants
    {
        #if PAL3
        public const string AppName = "PAL3";
        public const string AppNameCNShort = "仙剑三";
        public const string AppNameCNFull = "仙剑奇侠传三";
        #elif PAL3A
        public const string AppName = "PAL3A";
        public const string AppNameCNShort = "仙三外";
        public const string AppNameCNFull = "仙剑奇侠传三外传";
        #endif

        public const string Version = "0.13";

        public const string CompanyName = "OSS";

        public const string AppIdentifierPrefix = "com" + "." + CompanyName;

        public const string AppIdentifier = AppIdentifierPrefix + "." + AppName;

        public const string ContactInfo = "仙剑三复刻版讨论群：252315306 B站@柒才";
    }
}