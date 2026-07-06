using UnityEngine;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SceneLoader sceneLoader;

    private bool phoneGameplayUnlocked;
    private bool openPhoneOnNextIndoorLoad;

    public GameFlowState State { get; private set; } = GameFlowState.None;
    public bool PhoneGameplayUnlocked => phoneGameplayUnlocked;

    public static GameManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject gameObject = new GameObject("GameManager");
        return gameObject.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSceneLoader();
    }

    public void StartNewGame()
    {
        ResetDemoProgress();
        SetState(GameFlowState.OpeningLive);
        SceneLoader.Load(GameSceneId.OpeningStreaming);
    }

    public void ReturnToMainMenu()
    {
        ResetDemoProgress();
        SetState(GameFlowState.MainMenu);
        SceneLoader.Load(GameSceneId.MainMenu);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        Debug.Log("[GameManager] Quit requested. Application.Quit only closes a built player.");
#endif
    }

    public void OnOpeningCg1Started()
    {
        SetState(GameFlowState.Cg1);
    }

    public void OnOpeningChoiceShown()
    {
        SetState(GameFlowState.OpeningChoice);
    }

    public void ChooseOpeningBranch(OpeningChoice choice)
    {
        if (choice == OpeningChoice.KeepStatus)
        {
            SetState(GameFlowState.GameOver);
            return;
        }

        SetState(GameFlowState.TransformDialogue);
    }

    public void OnTransformDialogueFinished()
    {
        SetState(GameFlowState.Cg2);
    }

    public void OnOpeningChapterFinished()
    {
        EnterIndoorFlow();
    }

    public void EnterIndoorFlow()
    {
        SetState(GameFlowState.Indoor);
        SceneLoader.Load(GameSceneId.Indoor);
    }

    public void EnterOtherWorld()
    {
        SetState(GameFlowState.OtherWorld);
        SceneLoader.Load(GameSceneId.OtherWorld);
    }

    public void StartLiveStreamGameplay()
    {
        SetState(GameFlowState.LiveStream);
        SceneLoader.Load(GameSceneId.LiveStreamGameplay);
    }

    public void OnLiveStreamSucceeded()
    {
        phoneGameplayUnlocked = true;
        openPhoneOnNextIndoorLoad = true;
        SetState(GameFlowState.PhoneGameplay);
        SceneLoader.Load(GameSceneId.Indoor);
    }

    public void OnLiveStreamFailed()
    {
        SetState(GameFlowState.GameOver);
    }

    public void OnPhoneGameplayFinished()
    {
        SetState(GameFlowState.DemoEnd);
        Debug.Log("[GameManager] Demo flow reached phone gameplay result.");
    }

    public bool ConsumeOpenPhoneOnIndoorLoad()
    {
        if (!openPhoneOnNextIndoorLoad)
        {
            return false;
        }

        openPhoneOnNextIndoorLoad = false;
        return true;
    }

    public void SetState(GameFlowState state)
    {
        State = state;
        Debug.Log($"[GameManager] State -> {state}");
    }

    private void ResetDemoProgress()
    {
        phoneGameplayUnlocked = false;
        openPhoneOnNextIndoorLoad = false;
    }

    private SceneLoader SceneLoader
    {
        get
        {
            EnsureSceneLoader();
            return sceneLoader;
        }
    }

    private void EnsureSceneLoader()
    {
        if (sceneLoader == null)
        {
            sceneLoader = GetComponent<SceneLoader>();
        }

        if (sceneLoader == null)
        {
            sceneLoader = gameObject.AddComponent<SceneLoader>();
        }
    }
}
