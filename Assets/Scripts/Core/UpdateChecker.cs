using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

public class UpdateChecker : IInitializable
{
    private readonly string _repositoryApiUrl =
        "https://api.github.com/repos/BurngDaylght/VideoOptimizer-Tool/releases/latest";

    public event Action OnUpdateAvailable;
    
    public bool HasUpdate { get; private set; }

    public void Initialize()
    {
        CheckForUpdates().Forget();
    }

    private async UniTask CheckForUpdates()
    {
        UnityWebRequest request = UnityWebRequest.Get(_repositoryApiUrl);
        request.SetRequestHeader("User-Agent", "UnityApp");

        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Update check failed: " + request.error);
            return;
        }

        string json = request.downloadHandler.text;

        GitHubRelease release = JsonUtility.FromJson<GitHubRelease>(json);

        string latestVersion = release.tag_name;
        string currentVersion = Application.version;

        Debug.Log($"Current: {currentVersion} | Latest: {latestVersion}");

        if (IsNewerVersion(latestVersion, currentVersion))
        {
            Debug.Log("Update available!");
            HasUpdate = true;
            OnUpdateAvailable?.Invoke();
        }
    }

    private bool IsNewerVersion(string latest, string current)
    {
        if (latest.StartsWith("v"))
            latest = latest.Substring(1);

        Version vLatest = new Version(latest);
        Version vCurrent = new Version(current);

        return vLatest > vCurrent;
    }
}

[System.Serializable]
public class GitHubRelease
{
    public string tag_name;
}