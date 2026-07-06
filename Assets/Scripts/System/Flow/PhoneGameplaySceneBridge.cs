using UnityEngine;

[DisallowMultipleComponent]
public class PhoneGameplaySceneBridge : MonoBehaviour
{
    [SerializeField] private PhoneDemoFlowController phoneFlow;
    [SerializeField] private bool unlockPostOnStart = true;

    private void Awake()
    {
        if (phoneFlow == null)
        {
            phoneFlow = GetComponentInChildren<PhoneDemoFlowController>(true);
        }
    }

    private void OnEnable()
    {
        if (phoneFlow != null)
        {
            phoneFlow.FinalResultConfirmed += HandleFinalResultConfirmed;
        }
    }

    private void Start()
    {
        GameManager.EnsureInstance().SetState(GameFlowState.PhoneGameplay);

        if (unlockPostOnStart)
        {
            phoneFlow?.SetPostUnlocked(true);
        }
    }

    private void OnDisable()
    {
        if (phoneFlow != null)
        {
            phoneFlow.FinalResultConfirmed -= HandleFinalResultConfirmed;
        }
    }

    private void HandleFinalResultConfirmed(int goodCount, int badCount, float goodRatio, bool isGoodResult)
    {
        Debug.Log($"[PhoneGameplaySceneBridge] Phone result good={goodCount}, bad={badCount}, ratio={goodRatio:0.00}, goodResult={isGoodResult}");
        GameManager.EnsureInstance().OnPhoneGameplayFinished();
    }
}
