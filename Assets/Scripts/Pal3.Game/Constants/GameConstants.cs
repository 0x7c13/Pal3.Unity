// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.Constants
{
    public enum Language
    {
        SimplifiedChinese = 0,
        TraditionalChinese,
    }

    public static class GameConstants
    {
        #if PAL3
        public const string AppName = "PAL3";
        public const string AppNameCNShort = "仙劍三";
        public const string AppNameCNFull = "仙劍奇俠傳三";
        #elif PAL3A
        public const string AppName = "PAL3A";
        public const string AppNameCNShort = "仙三外";
        public const string AppNameCNFull = "仙劍奇俠傳三外传";
        #endif

        public const string CompanyName = "Jackil";

        public const string AppIdentifierPrefix = "com" + "." + CompanyName;

        public const string AppIdentifier = AppIdentifierPrefix + "." + AppName;

        public const string ContactInfo = AppNameCNFull + "(复刻版) " + "讨论Q群：252315306 B站@柒才";

        public const string TestingType = "Alpha";

        public const string GithubRepoOwner = "0x7c13";

        public const string GithubRepoName = "Pal3.Unity";
    }
}