// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.Utils
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public static class UnitySupportedVideoFormats
    {
        public static readonly HashSet<string> WindowsSupportedVideoFormats = new (StringComparer.OrdinalIgnoreCase)
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

        public static readonly HashSet<string> MacOSSupportedVideoFormats = new (StringComparer.OrdinalIgnoreCase)
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

        public static readonly HashSet<string> LinuxSupportedVideoFormats = new (StringComparer.OrdinalIgnoreCase)
        {
            ".ogv",
            ".vp8",
            ".webm",
        };

        public static readonly HashSet<string> AndroidSupportedVideoFormats = new (StringComparer.OrdinalIgnoreCase)
        {
            ".webm",
            ".mp4",
        };

        public static readonly HashSet<string> iOSSupportedVideoFormats = new (StringComparer.OrdinalIgnoreCase)
        {
            ".webm",
            ".mp4",
        };

        public static HashSet<string> GetSupportedVideoFormats(RuntimePlatform platform)
        {
            return platform switch
            {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => WindowsSupportedVideoFormats,
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor => MacOSSupportedVideoFormats,
                RuntimePlatform.LinuxPlayer or  RuntimePlatform.LinuxEditor => LinuxSupportedVideoFormats,
                RuntimePlatform.IPhonePlayer => iOSSupportedVideoFormats,
                RuntimePlatform.Android => AndroidSupportedVideoFormats,
                _ => new HashSet<string> (StringComparer.OrdinalIgnoreCase) {".mp4" }
            };
        }
    }
}