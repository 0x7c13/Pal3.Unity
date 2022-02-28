// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    public static class GameConstants
    {
        #if PAL3
        public const string AppName = "PAL3";
        #elif PAL3A
        public const string AppName = "PAL3A";
        #endif

        public const string Version = "0.5";

        public const string CompanyName = "OSS";

        public const string AppIdentifierPrefix = "com" + "." + CompanyName;

        public const string AppIdentifier = AppIdentifierPrefix + "." + AppName;

        public const string ContactInfo = "仙三复刻版开发测试群：252315306 B站@柒才";
    }
}