using UnityEngine;

[DisallowMultipleComponent]
public class LiveStreamSceneBridge : MonoBehaviour
{
    [SerializeField] private LiveStreamController liveStreamController;
    [SerializeField] private bool startLiveOnStart = true;
    [SerializeField] private bool loadPhoneSceneOnSuccess = true;

    private void Awake()
    {
        if (liveStreamController == null)
        {
            liveStreamController = GetComponentInChildren<LiveStreamController>(true);
        }
    }

    private void OnEnable()
    {
        if (liveStreamController != null)
        {
            liveStreamController.LiveSucceeded += HandleLiveSucceeded;
            liveStreamController.LiveFailed += HandleLiveFailed;
        }
    }

    private void Start()
    {
        GameManager.EnsureInstance().SetState(GameFlowState.LiveStream);

        if (startLiveOnStart)
        {
            liveStreamController?.StartLive();
        }
    }

    private void OnDisable()
    {
        if (liveStreamController != null)
        {
            liveStreamController.LiveSucceeded -= HandleLiveSucceeded;
            liveStreamController.LiveFailed -= HandleLiveFailed;
        }
    }

    private void HandleLiveSucceeded()
    {
        if (loadPhoneSceneOnSuccess)
        {
            GameManager.EnsureInstance().OnLiveStreamSucceeded();
        }
    }

    private void HandleLiveFailed(LiveStreamFailReason reason)
    {
        Debug.Log($"[LiveStreamSceneBridge] Live failed: {reason}");
        GameManager.EnsureInstance().OnLiveStreamFailed();
    }
}
