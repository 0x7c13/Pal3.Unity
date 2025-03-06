// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.Logging
{
    using System.IO;
    using System.Runtime.CompilerServices;

    public static class EngineLogger
    {
        public static void Log(string message, [CallerFilePath] string callerFilePath = "")
        {
            string callerClassName = Path.GetFileNameWithoutExtension(callerFilePath);
            UnityEngine.Debug.Log($"[{callerClassName}] {message}");
        }

        public static void LogWarning(string message, [CallerFilePath] string callerFilePath = "")
        {
            string callerClassName = Path.GetFileNameWithoutExtension(callerFilePath);
            UnityEngine.Debug.LogWarning($"[{callerClassName}] {message}");
        }

        public static void LogError(string message, [CallerFilePath] string callerFilePath = "")
        {
            string callerClassName = Path.GetFileNameWithoutExtension(callerFilePath);
            UnityEngine.Debug.LogError($"[{callerClassName}] {message}");
        }

        public static void LogException(System.Exception exception,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "")
        {
            string callerClassName = Path.GetFileNameWithoutExtension(callerFilePath);
            UnityEngine.Debug.LogError($"[{callerClassName}] [{callerMemberName}] Exception: {exception.Message}");
            UnityEngine.Debug.LogException(exception);
        }
    }
}