using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IndoorSceneBridge : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button outdoorButton;
    [SerializeField] private Button kitchenButton;

    [Header("Phone Demo")]
    [SerializeField] private PhoneDemoFlowController phoneFlow;
    [SerializeField] private bool openPhoneWhenReturningFromLive = true;

    [Header("Demo End")]
    [SerializeField] private GameObject endRoot;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private bool hideEndOnStart = true;
    [SerializeField] private float showEndDelaySeconds;

    private Coroutine showEndRoutine;

    private void OnEnable()
    {
        if (outdoorButton != null)
        {
            outdoorButton.onClick.AddListener(GoOutdoor);
        }

        if (kitchenButton != null)
        {
            kitchenButton.onClick.AddListener(GoKitchen);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (phoneFlow != null)
        {
            phoneFlow.FinalResultConfirmed += HandlePhoneFinalResultConfirmed;
        }
    }

    private void Start()
    {
        GameManager gameManager = GameManager.EnsureInstance();

        if (hideEndOnStart)
        {
            SetActive(endRoot, false);
        }

        bool shouldOpenPhone = gameManager.ConsumeOpenPhoneOnIndoorLoad();
        if (shouldOpenPhone)
        {
            gameManager.SetState(GameFlowState.PhoneGameplay);
            phoneFlow?.SetPostUnlocked(true);

            if (openPhoneWhenReturningFromLive)
            {
                phoneFlow?.OpenPhone();
            }
        }
        else
        {
            gameManager.SetState(GameFlowState.Indoor);
            phoneFlow?.SetPostUnlocked(gameManager.PhoneGameplayUnlocked);
        }
    }

    private void OnDisable()
    {
        if (outdoorButton != null)
        {
            outdoorButton.onClick.RemoveListener(GoOutdoor);
        }

        if (kitchenButton != null)
        {
            kitchenButton.onClick.RemoveListener(GoKitchen);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        if (phoneFlow != null)
        {
            phoneFlow.FinalResultConfirmed -= HandlePhoneFinalResultConfirmed;
        }
    }

    public void GoOutdoor()
    {
        AudioManager.PlayGenericButton();
        GameManager.EnsureInstance().EnterOtherWorld();
    }

    public void GoKitchen()
    {
        AudioManager.PlayGenericButton();
        GameManager.EnsureInstance().StartLiveStreamGameplay();
    }

    public void ReturnToMainMenu()
    {
        AudioManager.PlayMainMenu();
        GameManager.EnsureInstance().ReturnToMainMenu();
    }

    private void HandlePhoneFinalResultConfirmed(int goodCount, int badCount, float goodRatio, bool isGoodResult)
    {
        if (showEndRoutine != null)
        {
            StopCoroutine(showEndRoutine);
        }

        showEndRoutine = StartCoroutine(ShowEndRoutine());
    }

    private IEnumerator ShowEndRoutine()
    {
        if (showEndDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(showEndDelaySeconds);
        }

        GameManager.EnsureInstance().OnPhoneGameplayFinished();
        phoneFlow?.ClosePhone();
        SetActive(endRoot, true);
        showEndRoutine = null;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
