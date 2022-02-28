// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE.txt in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataLoader
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;

    public static class AudioClipLoader
    {
        public static IEnumerator LoadAudioClip(string filePath, AudioType audioType, Action<AudioClip> callback)
        {
            string url = "file:///" + filePath;

            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"Failed to load {url} with error: {request.error}");
            }
            else
            {
                callback?.Invoke(DownloadHandlerAudioClip.GetContent(request));
            }
        }
    }
}