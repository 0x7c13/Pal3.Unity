// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Utils
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class UnitySupportedVideoFormats
    {
        public static readonly HashSet<string> WindowsSupportedVideoFormats = new()
        {
            ".asf",
            ".avi",
            ".dv",
            ".m4v",
            ".mov",
            ".mp4",
            ".mpg",
            ".mpeg",
            ".ogv",
            ".vp8",
            ".webm",
            ".wmv",
        };

        public static readonly HashSet<string> MacOSSupportedVideoFormats = new()
        {
            ".dv",
            ".m4v",
            ".mov",
            ".mp4",
            ".mpg",
            ".mpeg",
            ".ogv",
            ".vp8",
            ".webm",
        };

        public static readonly HashSet<string> LinuxSupportedVideoFormats = new()
        {
            ".ogv",
            ".vp8",
            ".webm",
        };

        public static readonly HashSet<string> AndroidSupportedVideoFormats = new()
        {
            ".webm",
            ".mp4",
        };

        public static readonly HashSet<string> iOSSupportedVideoFormats = new()
        {
            ".webm",
            ".mp4",
        };

        public static HashSet<string> GetSupportedVideoFormats(RuntimePlatform platform)
        {
            return platform switch
            {
                RuntimePlatform.WindowsPlayer => WindowsSupportedVideoFormats,
                RuntimePlatform.WindowsEditor => WindowsSupportedVideoFormats,

                RuntimePlatform.OSXPlayer => MacOSSupportedVideoFormats,
                RuntimePlatform.OSXEditor => MacOSSupportedVideoFormats,

                RuntimePlatform.LinuxPlayer => LinuxSupportedVideoFormats,
                RuntimePlatform.LinuxEditor => LinuxSupportedVideoFormats,

                RuntimePlatform.IPhonePlayer => iOSSupportedVideoFormats,
                RuntimePlatform.Android => AndroidSupportedVideoFormats,

                _ => new () {".mp4" }
            };
        }
    }
}