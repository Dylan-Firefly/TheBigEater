using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneLoader : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string openingStreamingScene = "OpeningStreaming";
    [SerializeField] private string countrysideScene = "MainStreet";
    [SerializeField] private string otherWorldScene = "MainStreet";
    [SerializeField] private string marketScene = "MarketScene";
    [SerializeField] private string indoorScene = "IndoorScene";
    [SerializeField] private string liveStreamGameplayScene = "StreamGameplay";
    [SerializeField] private string phoneGameplayScene = "IndoorScene";

    public bool IsLoading { get; private set; }

    public void Load(GameSceneId sceneId)
    {
        string sceneName = GetSceneName(sceneId);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[SceneLoader] Scene name is empty for {sceneId}.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadAsync(GameSceneId sceneId)
    {
        if (!IsLoading)
        {
            StartCoroutine(LoadAsyncRoutine(sceneId));
        }
    }

    public string GetSceneName(GameSceneId sceneId)
    {
        return sceneId switch
        {
            GameSceneId.MainMenu => mainMenuScene,
            GameSceneId.OpeningStreaming => openingStreamingScene,
            GameSceneId.Countryside => countrysideScene,
            GameSceneId.OtherWorld => otherWorldScene,
            GameSceneId.Market => marketScene,
            GameSceneId.Indoor => indoorScene,
            GameSceneId.LiveStreamGameplay => liveStreamGameplayScene,
            GameSceneId.PhoneGameplay => phoneGameplayScene,
            _ => string.Empty
        };
    }

    private IEnumerator LoadAsyncRoutine(GameSceneId sceneId)
    {
        IsLoading = true;
        AsyncOperation operation = SceneManager.LoadSceneAsync(GetSceneName(sceneId));
        while (operation != null && !operation.isDone)
        {
            yield return null;
        }

        IsLoading = false;
    }
}
