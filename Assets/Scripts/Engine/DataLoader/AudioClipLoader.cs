// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2025, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Engine.DataLoader
{
    using System;
    using System.Collections;
    using Logging;
    using UnityEngine;
    using UnityEngine.Networking;

    public static class AudioClipLoader
    {
        /// <summary>
        /// Load Unity AudioClip from file.
        /// </summary>
        /// <param name="filePath">Audio file path</param>
        /// <param name="audioType">Audio type</param>
        /// <param name="streamAudio">Create streaming AudioClip</param>
        /// <param name="onLoaded">AudioClip callback invoker when loaded</param>
        /// <returns>IEnumerator</returns>
        public static IEnumerator LoadAudioClipAsync(string filePath,
            AudioType audioType,
            bool streamAudio,
            Action<AudioClip> onLoaded)
        {
            if (filePath.StartsWith("/")) filePath = filePath[1..];

            string url = "file:///" + filePath;

            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                EngineLogger.LogError($"Failed to load {url} with error: {request.error}");
                yield break; // Skip loading
            }

            try
            {
                DownloadHandlerAudioClip audioClipHandler = (DownloadHandlerAudioClip)request.downloadHandler;

                // Stream audio to avoid loading the entire AudioClip into memory at once,
                // which can cause frame drops and stuttering during gameplay.
                audioClipHandler.streamAudio = streamAudio;

                if (!streamAudio)
                {
                    // Compress audio to reduce memory usage,
                    // which is recommended for small sfx audio clips.
                    // StreamAudio must be false to compress audio.
                    audioClipHandler.compressed = true;
                }

                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                onLoaded?.Invoke(audioClip);
            }
            catch (Exception ex)
            {
                EngineLogger.LogException(ex);
            }
        }
    }
}