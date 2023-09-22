// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Dev
{
    using System;
    using System.Collections;
    using Constants;
    using Newtonsoft.Json.Linq;
    using UnityEngine;
    using UnityEngine.Networking;

    public static class GithubReleaseVersionFetcher
    {
        public static IEnumerator GetLatestReleaseVersionAsync(string owner, string repo, Action<string> callback)
        {
            // Construct the GitHub release API URL
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            using UnityWebRequest unityWebRequest = UnityWebRequest.Get(apiUrl);

            // Set the user agent header
            unityWebRequest.SetRequestHeader("User-Agent", GameConstants.AppName);

            // Wait for the request to complete
            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[{nameof(GithubReleaseVersionFetcher)}] Error fetching the latest release: {unityWebRequest.error}");
            }
            else
            {
                JObject releaseInfo = JObject.Parse(unityWebRequest.downloadHandler.text);
                callback.Invoke(releaseInfo["tag_name"]?.ToString());
            }
        }
    }
}

